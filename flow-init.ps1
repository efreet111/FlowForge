# FlowForge — flow-init (Windows PowerShell)
# Scaffolds a new project with FlowDoc docs structure + FlowForge IDE packs.
#
# Usage:
#   .\flow-init.ps1 -ProjectPath "E:\Proyectos\mi-app"
#   .\flow-init.ps1 -ProjectPath "E:\Proyectos\mi-app" -ProjectName "Mi App"
#   .\flow-init.ps1 -ProjectPath "E:\Proyectos\mi-app" -Force
#
# What it creates:
#   AGENTS.md, .flowforge.json, docs/ (PRD, DEVELOPMENT, tasks/, architecture/,
#   templates/), .ai-work/, QUICKSTART.project.md
#   Then runs ide/install.ps1 -ProjectPath to install IDE packs.
#
# Related:
#   ADR-004: docs/decisions/ADR-004-flowdoc-integration.md
#   ADR-002: docs/decisions/ADR-002-scaffold-doc-policy.md

param(
    [Parameter(Mandatory=$true)]
    [string]$ProjectPath,

    [string]$ProjectName = "",

    [switch]$Force
)

$ErrorActionPreference = "Stop"

# ── Locate FlowForge repo ─────────────────────────────────────────────────────
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if (-not $ScriptDir) { $ScriptDir = (Get-Location).Path }

$FlowForgeRepo = $ScriptDir
if (-not (Test-Path (Join-Path $FlowForgeRepo "AGENTS.md"))) {
    Write-Host "ERROR: Could not locate FlowForge repo. Run this script from the FlowForge directory." -ForegroundColor Red
    exit 1
}

$Templates       = Join-Path $FlowForgeRepo "templates\project"
$VersionFile     = Join-Path $FlowForgeRepo "VERSION.md"
$FlowForgeVersion = if (Test-Path $VersionFile) { (Get-Content $VersionFile -Raw).Trim() } else { "0.4.1" }
$Today           = (Get-Date).ToString("yyyy-MM-dd")

Write-Host "========================================" -ForegroundColor Blue
Write-Host "  FlowForge — flow-init" -ForegroundColor Blue
Write-Host "========================================" -ForegroundColor Blue
Write-Host ""

# ── Validate target directory ─────────────────────────────────────────────────
if (Test-Path $ProjectPath) {
    $nonGit = Get-ChildItem $ProjectPath -Force | Where-Object { $_.Name -ne ".git" }
    if ($nonGit -and -not $Force) {
        Write-Host "ERROR: Directory is not empty: $ProjectPath" -ForegroundColor Red
        Write-Host "Use -Force to scaffold into an existing directory."
        exit 1
    }
} else {
    New-Item -ItemType Directory -Force -Path $ProjectPath | Out-Null
    Write-Host "[OK] Created directory: $ProjectPath" -ForegroundColor Green
}

$Target = (Resolve-Path $ProjectPath).Path

# ── Derive project name ───────────────────────────────────────────────────────
if ([string]::IsNullOrWhiteSpace($ProjectName)) {
    $ProjectName = Split-Path -Leaf $Target
}
Write-Host "[OK] Project name: $ProjectName" -ForegroundColor Green
Write-Host "[OK] Target: $Target" -ForegroundColor Green
Write-Host ""

# ── Helper: replace placeholders in a file ────────────────────────────────────
function Replace-Placeholders {
    param([string]$FilePath)
    if (-not (Test-Path $FilePath)) { return }
    $content = Get-Content -Path $FilePath -Raw -Encoding UTF8
    $content = $content.Replace("__PROJECT_NAME__", $ProjectName)
    $content = $content.Replace("__FLOWFORGE_VERSION__", $FlowForgeVersion)
    $content = $content.Replace("__DATE__", $Today)
    $content = $content.Replace("__PROJECT_PATH__", $Target)
    Set-Content -Path $FilePath -Value $content -Encoding UTF8 -NoNewline
}

# ── Copy templates ────────────────────────────────────────────────────────────
Write-Host "[*] Copying templates..." -ForegroundColor Blue

# AGENTS.md
Copy-Item (Join-Path $Templates "AGENTS.md.template") (Join-Path $Target "AGENTS.md") -Force
Replace-Placeholders (Join-Path $Target "AGENTS.md")
Write-Host "  OK AGENTS.md" -ForegroundColor Green

