# FlowForge — Installer for Windows (PowerShell)
# Usage:
#   Global IDEs:  .\install.ps1
#   Per project:  .\install.ps1 -ProjectPath "E:\Proyectos\mi-app"
#   Remote:       iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
#
# Installs: shared parity, Cursor (~/.cursor), OpenCode (~/.config/opencode), VS Code hints,
#           and optionally Antigravity + project .cursor / .github/agents via -ProjectPath.

param(
    [string]$ProjectPath = ""
)

$ErrorActionPreference = "Stop"

# ── Remote mode detection ─────────────────────────────────────────────
$ScriptPath = $MyInvocation.MyCommand.Path
$IsRemote = [string]::IsNullOrEmpty($ScriptPath) -or $ScriptPath -eq ""

if ($IsRemote) {
    Write-Host "Modo remoto: descargando FlowForge..." -ForegroundColor Yellow
    $TempDir = Join-Path $env:TEMP "flowforge-install-$([System.Guid]::NewGuid().ToString().Substring(0,8))"
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
        Write-Host "Error: git no esta en PATH. https://git-scm.com/download/win" -ForegroundColor Red
        exit 1
    }
    git clone --depth 1 "https://github.com/efreet111/FlowForge.git" $TempDir 2>&1 | Out-Null
    $FlowForgeRepo = $TempDir
} else {
    $FlowForgeRepo = Split-Path -Parent $PSScriptRoot
}

$IdeDir = Join-Path $FlowForgeRepo "ide"
$BackupDir = "$env:USERPROFILE\.flowforge-backups\$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$GlobalShareDir = "$env:USERPROFILE\.flowforge\shared"
$Installed = $false
$Cyan = "Cyan"

function Install-FlowForgeShared {
    param([string]$DestinationDir)
    $src = Join-Path $IdeDir "shared"
    if (-not (Test-Path $src)) { return }
    New-Item -ItemType Directory -Force -Path $DestinationDir | Out-Null
    Copy-Item -Path (Join-Path $src "*") -Destination $DestinationDir -Recurse -Force
}

function Compile-CursorAgents {
    $compile = Join-Path $IdeDir "cursor\compile-agents-from-skills.py"
    if (-not (Test-Path $compile)) { return $false }
    if (-not (Get-Command python -ErrorAction SilentlyContinue)) {
        Write-Host "  ! Python no encontrado; agentes Cursor sin recompilar desde skills/" -ForegroundColor Yellow
        return $false
    }
    Push-Location $FlowForgeRepo
    try {
        python $compile 2>&1 | ForEach-Object { Write-Host "  $_" }
        Write-Host "  OK Agentes Cursor recompilados desde skills/" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "  ! compile-agents fallo: $_" -ForegroundColor Yellow
        return $false
    } finally {
        Pop-Location
    }
}

