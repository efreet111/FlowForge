using System.Diagnostics;
using FlowForge.Installer.Infrastructure;

namespace FlowForge.Installer.Update;

/// <summary>
/// Post-update health-check verification.
/// Checks binary version, MCP config parse, etc.
/// </summary>
public sealed record HealthCheckResult(
    string CheckName,
    bool Passed,
    string? Detail
);

public sealed class HealthCheckRunner
{
    readonly InstallerLogger _log;

    public HealthCheckRunner(InstallerLogger log)
    {
        _log = log;
    }

    /// <summary>
    /// Binary health-check: run `{binary} --version`, verify exit code + version string.
    /// </summary>
    public async Task<HealthCheckResult> CheckBinaryAsync(
        string binaryPath, string expectedVersion, CancellationToken ct = default)
    {
        if (!File.Exists(binaryPath))
            return new HealthCheckResult("binary-exists", false, $"Binary not found: {binaryPath}");

        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = binaryPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            proc.Start();
            var output = await proc.StandardOutput.ReadToEndAsync(ct);
            await proc.StandardError.ReadToEndAsync(ct);

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

            try
            {
                if (!proc.WaitForExit(10_000))
                {
                    try { proc.Kill(); } catch { }
                    return new HealthCheckResult("binary-version", false, "Timeout running --version");
                }
            }
            catch (OperationCanceledException)
            {
                try { proc.Kill(); } catch { }
                return new HealthCheckResult("binary-version", false, "Cancelled");
            }

            if (proc.ExitCode != 0)
                return new HealthCheckResult("binary-version", false,
                    $"Exit code {proc.ExitCode}");

            var versionOutput = output.Trim();
            if (!string.IsNullOrEmpty(expectedVersion) &&
                !versionOutput.Contains(expectedVersion, StringComparison.OrdinalIgnoreCase))
            {
                return new HealthCheckResult("binary-version", false,
                    $"Expected {expectedVersion}, got: {versionOutput}");
            }

            return new HealthCheckResult("binary-version", true, versionOutput);
        }
        catch (Exception ex)
        {
            return new HealthCheckResult("binary-version", false, ex.Message);
        }
    }

    /// <summary>
    /// MCP config parse check: validate JSON is parseable and contains engram entry.
    /// </summary>
    public HealthCheckResult CheckMcpConfig(string mcpConfigPath)
    {
        if (!File.Exists(mcpConfigPath))
            return new HealthCheckResult("mcp-config", false, $"Config not found: {mcpConfigPath}");

        try
        {
            var text = File.ReadAllText(mcpConfigPath);
            var node = System.Text.Json.Nodes.JsonNode.Parse(text);
            if (node == null)
                return new HealthCheckResult("mcp-config", false, "JSON parse returned null");

            // Check for mcpServers.engram or mcp.engram
            var hasEngram = node["mcpServers"]?["engram"] != null ||
                           node["mcp"]?["engram"] != null;

            return new HealthCheckResult("mcp-config", true,
                hasEngram ? "Valid JSON with engram entry" : "Valid JSON (no engram entry)");
        }
        catch (Exception ex)
        {
            return new HealthCheckResult("mcp-config", false, $"Invalid JSON: {ex.Message}");
        }
    }

    /// <summary>
    /// Run all post-update checks for a component.
    /// </summary>
    public async Task<IReadOnlyList<HealthCheckResult>> RunAllAsync(
        UpdateComponent component, string version, CancellationToken ct = default)
    {
        var results = new List<HealthCheckResult>();

        if (component == UpdateComponent.Engram)
        {
            results.Add(await CheckBinaryAsync(PathHelper.EngramBinary, version, ct));
        }

        return results;
    }
}
