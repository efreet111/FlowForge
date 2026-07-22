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
    # git writes progress to stderr; with $ErrorActionPreference = "Stop" that aborts the script.
    $prevErrorAction = $ErrorActionPreference
    $ErrorActionPreference = "Continue"
    try {
        & git clone --depth 1 "https://github.com/efreet111/FlowForge.git" $TempDir 2>&1 | Out-Null
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Error: git clone fallo (exit $LASTEXITCODE). Comprueba red y acceso a GitHub." -ForegroundColor Red
            exit 1
        }
        if (-not (Test-Path (Join-Path $TempDir "AGENTS.md"))) {
            Write-Host "Error: clone incompleto (falta AGENTS.md en $TempDir)." -ForegroundColor Red
            exit 1
        }
    } finally {
        $ErrorActionPreference = $prevErrorAction
    }
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

function Patch-OpenCodeFlowforgeJson {
    param(
        [string]$Dest,
        [string]$Repo
    )
    if (-not (Test-Path $Dest)) { return }
    $content = Get-Content -Path $Dest -Raw -Encoding UTF8
    if ($content -notmatch '__FLOWFORGE_REPO__') { return }
    $content = $content.Replace('__FLOWFORGE_REPO__', $Repo.Replace('\', '/'))
    Set-Content -Path $Dest -Value $content -Encoding UTF8 -NoNewline
    Write-Host "  OK Rutas skills -> $Repo" -ForegroundColor Green
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

function Install-AntigravitySkills {
    param(
        [string]$DestDir,
        [string]$FlowForgeRepo
    )
    $skillsSrc = Join-Path $FlowForgeRepo "skills"
    if (-not (Test-Path $skillsSrc)) { return }

    New-Item -ItemType Directory -Force -Path $DestDir | Out-Null
    $useCopy = $script:IsRemote -or ($FlowForgeRepo -match '[\\/]Temp[\\/]') -or ($FlowForgeRepo -match '[\\/]tmp[\\/]')

    foreach ($skillDir in Get-ChildItem -Path $skillsSrc -Directory -Filter "forge-*") {
        $name = $skillDir.Name
        $target = Join-Path $DestDir $name
        if (Test-Path $target) {
            Remove-Item -Recurse -Force $target -ErrorAction SilentlyContinue
        }
        if ($useCopy) {
            Copy-Item -Path $skillDir.FullName -Destination $target -Recurse -Force
        } else {
            try {
                New-Item -ItemType SymbolicLink -Path $target -Target $skillDir.FullName -Force | Out-Null
            } catch {
                Copy-Item -Path $skillDir.FullName -Destination $target -Recurse -Force
            }
        }
    }
}

function Remove-LegacyAntigravityPack {
    $legacyPaths = @(
        (Join-Path $env:USERPROFILE ".gemini\antigravity"),
        (Join-Path $env:LOCALAPPDATA "Google\Gemini\antigravity")
    )
    foreach ($legacy in $legacyPaths) {
        if (-not (Test-Path $legacy)) { continue }
        foreach ($item in @("AGENTS.md", "rules", "workflows")) {
            $path = Join-Path $legacy $item
            if (Test-Path $path) {
                Remove-Item -Recurse -Force $path -ErrorAction SilentlyContinue
            }
        }
    }
    $skillsJson = Join-Path $env:USERPROFILE ".gemini\config\skills.json"
    if (Test-Path $skillsJson) {
        Remove-Item -Force $skillsJson -ErrorAction SilentlyContinue
    }
}

function Migrate-LegacyAntigravityWorkflows {
    $legacy = Join-Path $env:USERPROFILE ".gemini\config\workflows"
    $target = Join-Path $env:USERPROFILE ".gemini\config\global_workflows"
    if (-not (Test-Path $legacy)) { return }
    New-Item -ItemType Directory -Force -Path $target | Out-Null
    foreach ($f in Get-ChildItem -Path $legacy -Filter "flow-*.md" -ErrorAction SilentlyContinue) {
        $dest = Join-Path $target $f.Name
        if (-not (Test-Path $dest) -or $f.LastWriteTimeUtc -gt (Get-Item $dest).LastWriteTimeUtc) {
            Copy-Item $f.FullName $dest -Force
        }
    }
}

function Install-AntigravityGlobal {
    param([string]$FlowForgeRepo)

    $configDir = Join-Path $env:USERPROFILE ".gemini\config"
    $workspaceAgents = Join-Path $configDir ".agents"
    $dirs = @(
        $configDir,
        (Join-Path $configDir "rules"),
        (Join-Path $configDir "global_workflows"),
        (Join-Path $configDir "skills"),
        $workspaceAgents,
        (Join-Path $workspaceAgents "rules"),
        (Join-Path $workspaceAgents "workflows"),
        (Join-Path $workspaceAgents "skills")
    )
    foreach ($d in $dirs) { New-Item -ItemType Directory -Force -Path $d | Out-Null }

    Migrate-LegacyAntigravityWorkflows

    $agentsMd = Join-Path $IdeDir "antigravity\AGENTS.md"
    if (Test-Path $agentsMd) {
        Copy-Item $agentsMd (Join-Path $configDir "AGENTS.md") -Force
        Copy-Item $agentsMd (Join-Path $workspaceAgents "AGENTS.md") -Force
    }
    Copy-Item (Join-Path $IdeDir "antigravity\rules\*") (Join-Path $configDir "rules") -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $IdeDir "antigravity\workflows\*") (Join-Path $configDir "global_workflows") -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $IdeDir "antigravity\rules\*") (Join-Path $workspaceAgents "rules") -Force -ErrorAction SilentlyContinue
    Copy-Item (Join-Path $IdeDir "antigravity\workflows\*") (Join-Path $workspaceAgents "workflows") -Force -ErrorAction SilentlyContinue

    Install-AntigravitySkills -DestDir (Join-Path $configDir "skills") -FlowForgeRepo $FlowForgeRepo
    Install-AntigravitySkills -DestDir (Join-Path $workspaceAgents "skills") -FlowForgeRepo $FlowForgeRepo

    $workflowRule = Join-Path $IdeDir "antigravity\rules\workflow.md"
    if (Test-Path $workflowRule) {
        $geminiMd = Join-Path $env:USERPROFILE ".gemini\GEMINI.md"
        New-Item -ItemType Directory -Force -Path (Split-Path $geminiMd) | Out-Null
        Copy-Item $workflowRule $geminiMd -Force
    }

    Remove-LegacyAntigravityPack
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
    $agentsSkills = Join-Path $Root ".agents\skills"
    New-Item -ItemType Directory -Force -Path $agentsRules, $agentsWf, $agentsSkills | Out-Null
    Copy-Item -Path (Join-Path $IdeDir "antigravity\rules\*") -Destination $agentsRules -Force
    Copy-Item -Path (Join-Path $IdeDir "antigravity\workflows\*") -Destination $agentsWf -Force
    Copy-Item -Path (Join-Path $IdeDir "antigravity\AGENTS.md") -Destination (Join-Path $Root ".agents\AGENTS.md") -Force
    if (-not (Test-Path (Join-Path $Root "AGENTS.md"))) {
        Copy-Item -Path (Join-Path $IdeDir "antigravity\AGENTS.md") -Destination $Root -Force
    }
    Install-AntigravitySkills -DestDir $agentsSkills -FlowForgeRepo $FlowForgeRepo
    Write-Host "  OK .agents/rules + workflows + skills + AGENTS.md" -ForegroundColor Green

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
    $ghDir = Join-Path $Root ".github"
    Copy-Item (Join-Path $IdeDir "vscode\copilot-instructions.md") $ghDir -Force -ErrorAction SilentlyContinue
    Write-Host "  OK .github/copilot-instructions.md" -ForegroundColor Green
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
    $agentsDir = Join-Path $OpenCodeDir "agents"
    $commandsDir = Join-Path $OpenCodeDir "commands"
    New-Item -ItemType Directory -Force -Path $agentsDir, $commandsDir | Out-Null
    if (Test-Path (Join-Path $IdeDir "opencode\agents")) {
        Copy-Item (Join-Path $IdeDir "opencode\agents\*.md") $agentsDir -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path (Join-Path $IdeDir "opencode\commands")) {
        Copy-Item (Join-Path $IdeDir "opencode\commands\*.md") $commandsDir -Force -ErrorAction SilentlyContinue
    }
    $legacyBundle = Join-Path $OpenCodeDir "flowforge"
    if (Test-Path $legacyBundle) {
        Remove-Item -Recurse -Force $legacyBundle
    }
    $legacyJson = Join-Path $OpenCodeDir "opencode.flowforge.json"
    if (Test-Path $legacyJson) {
        Remove-Item -Force $legacyJson
    }
    Write-Host "  OK ~/.config/opencode/agents/ + commands/" -ForegroundColor Green
    Write-Host "  ! Modelos opencode-go/*: configura proveedor + API keys en OpenCode" -ForegroundColor Yellow
    $Installed = $true
}

# --- VS Code (global) ---
if (Test-Path "$env:USERPROFILE\.vscode") {
    Write-Host "[OK] VS Code detectado" -ForegroundColor Green

    $extensionsDir = "$env:USERPROFILE\.vscode\extensions"
    $hasCopilot = (Test-Path $extensionsDir) -and (Get-ChildItem -Path $extensionsDir -Filter "github.copilot*" -Directory -ErrorAction SilentlyContinue | Measure-Object).Count -gt 0
    $hasKilo = (Test-Path $extensionsDir) -and (Get-ChildItem -Path $extensionsDir -Filter "kilocode.*" -Directory -ErrorAction SilentlyContinue | Measure-Object).Count -gt 0

    if ($hasCopilot -or -not $hasKilo) {
        $copilotAgents = "$env:USERPROFILE\.copilot\agents"
        $copilotInstructions = "$env:USERPROFILE\.copilot\instructions"
        New-Item -ItemType Directory -Force -Path $copilotAgents, $copilotInstructions | Out-Null
        Copy-Item (Join-Path $IdeDir "vscode\agents\*.agent.md") $copilotAgents -Force -ErrorAction SilentlyContinue
        $srcInstructions = Join-Path $IdeDir "vscode\copilot-instructions.md"
        if (Test-Path $srcInstructions) {
            $destInstructions = Join-Path $copilotInstructions "flowforge.instructions.md"
            @(
                "---"
                "applyTo: '**'"
                "---"
                (Get-Content $srcInstructions -Raw)
            ) | Set-Content -Path $destInstructions -Encoding UTF8
        }
        Write-Host "  OK ~/.copilot/agents + ~/.copilot/instructions/" -ForegroundColor Green
    }

    if ($hasKilo) {
        $kiloDir = "$env:USERPROFILE\.config\kilo\agents"
        New-Item -ItemType Directory -Force -Path $kiloDir | Out-Null
        Copy-Item (Join-Path $IdeDir "opencode\agents\*.md") $kiloDir -Force -ErrorAction SilentlyContinue
        Write-Host "  OK ~/.config/kilo/agents/" -ForegroundColor Green
    }

    if (-not $hasCopilot -and -not $hasKilo) {
        $kiloDir = "$env:USERPROFILE\.config\kilo\agents"
        New-Item -ItemType Directory -Force -Path $kiloDir | Out-Null
        Copy-Item (Join-Path $IdeDir "opencode\agents\*.md") $kiloDir -Force -ErrorAction SilentlyContinue
        Write-Host "  ! VS Code: no se detectó GitHub Copilot ni Kilo Code — instalados ambos formatos por si acaso" -ForegroundColor Yellow
        Write-Host "  OK ~/.config/kilo/agents/" -ForegroundColor Green
    }

    Write-Host "  ! Para repo: ejecuta install.ps1 -ProjectPath <ruta>" -ForegroundColor Yellow
    $Installed = $true
}

# --- Antigravity (global) ---
if (Test-Path (Join-Path $env:USERPROFILE ".gemini")) {
    Write-Host "[OK] Antigravity detectado" -ForegroundColor Green
    Install-AntigravityGlobal -FlowForgeRepo $FlowForgeRepo
    Write-Host "  OK $env:USERPROFILE\.gemini\config\ (AGENTS + rules + global_workflows + skills)" -ForegroundColor Green
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
    Write-Host "  1. Reload Window / reiniciar Antigravity en el IDE"
    Write-Host "  2. Proyecto nuevo: install.ps1 -ProjectPath <repo>"
    Write-Host "  3. /flow-start <feature>  o  /flow-rework si hay bug"
    Write-Host ""
    Write-Host "Paridad: $GlobalShareDir\workflow-orchestrator-parity.md"
} else {
    Write-Host "No se detecto IDE compatible (Cursor / OpenCode / VS Code)" -ForegroundColor Red
}
Write-Host "========================================" -ForegroundColor Blue
