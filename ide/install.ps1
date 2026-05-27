# FlowForge — Installer for Windows (PowerShell)
# Usage:
#   Local:  .\install.ps1
#   Remote: iex ((New-Object System.Net.WebClient).DownloadString('https://raw.githubusercontent.com/efreet111/FlowForge/main/ide/install.ps1'))
#
# Detects installed IDEs and installs FlowForge rules, agents, and workflows.
# Supports: Cursor, Antigravity, VS Code

$ErrorActionPreference = "Stop"

# ── Remote mode detection ─────────────────────────────────────────────
$ScriptPath = $MyInvocation.MyCommand.Path
$IsRemote = [string]::IsNullOrEmpty($ScriptPath) -or $ScriptPath -eq ""

if ($IsRemote) {
    Write-Host "🌐 Modo remoto: descargando FlowForge..." -ForegroundColor Yellow
    $TempDir = Join-Path $env:TEMP "flowforge-install-$([System.Guid]::NewGuid().ToString().Substring(0,8))"
    
    try {
        if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
            Write-Host "❌ Error: 'git' no está instalado o no está en PATH." -ForegroundColor Red
            Write-Host "   Instalalo desde: https://git-scm.com/download/win" -ForegroundColor Yellow
            exit 1
        }
        git clone --depth 1 "https://github.com/efreet111/FlowForge.git" $TempDir 2>&1 | Out-Null
        Write-Host "✅ Repositorio descargado temporalmente en $TempDir" -ForegroundColor Green
    } catch {
        Write-Host "❌ Error: No se pudo clonar el repositorio. Verificá tu conexión a internet." -ForegroundColor Red
        exit 1
    }
    $FlowForgeRepo = $TempDir
} else {
    $FlowForgeRepo = Split-Path -Parent $PSScriptRoot
    Write-Host "📦 Modo local: $FlowForgeRepo" -ForegroundColor Green
}

$BackupDir = "$env:USERPROFILE\.flowforge-backups\$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$Installed = $false
$Cyan = "Cyan"

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
Write-Host "  FlowForge — Instalador para Windows" -ForegroundColor Blue
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
Write-Host ""

# --- Detect Cursor ────────────────────────────────────────────────────
$CursorDir = "$env:USERPROFILE\.cursor"
if (Test-Path $CursorDir) {
    Write-Host "[✓] Cursor detectado" -ForegroundColor Green
    New-Item -ItemType Directory -Force -Path "$BackupDir\cursor" -ErrorAction SilentlyContinue | Out-Null
    New-Item -ItemType Directory -Force -Path "$CursorDir\rules" -ErrorAction SilentlyContinue | Out-Null
    New-Item -ItemType Directory -Force -Path "$CursorDir\agents" -ErrorAction SilentlyContinue | Out-Null

    # Backup existing files
    if (Test-Path "$CursorDir\rules\workflow.mdc") { Copy-Item "$CursorDir\rules\workflow.mdc" "$BackupDir\cursor\" }

    # Rules
    $rules = @("workflow.mdc", "model-assignments.mdc", "git-sin-push.mdc")
    foreach ($rule in $rules) {
        $src = "$FlowForgeRepo\ide\cursor\rules\$rule"
        if (Test-Path $src) { Copy-Item $src "$CursorDir\rules\" }
    }
    Write-Host "  ✓ Rules copiadas" -ForegroundColor Green

    # Agents
    $agents = @("forge-discovery", "forge-arch", "forge-plan", "forge-dev", "forge-verify", "forge-memory")
    foreach ($agent in $agents) {
        $src = "$FlowForgeRepo\ide\cursor\agents\$agent.md"
        if (Test-Path $src) {
            Copy-Item $src "$CursorDir\agents\"
            Write-Host "  ✓ Agent $agent copiado" -ForegroundColor Green
        }
    }
    $Installed = $true
}

# --- Detect Antigravity ───────────────────────────────────────────────
$GeminiDir = "$env:LOCALAPPDATA\Google\Gemini"  # Common path, may vary
$GeminiDir2 = "$env:USERPROFILE\.gemini"
if ((Test-Path $GeminiDir) -or (Test-Path $GeminiDir2)) {
    Write-Host "[✓] Antigravity detectado" -ForegroundColor Green
    Write-Host "  Las reglas se instalan por proyecto." -ForegroundColor Yellow
    Write-Host "  En la raíz de tu proyecto ejecutá:" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "    New-Item -ItemType Directory -Force -Path .agents\rules, .agents\workflows" -ForegroundColor $Cyan
    Write-Host "    Copy-Item -Path ""$FlowForgeRepo\ide\antigravity\rules\*"" -Destination .agents\rules\" -ForegroundColor $Cyan
    Write-Host "    Copy-Item -Path ""$FlowForgeRepo\ide\antigravity\workflows\*"" -Destination .agents\workflows\" -ForegroundColor $Cyan
    Write-Host "    Copy-Item -Path ""$FlowForgeRepo\ide\antigravity\AGENTS.md"" -Destination ." -ForegroundColor $Cyan
    Write-Host ""
    $Installed = $true
}

# --- Detect VS Code ───────────────────────────────────────────────────
$VSCodeDir = "$env:USERPROFILE\.vscode"
if (Test-Path $VSCodeDir) {
    Write-Host "[✓] VS Code detectado" -ForegroundColor Green
    $src = "$FlowForgeRepo\ide\vscode\copilot-instructions.md"
    if (Test-Path $src) {
        Copy-Item $src "$VSCodeDir\"
        Write-Host "  ✓ Copilot instructions copiadas" -ForegroundColor Green
    }
    $Installed = $true
}

# ── Cleanup remote temp ──────────────────────────────────────────────
if ($IsRemote -and (Test-Path $TempDir)) {
    Remove-Item -Recurse -Force $TempDir -ErrorAction SilentlyContinue
}

# ── Summary ──────────────────────────────────────────────────────────
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
if ($Installed) {
    Write-Host "✅ Instalación completada" -ForegroundColor Green
    if (Test-Path $BackupDir) {
        Write-Host "  Backups en: $BackupDir" -ForegroundColor Yellow
    }
    Write-Host ""
    Write-Host "📋 Próximos pasos:" -ForegroundColor Yellow
    Write-Host "  1. Reiniciá tu IDE"
    Write-Host "  2. Seleccioná el agente 'flowforge'"
    Write-Host "  3. Escribí: /flow-start CRUD de tareas"
    Write-Host ""
    Write-Host "📖 Documentación completa:" -ForegroundColor Yellow
    Write-Host "  https://github.com/efreet111/FlowForge"
} else {
    Write-Host "❌ No se detectó ningún IDE compatible" -ForegroundColor Red
}
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
