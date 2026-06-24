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

    if ($Channel -eq "stable") {
        # /releases/latest 404s when every release is marked prerelease (alpha-only repos).
        try {
            $latest = Invoke-RestMethod -Uri "$releasesUrl/latest" -Headers $GhHeaders
            if ($latest.tag_name) { return $latest.tag_name }
        } catch {
            # fall through to list
        }
        $all = @(Invoke-RestMethod -Uri $releasesUrl -Headers $GhHeaders)
        $stable = @($all | Where-Object { -not $_.prerelease } | Select-Object -First 1)
        if ($stable.Count -gt 0) { return $stable[0].tag_name }
        if ($all.Count -gt 0) {
            $tag = ($all | Select-Object -First 1).tag_name
            Write-Host "  Nota: sin release estable; usando pre-release $tag" -ForegroundColor Yellow
            return $tag
        }
    } else {
        $all = @(Invoke-RestMethod -Uri $releasesUrl -Headers $GhHeaders)
        $pre = @($all | Where-Object { $_.prerelease } | Select-Object -First 1)
        if ($pre.Count -gt 0) { return $pre[0].tag_name }
        if ($all.Count -gt 0) { return ($all | Select-Object -First 1).tag_name }
    }
    return $null
}

# ── Obtener versión desde GitHub ─────────────────────────────────────────────
if (-not $Version) {
    Write-Host "Buscando ultima version (canal: $Channel)..."
    try {
        $Version = Get-ReleaseTag -Channel $Channel
    } catch {
        Write-Error "No se pudo obtener la version desde GitHub: $_"
        Write-Host "Intenta guardar el script y ejecutar: .\install.ps1 -Version v0.1.0-alpha.2"
        exit 1
    }
}

if (-not $Version) {
    Write-Error "No se encontro version para el canal '$Channel'."
    Write-Host "Intenta: .\install.ps1 -Version v0.1.0-alpha.2"
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
