using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Descarga binarios desde GitHub Releases y verifica SHA-256.
/// </summary>
public sealed class GitHubReleasesClient(HttpClient http, InstallerLogger log)
{
    const string EngramRepo    = "efreet111/engram-dotnet";
    const string FlowForgeRepo = "efreet111/FlowForge";

    /// <summary>
    /// Obtiene el tag de la última release estable de un repo.
    /// </summary>
    public async Task<string?> GetLatestVersionAsync(string repo, string channel, CancellationToken ct = default)
    {
        try
        {
            var url = channel == "stable"
                ? $"https://api.github.com/repos/{repo}/releases/latest"
                : $"https://api.github.com/repos/{repo}/releases?per_page=10";

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");
            req.Headers.Add("Accept", "application/vnd.github+json");

            using var resp = await http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();

            var content = await resp.Content.ReadAsStringAsync(ct);

            if (channel == "stable")
            {
                var release = JsonSerializer.Deserialize(content, GitHubJsonContext.Default.GitHubRelease);
                return release?.TagName;
            }
            else
            {
                var releases = JsonSerializer.Deserialize(content, GitHubJsonContext.Default.GitHubReleaseArray);
                // beta: primer pre-release; nightly: primer pre-release con "nightly" en tag
                var match = channel == "nightly"
                    ? releases?.FirstOrDefault(r => r.Prerelease && r.TagName.Contains("nightly"))
                    : releases?.FirstOrDefault(r => r.Prerelease);
                return match?.TagName;
            }
        }
        catch (Exception ex)
        {
            log.Warn($"GitHubReleasesClient.GetLatestVersion: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Descarga el binario engram-dotnet desde GitHub Releases.
    /// </summary>
    public async Task<bool> DownloadEngramAsync(string version, string destPath, CancellationToken ct = default)
    {
        var assetName = GetEngramAssetName();
        var url = $"https://github.com/{EngramRepo}/releases/download/{version}/{assetName}";
        return await DownloadAndVerifyAsync(url, assetName, EngramRepo, version, destPath, ct);
    }

    /// <summary>
    /// Descarga la librería nativa SQLite (e_sqlite3.so / e_sqlite3.dll)
    /// desde GitHub Releases. Retorna true si ya existe o se descargó correctamente.
    /// </summary>
    public async Task<bool> DownloadNativeSqliteLibAsync(string version, CancellationToken ct = default)
    {
        var assetName = OperatingSystem.IsWindows() ? "e_sqlite3.dll" : "e_sqlite3.so";
        var destPath = Path.Combine(PathHelper.EngramBinDir, assetName);

        // Si ya existe (release anterior o symlink), no descargar
        if (File.Exists(destPath))
        {
            log.Info($"Native lib ya existe: {destPath}");
            return true;
        }

        var url = $"https://github.com/{EngramRepo}/releases/download/{version}/{assetName}";
        log.Info($"Descargando native lib: {assetName}");
        return await DownloadAndVerifyAsync(url, assetName, EngramRepo, version, destPath, ct);
    }

    static string GetEngramAssetName() =>
        OperatingSystem.IsWindows() ? "engram-win-x64.exe" : "engram-linux-x64";

    /// <summary>
    /// Descarga un asset y verifica SHA-256 contra el checksum publicado en GitHub Releases.
    /// </summary>
    async Task<bool> DownloadAndVerifyAsync(
        string url, string assetName, string repo, string version,
        string destPath, CancellationToken ct)
    {
        log.Info($"Download: {url}");
        try
        {
            var dir = Path.GetDirectoryName(destPath);
            if (dir != null) Directory.CreateDirectory(dir);

            var tmpPath = destPath + ".tmp";
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");

            using var resp = await http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            await using (var stream = await resp.Content.ReadAsStreamAsync(ct))
            await using (var file = File.Create(tmpPath))
            {
                await stream.CopyToAsync(file, ct);
            }

            // Verify SHA-256 si hay checksum disponible
            var expectedSha = await FetchChecksumAsync(repo, version, assetName, ct);
            if (expectedSha != null)
            {
                var actualSha = ComputeSha256(tmpPath);
                if (!string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase))
                {
                    log.Error($"Checksum mismatch para {assetName}: esperado {expectedSha}, obtenido {actualSha}");
                    File.Delete(tmpPath);
                    return false;
                }
                log.Info($"SHA-256 OK: {assetName}");
            }
            else
            {
                log.Warn($"No se encontró checksum para {assetName} — descarga sin verificación");
            }

            File.Move(tmpPath, destPath, overwrite: true);

            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(destPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

            log.Info($"Instalado: {destPath}");
            return true;
        }
        catch (Exception ex)
        {
            log.Error($"DownloadAndVerify error: {ex.Message}");
            return false;
        }
    }

    async Task<string?> FetchChecksumAsync(string repo, string version, string assetName, CancellationToken ct)
    {
        // Convención: archivo {assetName}.sha256 en el release
        var url = $"https://github.com/{repo}/releases/download/{version}/{assetName}.sha256";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");
            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var content = await resp.Content.ReadAsStringAsync(ct);
            // Formato: "<sha256>  filename" o solo "<sha256>"
            return content.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }
}

// ── GitHub API models (AOT-safe) ─────────────────────────────────────────────

public sealed class GitHubRelease
{
    [JsonPropertyName("tag_name")]
    public string TagName { get; set; } = "";

    [JsonPropertyName("prerelease")]
    public bool Prerelease { get; set; }

    [JsonPropertyName("draft")]
    public bool Draft { get; set; }
}

[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(GitHubRelease[]))]
public partial class GitHubJsonContext : JsonSerializerContext { }
