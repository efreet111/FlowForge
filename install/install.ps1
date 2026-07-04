# FlowForge stack bootstrap (Windows) — prerelease-aware version lookup
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
$GhHeaders  = @{ "User-Agent" = "flowforge-installer"; "Accept" = "application/vnd.github+json" }

function Get-ReleaseTag {
    param([string]$Channel)
    $releasesUrl = "https://api.github.com/repos/$Repo/releases"

    # Always use the list endpoint — /releases/latest returns 404 when every
    # release is a pre-release. We avoid @() wrapping because in PS 5.1
    # @(array) can double-wrap, giving Count=1 with a nested array as element.
    # Pipeline operations work correctly regardless of the response type.
    $response = $null
    try {
        $response = Invoke-RestMethod -Uri $releasesUrl -Headers $GhHeaders
    } catch {
        return $null
    }
    if (-not $response) { return $null }

    if ($Channel -eq "stable") {
        $release = $response | Where-Object { -not $_.prerelease } | Select-Object -First 1
        if ($release) { return $release.tag_name }
        # No stable release yet — fall back to latest pre-release
        $release = $response | Select-Object -First 1
        if (-not $release) { return $null }
        Write-Host "  Nota: sin release estable; usando pre-release $($release.tag_name)" -ForegroundColor Yellow
        return $release.tag_name
    } else {
        $release = $response | Where-Object { $_.prerelease } | Select-Object -First 1
        if (-not $release) { $release = $response | Select-Object -First 1 }
        if ($release) { return $release.tag_name }
        return $null
    }
}

# ── Obtener versión desde GitHub ─────────────────────────────────────────────
if (-not $Version) {
    Write-Host "Buscando ultima version (canal: $Channel)..."
    try {
        $Version = Get-ReleaseTag -Channel $Channel
    } catch {
        Write-Error "No se pudo obtener la version desde GitHub: $_"
        Write-Host "Intenta guardar el script y ejecutar: .\install.ps1 -Version v0.1.0-alpha.6"
        exit 1
    }
}

if (-not $Version) {
    Write-Error "No se encontro version para el canal '$Channel'."
    Write-Host "Intenta: .\install.ps1 -Version v0.1.0-alpha.6"
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
# Si hay consola interactiva disponible, lanzar el wizard interactivo.
# Si NO (CI/Docker/pipeline), pasar --yes para headless mode.
Write-Host ""
Write-Host "Iniciando wizard de instalación..."
if ([Environment]::UserInteractive) {
    # Consola interactiva — wizard preguntará qué instalar
    & $BinaryDest install
} else {
    # Sin consola interactiva — modo headless con defaults (ambos componentes)
    & $BinaryDest install --yes
}
