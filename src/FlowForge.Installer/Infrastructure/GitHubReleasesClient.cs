using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Descarga binarios desde GitHub Releases y verifica SHA-256.
/// </summary>
public sealed class GitHubReleasesClient
{
    const string EngramRepo    = "efreet111/engram-dotnet";
    const string FlowForgeRepo = "efreet111/FlowForge";

    readonly HttpClient _http;
    readonly InstallerLogger _log;
    readonly int _downloadTimeoutSeconds;

    /// <summary>
    /// Inicializa el cliente con timeouts configurables.
    /// </summary>
    public GitHubReleasesClient(HttpClient http, InstallerLogger log, int downloadTimeoutSeconds = 300)
    {
        _http = http;
        _log  = log;
        var apiTimeoutSeconds = ParseTimeoutEnv("FLOWFORGE_API_TIMEOUT_SECONDS", 30);
        _http.Timeout = TimeSpan.FromSeconds(apiTimeoutSeconds);
        _downloadTimeoutSeconds = Math.Max(1, ParseTimeoutEnv("FLOWFORGE_DOWNLOAD_TIMEOUT_SECONDS", downloadTimeoutSeconds));
    }

    static int ParseTimeoutEnv(string envName, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(envName);
        return int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }

    /// <summary>
    /// Obtiene el tag de la última release estable de un repo.
    /// </summary>
    public async Task<string?> GetLatestVersionAsync(string repo, string channel, CancellationToken ct = default)
    {
        try
        {
            if (string.Equals(repo, EngramRepo, StringComparison.Ordinal))
                return await GetLatestEngramVersionAsync(channel, ct);

            if (channel == "stable")
            {
                var url = $"https://api.github.com/repos/{repo}/releases/latest";
                using var req = new HttpRequestMessage(HttpMethod.Get, url);
                req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");
                req.Headers.Add("Accept", "application/vnd.github+json");

                using var resp = await _http.SendAsync(req, ct);
                resp.EnsureSuccessStatusCode();

                var content = await resp.Content.ReadAsStringAsync(ct);
                var release = JsonSerializer.Deserialize(content, GitHubJsonContext.Default.GitHubRelease);
                return release?.TagName;
            }

            var releases = await FetchReleasesPageAsync(repo, 10, ct);
            if (releases == null)
                return null;

            var match = channel == "nightly"
                ? releases.FirstOrDefault(r => r.Prerelease && r.TagName.Contains("nightly", StringComparison.OrdinalIgnoreCase))
                : releases.FirstOrDefault(r => r.Prerelease);
            return match?.TagName;
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            var timeoutSec = _http.Timeout.TotalSeconds;
            _log.Error($"GitHub API timeout tras {timeoutSec} segundos.");
            throw new TimeoutException($"GitHub API request timed out after {timeoutSec} seconds.", ex);
        }
        catch (Exception ex)
        {
            _log.Warn($"GitHubReleasesClient.GetLatestVersion: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// engram-dotnet v1.3.0+ may publish release notes without assets; skip empty releases.
    /// </summary>
    async Task<string?> GetLatestEngramVersionAsync(string channel, CancellationToken ct)
    {
        var releases = await FetchReleasesPageAsync(EngramRepo, 20, ct);
        if (releases == null || releases.Length == 0)
            return null;

        var assetName = GetEngramAssetName();
        foreach (var release in releases)
        {
            if (release.Draft)
                continue;

            var matchesChannel = channel switch
            {
                "stable" => !release.Prerelease,
                "nightly" => release.Prerelease && release.TagName.Contains("nightly", StringComparison.OrdinalIgnoreCase),
                _ => release.Prerelease,
            };
            if (!matchesChannel)
                continue;

            if (await ReleaseAssetExistsAsync(release.TagName, assetName, ct))
            {
                _log.Info($"engram-dotnet: usando {release.TagName} ({assetName} disponible)");
                return release.TagName;
            }

            _log.Warn($"engram-dotnet: omitiendo {release.TagName} — asset {assetName} no publicado");
        }

        return null;
    }

    async Task<GitHubRelease[]?> FetchReleasesPageAsync(string repo, int perPage, CancellationToken ct)
    {
        var url = $"https://api.github.com/repos/{repo}/releases?per_page={perPage}";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");
        req.Headers.Add("Accept", "application/vnd.github+json");

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        var content = await resp.Content.ReadAsStringAsync(ct);
        return JsonSerializer.Deserialize(content, GitHubJsonContext.Default.GitHubReleaseArray);
    }

    async Task<bool> ReleaseAssetExistsAsync(string version, string assetName, CancellationToken ct)
    {
        var url = $"https://github.com/{EngramRepo}/releases/download/{version}/{assetName}";
        using var req = new HttpRequestMessage(HttpMethod.Head, url);
        req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");
        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        return resp.IsSuccessStatusCode;
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
    /// desde GitHub Releases.
    /// </summary>
    public async Task<bool> DownloadNativeSqliteLibAsync(string version, CancellationToken ct = default)
    {
        var assetName = OperatingSystem.IsWindows() ? "e_sqlite3.dll" : "libe_sqlite3.so";
        var destPath = Path.Combine(PathHelper.EngramBinDir, assetName);

        if (File.Exists(destPath))
        {
            _log.Info($"Native lib ya existe: {destPath}");
            return true;
        }

        var url = $"https://github.com/{EngramRepo}/releases/download/{version}/{assetName}";
        _log.Info($"Descargando native lib: {assetName}");
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
        _log.Info($"Download: {url}");
        var tmpPath = destPath + ".tmp";
        try
        {
            var dir = Path.GetDirectoryName(destPath);
            if (dir != null) Directory.CreateDirectory(dir);

            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");

            using var downloadCts = new CancellationTokenSource(TimeSpan.FromSeconds(_downloadTimeoutSeconds));
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, downloadCts.Token);
            var linkedToken = linkedCts.Token;

            using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, linkedToken);
            resp.EnsureSuccessStatusCode();

            await using (var stream = await resp.Content.ReadAsStreamAsync(linkedToken))
            await using (var file = File.Create(tmpPath))
            {
                await stream.CopyToAsync(file, linkedToken);
            }

            var expectedSha = await FetchChecksumAsync(repo, version, assetName, linkedToken);
            if (expectedSha != null)
            {
                var actualSha = ComputeSha256(tmpPath);
                if (!string.Equals(actualSha, expectedSha, StringComparison.OrdinalIgnoreCase))
                {
                    _log.Error($"Checksum mismatch para {assetName}: esperado {expectedSha}, obtenido {actualSha}");
                    SafeDelete(tmpPath);
                    return false;
                }
                _log.Info($"SHA-256 OK: {assetName}");
            }
            else
            {
                _log.Warn($"No se encontró checksum para {assetName} — descarga sin verificación");
            }

            File.Move(tmpPath, destPath, overwrite: true);

            if (!OperatingSystem.IsWindows())
                File.SetUnixFileMode(destPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);

            _log.Info($"Instalado: {destPath}");
            return true;
        }
        catch (OperationCanceledException ex)
        {
            _log.Error($"DownloadAndVerify timeout: {ex.Message}");
            SafeDelete(tmpPath);
            return false;
        }
        catch (Exception ex)
        {
            _log.Error($"DownloadAndVerify error: {ex.Message}");
            SafeDelete(tmpPath);
            return false;
        }
    }

    async Task<string?> FetchChecksumAsync(string repo, string version, string assetName, CancellationToken ct)
    {
        var url = $"https://github.com/{repo}/releases/download/{version}/{assetName}.sha256";
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("User-Agent", "flowforge-installer/0.1.0");
            using var resp = await _http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;
            var content = await resp.Content.ReadAsStringAsync(ct);
            return content.Split(new char[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    static void SafeDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch { }
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
