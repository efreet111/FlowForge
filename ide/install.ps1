# FlowForge — Installer for Windows (PowerShell)
# Usage: .\install.ps1
#
# Detects installed IDEs and installs FlowForge rules, agents, and workflows.
# Supports: Cursor, Antigravity, VS Code

$ErrorActionPreference = "Stop"
$FlowForgeRepo = Split-Path -Parent $PSScriptRoot
$BackupDir = "$env:USERPROFILE\.flowforge-backups\$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$Installed = $false

Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
Write-Host "  FlowForge — Instalador para Windows" -ForegroundColor Blue
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
Write-Host ""
Write-Host "Repositorio: $FlowForgeRepo" -ForegroundColor Yellow
Write-Host ""

# --- Detect Cursor ---
$CursorDir = "$env:USERPROFILE\.cursor"
if (Test-Path $CursorDir) {
    Write-Host "[✓] Cursor detectado" -ForegroundColor Green
    New-Item -ItemType Directory -Force -Path "$BackupDir\cursor" | Out-Null

    # Backup existing files
    if (Test-Path "$CursorDir\rules\workflow.mdc") { Copy-Item "$CursorDir\rules\workflow.mdc" "$BackupDir\cursor\" }

    # Rules
    New-Item -ItemType Directory -Force -Path "$CursorDir\rules" | Out-Null
    Copy-Item "$FlowForgeRepo\ide\cursor\rules\workflow.mdc" "$CursorDir\rules\"
    Copy-Item "$FlowForgeRepo\ide\cursor\rules\model-assignments.mdc" "$CursorDir\rules\"
    Copy-Item "$FlowForgeRepo\ide\cursor\rules\git-sin-push.mdc" "$CursorDir\rules\"
    Write-Host "  ✓ Rules copiadas" -ForegroundColor Green

    # Agents
    New-Item -ItemType Directory -Force -Path "$CursorDir\agents" | Out-Null
    $agents = @("forge-discovery", "forge-arch", "forge-plan", "forge-dev", "forge-verify", "forge-memory")
    foreach ($agent in $agents) {
        $src = "$FlowForgeRepo\ide\cursor\agents\$agent.md"
        if (Test-Path $src) {
            Copy-Item $src "$CursorDir\agents\"
            Write-Host "  ✓ Agent $agent.md copiado" -ForegroundColor Green
        }
    }
    $Installed = $true
}

# --- Detect Antigravity ---
$GeminiDir = "$env:USERPROFILE\.gemini"
if (Test-Path $GeminiDir) {
    Write-Host "[✓] Antigravity detectado" -ForegroundColor Green
    Write-Host "  Nota: Las reglas de Antigravity se instalan por proyecto." -ForegroundColor Yellow
    Write-Host "  Copiá la carpeta ide/antigravity/ a la raíz de tu proyecto." -ForegroundColor Yellow
    Write-Host "  O ejecutá en la raíz de tu proyecto:" -ForegroundColor Yellow
    Write-Host "    Copy-Item -Recurse .\ide\antigravity\. .\.agents\" -ForegroundColor Cyan
    $Installed = $true
}

# --- Detect VS Code ---
$VSCodeDir = "$env:USERPROFILE\.vscode"
if (Test-Path $VSCodeDir) {
    Write-Host "[✓] VS Code detectado" -ForegroundColor Green
    Copy-Item "$FlowForgeRepo\ide\vscode\copilot-instructions.md" "$VSCodeDir\"
    Write-Host "  ✓ Copilot instructions copiadas" -ForegroundColor Green
    $Installed = $true
}

# --- Summary ---
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
if ($Installed) {
    Write-Host "✅ Instalación completada" -ForegroundColor Green
    Write-Host "  Backups en: $BackupDir" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "📋 Próximos pasos:" -ForegroundColor Yellow
    Write-Host "  1. Reiniciá tu IDE"
    Write-Host "  2. Seleccioná el agente 'flowforge'"
    Write-Host "  3. Escribí: /flow-start CRUD de tareas"
} else {
    Write-Host "❌ No se detectó ningún IDE compatible" -ForegroundColor Red
}
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