# .flowforge.json
Copy-Item (Join-Path $Templates ".flowforge.json.template") (Join-Path $Target ".flowforge.json") -Force
Replace-Placeholders (Join-Path $Target ".flowforge.json")
Write-Host "  OK .flowforge.json" -ForegroundColor Green

# QUICKSTART.project.md
Copy-Item (Join-Path $Templates "QUICKSTART.project.md") (Join-Path $Target "QUICKSTART.project.md") -Force
Replace-Placeholders (Join-Path $Target "QUICKSTART.project.md")
Write-Host "  OK QUICKSTART.project.md" -ForegroundColor Green

# docs/ structure
$docsDirs = @(
    "docs\architecture\adr",
    "docs\architecture\rfc",
    "docs\tasks",
    "docs\templates"
)
foreach ($d in $docsDirs) {
    New-Item -ItemType Directory -Force -Path (Join-Path $Target $d) | Out-Null
}

foreach ($f in @("PRD.md", "DEVELOPMENT.md")) {
    Copy-Item (Join-Path $Templates "docs\$f") (Join-Path $Target "docs\$f") -Force
    Replace-Placeholders (Join-Path $Target "docs\$f")
}
Write-Host "  OK docs/PRD.md + docs/DEVELOPMENT.md" -ForegroundColor Green

Copy-Item (Join-Path $Templates "docs\tasks\HU-001-example.md") (Join-Path $Target "docs\tasks\") -Force
Replace-Placeholders (Join-Path $Target "docs\tasks\HU-001-example.md")
Write-Host "  OK docs/tasks/HU-001-example.md" -ForegroundColor Green

foreach ($tmpl in @("HU-template.md", "adr-template.md", "rfc-template.md")) {
    Copy-Item (Join-Path $Templates "docs\templates\$tmpl") (Join-Path $Target "docs\templates\") -Force
}
Write-Host "  OK docs/templates/ (HU, ADR, RFC templates)" -ForegroundColor Green

# .gitkeep for empty tracked folders
New-Item -ItemType File -Force -Path (Join-Path $Target "docs\architecture\adr\.gitkeep") | Out-Null
New-Item -ItemType File -Force -Path (Join-Path $Target "docs\architecture\rfc\.gitkeep") | Out-Null
Write-Host "  OK docs/architecture/ (adr/ + rfc/)" -ForegroundColor Green

# .ai-work/
New-Item -ItemType Directory -Force -Path (Join-Path $Target ".ai-work") | Out-Null
New-Item -ItemType File -Force -Path (Join-Path $Target ".ai-work\.gitkeep") | Out-Null
Write-Host "  OK .ai-work/" -ForegroundColor Green

Write-Host ""

# ── Install IDE packs ─────────────────────────────────────────────────────────
Write-Host "[*] Installing FlowForge IDE packs..." -ForegroundColor Blue
$InstallScript = Join-Path $FlowForgeRepo "ide\install.ps1"
if (Test-Path $InstallScript) {
    try {
        & $InstallScript -ProjectPath $Target
    } catch {
        Write-Host "  ! IDE installer finished with warnings: $_" -ForegroundColor Yellow
        Write-Host "  ! Templates are intact. Re-run ide\install.ps1 manually if needed." -ForegroundColor Yellow
    }
} else {
    Write-Host "  ! ide\install.ps1 not found — run it manually:" -ForegroundColor Yellow
    Write-Host "    .\ide\install.ps1 -ProjectPath `"$Target`"" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Blue
Write-Host "flow-init complete!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. Edit docs\PRD.md          — describe your product"
Write-Host "  2. Edit AGENTS.md            — fill in your stack"
Write-Host "  3. Edit .flowforge.json      — set engram project name if needed"
Write-Host "  4. Create your first HU:"
Write-Host "       copy docs\templates\HU-template.md docs\tasks\HU-001-your-feature.md"
Write-Host "  5. Reload your IDE, then run:"
Write-Host "       /flow-start HU-001-your-feature"
Write-Host ""
Write-Host "  See QUICKSTART.project.md for the full guide."
Write-Host "========================================"
Write-Host "  Project: $ProjectName" -ForegroundColor Green
Write-Host "  Path:    $Target" -ForegroundColor Green
Write-Host "  FlowDoc: flowdoc@1.1 (embedded)" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Blue
