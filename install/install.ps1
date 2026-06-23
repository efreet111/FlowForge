#Requires -Version 5.1
<#
.SYNOPSIS
  FlowForge Stack Installer — bootstrap script (Windows)

.DESCRIPTION
  Descarga el binario flowforge desde GitHub Releases y lo ejecuta.

.EXAMPLE
  iwr -useb https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.ps1 | iex
  .\install.ps1 -Channel beta
  .\install.ps1 -Version v0.1.0-alpha.1
#>
param(
    [string] $Channel = "stable",
    [string] $Version = ""
)
$ErrorActionPreference = "Stop"

$Repo       = "efreet111/FlowForge"
$InstallDir = Join-Path $env:LOCALAPPDATA "Programs\FlowForge"
$BinaryName = "flowforge-win-x64.exe"

# ── Obtener versión desde GitHub ─────────────────────────────────────────────
if (-not $Version) {
    Write-Host "Buscando última versión (canal: $Channel)..."
    try {
        if ($Channel -eq "stable") {
            $url     = "https://api.github.com/repos/$Repo/releases/latest"
            $headers = @{ "User-Agent" = "flowforge-installer"; "Accept" = "application/vnd.github+json" }
            $resp    = Invoke-RestMethod -Uri $url -Headers $headers
            $Version = $resp.tag_name
        } else {
            $url     = "https://api.github.com/repos/$Repo/releases"
            $headers = @{ "User-Agent" = "flowforge-installer"; "Accept" = "application/vnd.github+json" }
            $resp    = Invoke-RestMethod -Uri $url -Headers $headers
            $Version = ($resp | Where-Object { $_.prerelease } | Select-Object -First 1).tag_name
        }
    } catch {
        Write-Error "No se pudo obtener la versión desde GitHub: $_"
        Write-Host "Intentá: .\install.ps1 -Version v0.1.0-alpha.1"
        exit 1
    }
}

if (-not $Version) {
    Write-Error "No se encontró versión para el canal '$Channel'."
    exit 1
}

Write-Host "Instalando FlowForge $Version..."

# ── Descargar binario ─────────────────────────────────────────────────────────
$DownloadUrl = "https://github.com/$Repo/releases/download/$Version/$BinaryName"
$TmpFile     = Join-Path $env:TEMP "flowforge-$([guid]::NewGuid().ToString('N')).exe"

Write-Host "  Descargando: $DownloadUrl"
try {
    Invoke-WebRequest -Uri $DownloadUrl -OutFile $TmpFile -UseBasicParsing
} catch {
    Write-Error "Error al descargar: $_"
    exit 1
}

# ── Verificar SHA-256 ─────────────────────────────────────────────────────────
$ChecksumUrl = "$DownloadUrl.sha256"
try {
    $expectedLine = (Invoke-WebRequest -Uri $ChecksumUrl -UseBasicParsing).Content.Trim()
    $expectedSha  = $expectedLine.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)[0]
    $actualSha    = (Get-FileHash -Path $TmpFile -Algorithm SHA256).Hash.ToLower()
    if ($expectedSha -ne $actualSha) {
        Write-Error "SHA-256 no coincide. Descarga corrupta.`nEsperado: $expectedSha`nObtenido: $actualSha"
        Remove-Item $TmpFile -Force
        exit 1
    }
    Write-Host "  SHA-256 OK"
} catch {
    Write-Host "  WARN: No se pudo verificar checksum (continuando)"
}

# ── Instalar ──────────────────────────────────────────────────────────────────
if (-not (Test-Path $InstallDir)) {
    New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null
}
$BinaryDest = Join-Path $InstallDir "flowforge.exe"
Move-Item -Path $TmpFile -Destination $BinaryDest -Force
Write-Host "  Instalado: $BinaryDest"

# ── Agregar al PATH ───────────────────────────────────────────────────────────
$currentPath = [Environment]::GetEnvironmentVariable("PATH", "User")
if ($currentPath -notlike "*$InstallDir*") {
    [Environment]::SetEnvironmentVariable("PATH", "$currentPath;$InstallDir", "User")
    Write-Host "  PATH actualizado (reiniciá la terminal para aplicar)"
}

# ── Lanzar wizard ─────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "Iniciando wizard de instalación..."
& $BinaryDest install
