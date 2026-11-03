using FlowForge.Installer.Models;

namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Descarga el manifest.yaml de GitHub en runtime.
/// Esto permite actualizar reglas de compatibilidad sin recompilar el installer.
///
/// URL del manifest:
/// https://raw.githubusercontent.com/efreet111/FlowForge/main/install/manifest.yaml
/// </summary>
public sealed class ManifestClient(HttpClient http, InstallerLogger log)
{
    const string ManifestUrl =
        "https://raw.githubusercontent.com/efreet111/FlowForge/main/install/manifest.yaml";

    /// <summary>
    /// Intenta descargar el manifest remoto.
    /// Si falla (sin red, timeout), devuelve el manifest por defecto (no bloquea la instalación).
    /// </summary>
    public async Task<RemoteManifest> FetchAsync(CancellationToken ct = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, ManifestUrl);
            req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5)); // No bloquear más de 5s

            using var resp = await http.SendAsync(req, cts.Token);
            if (!resp.IsSuccessStatusCode)
            {
                log.Warn($"ManifestClient: HTTP {(int)resp.StatusCode} — usando manifest por defecto");
                return RemoteManifest.Default;
            }

            var yaml = await resp.Content.ReadAsStringAsync(cts.Token);
            var manifest = ParseYaml(yaml);
            manifest.IsRemote = true;
            log.Info($"ManifestClient: manifest remoto cargado (installer_version={manifest.InstallerVersion})");
            return manifest;
        }
        catch (OperationCanceledException)
        {
            log.Warn("ManifestClient: timeout al obtener manifest — usando defaults");
            return RemoteManifest.Default;
        }
        catch (Exception ex)
        {
            log.Warn($"ManifestClient: error al obtener manifest — usando defaults. {ex.Message}");
            return RemoteManifest.Default;
        }
    }

    /// <summary>
    /// Verifica si la versión instalada de engram-dotnet es compatible
    /// con el manifest (campo requires.engram-dotnet).
    /// Devuelve null si es compatible, mensaje de error si no.
    /// </summary>
    public static string? CheckEngramCompatibility(RemoteManifest manifest, string installedVersion)
    {
        if (string.IsNullOrEmpty(installedVersion)) return null;

        // Parsear la constraint mínima ">=0.3.0" o ">=0.3.0, <2.0.0"
        var minVersion = ParseMinVersion(manifest.RequiresEngramDotnet);
        if (minVersion == null) return null;

        if (CompareSemVer(installedVersion, minVersion) is < 0)
        {
            return $"engram-dotnet {installedVersion} no es compatible. " +
                   $"Se requiere {manifest.RequiresEngramDotnet}. " +
                   $"Actualizá con: flowforge update";
        }

        return null;
    }

    /// <summary>
    /// Verifica si el installer actual es compatible con el manifest remoto.
    /// Si el manifest requiere una versión más nueva del installer, hay que auto-actualizarse.
    /// </summary>
    public static string? CheckInstallerCompatibility(RemoteManifest manifest, string currentInstallerVersion)
    {
        if (!manifest.IsRemote) return null; // Solo verificar con manifest remoto

        var minVersion = ParseMinVersion(manifest.RequiresInstaller);
        if (minVersion == null) return null;

        if (CompareSemVer(currentInstallerVersion, minVersion) is < 0)
        {
            return $"Este installer (v{currentInstallerVersion}) está desactualizado. " +
                   $"Se requiere v{minVersion} o superior. " +
                   $"Actualizá reinstalando el installer: " +
                   "curl -fsSL https://raw.githubusercontent.com/efreet111/FlowForge/main/install/install.sh | bash";
        }

        return null;
    }

    /// <summary>
    /// Verifica si todas las versiones instaladas son compatibles con el manifest.
    /// Returns null if all compatible, error message if any component is incompatible.
    /// </summary>
    public static string? CheckCrossComponentCompatibility(
        RemoteManifest manifest,
        Dictionary<string, string?> installedComponents)
    {
        // Check engram-dotnet
        if (installedComponents.TryGetValue("engram", out var engramVersion) && engramVersion != null)
        {
            var engramError = CheckEngramCompatibility(manifest, engramVersion);
            if (engramError != null) return engramError;
        }

        // Check installer
        if (installedComponents.TryGetValue("installer", out var installerVersion) && installerVersion != null)
        {
            var installerError = CheckInstallerCompatibility(manifest, installerVersion);
            if (installerError != null) return installerError;
        }

        return null;
    }

    // ── Parser YAML mínimo (sin dependencia externa para mantener AOT simple) ──

    static RemoteManifest ParseYaml(string yaml)
    {
        var manifest = new RemoteManifest();
        foreach (var line in yaml.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#') || !trimmed.Contains(':')) continue;

            var idx = trimmed.IndexOf(':');
            var key = trimmed[..idx].Trim().ToLowerInvariant();
            var val = trimmed[(idx + 1)..].Trim().Trim('"', '\'');

            switch (key)
            {
                case "installer_version": manifest.InstallerVersion     = val; break;
                case "engram-dotnet":     manifest.RequiresEngramDotnet = val; break;
                case "installer":         manifest.RequiresInstaller    = val; break;
            }
        }
        return manifest;
    }

    static string? ParseMinVersion(string constraint)
    {
        // ">=0.3.0" → "0.3.0"
        // ">=0.3.0, <2.0.0" → "0.3.0"
        if (string.IsNullOrEmpty(constraint)) return null;
        var parts = constraint.Split(',')[0].Trim();
        return parts.TrimStart('>', '=', '<', ' ');
    }

    /// <summary>
    /// Compara dos versiones semver incluyendo el sufijo prerelease
    /// (<c>0.1.0-alpha.13 &lt; 0.1.0-alpha.15</c>).
    /// <see cref="Version"/> descarta el sufijo, así que comparar sólo el core hacía
    /// que cualquier requisito dentro de la línea alpha fuese invisible
    /// (el gate de compatibilidad era un no-op — detectado 2026-10-30).
    /// Devuelve null si alguna versión no se puede parsear.
    /// </summary>
    static int? CompareSemVer(string left, string right)
    {
        if (!TrySplitSemVer(left, out var lc, out var lp)) return null;
        if (!TrySplitSemVer(right, out var rc, out var rp)) return null;

        var cmp = lc.CompareTo(rc);
        if (cmp != 0) return cmp;

        // Una release es mayor que cualquier prerelease de la misma base.
        if (lp.Length == 0 && rp.Length == 0) return 0;
        if (lp.Length == 0) return 1;
        if (rp.Length == 0) return -1;

        var shared = Math.Min(lp.Length, rp.Length);
        for (var i = 0; i < shared; i++)
        {
            var leftNumeric = long.TryParse(lp[i], out var ln);
            var rightNumeric = long.TryParse(rp[i], out var rn);
            var partCmp = leftNumeric && rightNumeric
                ? ln.CompareTo(rn)
                : string.CompareOrdinal(lp[i], rp[i]);

            if (partCmp != 0) return Math.Sign(partCmp);
        }

        return lp.Length.CompareTo(rp.Length);
    }

    static bool TrySplitSemVer(string raw, out Version core, out string[] prerelease)
    {
        core = new Version(0, 0, 0);
        prerelease = [];

        if (string.IsNullOrWhiteSpace(raw)) return false;

        var text = raw.Trim();
        var dash = text.IndexOf('-');
        var coreText = dash < 0 ? text : text[..dash];
        var preText = dash < 0 ? string.Empty : text[(dash + 1)..];

        if (!Version.TryParse(coreText, out var parsed)) return false;

        core = parsed;
        prerelease = preText.Length == 0 ? [] : preText.Split('.');
        return true;
    }
}
