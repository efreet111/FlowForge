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

        if (!TryParseVersion(installedVersion, out var installed)) return null;
        if (!TryParseVersion(minVersion, out var minimum)) return null;

        if (installed < minimum)
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

        if (!TryParseVersion(currentInstallerVersion, out var current)) return null;
        if (!TryParseVersion(minVersion, out var minimum)) return null;

        if (current < minimum)
        {
            return $"Este installer (v{currentInstallerVersion}) está desactualizado. " +
                   $"Se requiere v{minVersion} o superior. " +
                   $"Actualizá con: flowforge update --self";
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

    static bool TryParseVersion(string raw, out Version version)
    {
        // Normalizar "0.3.0-alpha.1" → "0.3.0" para comparación numérica
        var normalized = raw.Split('-')[0];
        return Version.TryParse(normalized, out version!);
    }
}