function Install-ProjectBundle {
    param([string]$Root)
    if (-not (Test-Path $Root)) {
        Write-Host "  X ProjectPath no existe: $Root" -ForegroundColor Red
        return
    }
    Write-Host "[*] Instalacion por proyecto: $Root" -ForegroundColor Green

    # Antigravity
    $agentsRules = Join-Path $Root ".agents\rules"
    $agentsWf = Join-Path $Root ".agents\workflows"
    New-Item -ItemType Directory -Force -Path $agentsRules, $agentsWf | Out-Null
    Copy-Item -Path (Join-Path $IdeDir "antigravity\rules\*") -Destination $agentsRules -Force
    Copy-Item -Path (Join-Path $IdeDir "antigravity\workflows\*") -Destination $agentsWf -Force
    Copy-Item -Path (Join-Path $IdeDir "antigravity\AGENTS.md") -Destination $Root -Force
    Write-Host "  OK .agents/rules + workflows + AGENTS.md" -ForegroundColor Green

    # Paridad en repo (referencia)
    $projShared = Join-Path $Root ".flowforge\shared"
    Install-FlowForgeShared -DestinationDir $projShared
    Write-Host "  OK .flowforge/shared/" -ForegroundColor Green

    # Cursor por proyecto (demo / equipos)
    $projCursor = Join-Path $Root ".cursor"
    New-Item -ItemType Directory -Force -Path "$projCursor\rules", "$projCursor\agents" | Out-Null
    foreach ($rule in @("workflow.mdc", "model-assignments.mdc", "git-sin-push.mdc")) {
        $src = Join-Path $IdeDir "cursor\rules\$rule"
        if (Test-Path $src) { Copy-Item $src "$projCursor\rules\" -Force }
    }
    foreach ($agent in @("forge-discovery", "forge-arch", "forge-plan", "forge-dev", "forge-verify", "forge-memory")) {
        $src = Join-Path $IdeDir "cursor\agents\$agent.md"
        if (Test-Path $src) { Copy-Item $src "$projCursor\agents\" -Force }
    }
    $cmdSrc = Join-Path $IdeDir "cursor\commands"
    if (Test-Path $cmdSrc) {
        New-Item -ItemType Directory -Force -Path "$projCursor\commands" | Out-Null
        Copy-Item -Path "$cmdSrc\*.md" -Destination "$projCursor\commands\" -Force
        Write-Host "  OK .cursor/commands/" -ForegroundColor Green
    }
    Write-Host "  OK .cursor/rules + agents" -ForegroundColor Green

    # VS Code agents por proyecto
    $ghAgents = Join-Path $Root ".github\agents"
    New-Item -ItemType Directory -Force -Path $ghAgents | Out-Null
    $vscodeAgents = Join-Path $IdeDir "vscode\agents"
    if (Test-Path $vscodeAgents) {
        Copy-Item -Path "$vscodeAgents\*.agent.md" -Destination $ghAgents -Force
        Write-Host "  OK .github/agents/" -ForegroundColor Green
    }
    $vscodeDir = Join-Path $Root ".vscode"
    New-Item -ItemType Directory -Force -Path $vscodeDir | Out-Null
    Copy-Item (Join-Path $IdeDir "vscode\copilot-instructions.md") $vscodeDir -Force -ErrorAction SilentlyContinue
}

Write-Host "========================================" -ForegroundColor Blue
Write-Host "  FlowForge - Instalador Windows" -ForegroundColor Blue
Write-Host "========================================" -ForegroundColor Blue
Write-Host ""

# Shared parity (global)
Install-FlowForgeShared -DestinationDir $GlobalShareDir
Write-Host "[OK] Paridad global: $GlobalShareDir" -ForegroundColor Green

Compile-CursorAgents | Out-Null

# --- Cursor (global) ---
$CursorDir = "$env:USERPROFILE\.cursor"
if (Test-Path $CursorDir) {
    Write-Host "[OK] Cursor detectado" -ForegroundColor Green
    New-Item -ItemType Directory -Force -Path "$BackupDir\cursor", "$CursorDir\rules", "$CursorDir\agents" | Out-Null
    if (Test-Path "$CursorDir\rules\workflow.mdc") { Copy-Item "$CursorDir\rules\workflow.mdc" "$BackupDir\cursor\" }
    foreach ($rule in @("workflow.mdc", "model-assignments.mdc", "git-sin-push.mdc")) {
        $src = Join-Path $IdeDir "cursor\rules\$rule"
        if (Test-Path $src) { Copy-Item $src "$CursorDir\rules\" -Force }
    }
    foreach ($agent in @("forge-discovery", "forge-arch", "forge-plan", "forge-dev", "forge-verify", "forge-memory")) {
        $src = Join-Path $IdeDir "cursor\agents\$agent.md"
        if (Test-Path $src) { Copy-Item $src "$CursorDir\agents\" -Force }
    }
    $cmdGlobal = Join-Path $IdeDir "cursor\commands"
    if (Test-Path $cmdGlobal) {
        New-Item -ItemType Directory -Force -Path "$CursorDir\commands" | Out-Null
        Copy-Item "$cmdGlobal\*.md" "$CursorDir\commands\" -Force
    }
    Write-Host "  OK ~/.cursor/rules + agents + commands" -ForegroundColor Green
    $Installed = $true
}

# --- OpenCode ---
$OpenCodeDir = "$env:USERPROFILE\.config\opencode"
if (Test-Path $OpenCodeDir) {
    Write-Host "[OK] OpenCode detectado" -ForegroundColor Green
    $ffBundle = Join-Path $OpenCodeDir "flowforge"
    New-Item -ItemType Directory -Force -Path "$ffBundle\shared" | Out-Null
    Copy-Item (Join-Path $IdeDir "opencode\AGENTS.md") $ffBundle -Force
    Install-FlowForgeShared -DestinationDir (Join-Path $ffBundle "shared")
    Copy-Item (Join-Path $IdeDir "opencode\opencode.flowforge.json") $OpenCodeDir -Force
    Write-Host "  OK ~/.config/opencode/flowforge/ + opencode.flowforge.json" -ForegroundColor Green
    Write-Host "  ! Mergea agent{} de opencode.flowforge.json en tu opencode.json" -ForegroundColor Yellow
    Write-Host "  ! Ajusta rutas {file:...} de skills a tu clone de FlowForge" -ForegroundColor Yellow
    $Installed = $true
}

# --- VS Code (global hints) ---
if (Test-Path "$env:USERPROFILE\.vscode") {
    Write-Host "[OK] VS Code detectado" -ForegroundColor Green
    $vsAgents = "$env:USERPROFILE\.vscode\agents"
    New-Item -ItemType Directory -Force -Path $vsAgents | Out-Null
    Copy-Item (Join-Path $IdeDir "vscode\copilot-instructions.md") "$env:USERPROFILE\.vscode\" -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $IdeDir "vscode\agents\*.agent.md") $vsAgents -Force -ErrorAction SilentlyContinue
    Write-Host "  OK ~/.vscode/copilot-instructions + agents" -ForegroundColor Green
    Write-Host "  ! Para repo: ejecuta install.ps1 -ProjectPath <ruta>" -ForegroundColor Yellow
    $Installed = $true
}

# --- Antigravity (mensaje o proyecto) ---
$HasAntigravity = (Test-Path "$env:LOCALAPPDATA\Google\Gemini") -or (Test-Path "$env:USERPROFILE\.gemini")
if ($HasAntigravity) {
    Write-Host "[OK] Antigravity detectado" -ForegroundColor Green
    if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
        Write-Host "  Usa: .\install.ps1 -ProjectPath .  (desde la raiz del proyecto)" -ForegroundColor $Cyan
    }
    $Installed = $true
}

# --- Proyecto opcional ---
if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
    $resolved = Resolve-Path $ProjectPath -ErrorAction SilentlyContinue
    if ($resolved) { Install-ProjectBundle -Root $resolved.Path }
    else { Install-ProjectBundle -Root $ProjectPath }
}

if ($IsRemote -and (Test-Path $TempDir)) {
    Remove-Item -Recurse -Force $TempDir -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Blue
if ($Installed) {
    Write-Host "Instalacion completada" -ForegroundColor Green
    if (Test-Path $BackupDir) { Write-Host "  Backups: $BackupDir" -ForegroundColor Yellow }
    Write-Host ""
    Write-Host "Proximos pasos:" -ForegroundColor Yellow
    Write-Host "  1. Reload Window en el IDE"
    Write-Host "  2. Proyecto nuevo: install.ps1 -ProjectPath <repo>"
    Write-Host "  3. /flow-start <feature>  o  /flow-rework si hay bug"
    Write-Host ""
    Write-Host "Paridad: $GlobalShareDir\workflow-orchestrator-parity.md"
} else {
    Write-Host "No se detecto IDE compatible (Cursor / OpenCode / VS Code)" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Blue
