using System.Diagnostics;
using FlowForge.Installer.Infrastructure;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Subprocess bridge to the engram CLI (FR-003).
/// Uses ArgumentList (not Arguments string) to prevent shell injection (NFR-006).
/// Follows HealthCheckRunner pattern for subprocess invocation.
/// </summary>
public sealed class EngramSubprocess
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Retrieve all onboarding data from engram CLI.
    /// Fixed query set:
    /// 1. engram context &lt;project&gt;
    /// 2. engram search "project" --type decision --project &lt;p&gt; --limit 10
    /// 3. engram search "project" --type pattern --project &lt;p&gt; --limit 10
    /// </summary>
    public async Task<EngramRawOutput> RetrieveAllAsync(
        string project,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        var t = timeout ?? DefaultTimeout;

        // Run all three queries in parallel for performance (NFR-001)
        var contextTask = InvokeAsync(
            ["context", project], t, ct);

        var decisionTask = InvokeAsync(
            ["search", "project", "--type", "decision", "--project", project, "--limit", "10"], t, ct);

        var patternTask = InvokeAsync(
            ["search", "project", "--type", "pattern", "--project", project, "--limit", "10"], t, ct);

        await Task.WhenAll(contextTask, decisionTask, patternTask);

        var (contextOut, _, _) = await contextTask;
        var (decisionOut, _, _) = await decisionTask;
        var (patternOut, _, _) = await patternTask;

        return new EngramRawOutput(contextOut, decisionOut, patternOut);
    }

    /// <summary>
    /// Single subprocess invocation. Returns (stdout, stderr, exitCode).
    /// Uses ArgumentList (no shell interpolation — NFR-006).
    /// </summary>
    public async Task<(string StdOut, string StdErr, int ExitCode)> InvokeAsync(
        string[] arguments,
        TimeSpan timeout,
        CancellationToken ct = default)
    {
        var binary = PathHelper.EngramBinary;

        using var proc = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = binary,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            }
        };

        // Use ArgumentList for security (NFR-006 — no shell interpolation)
        foreach (var arg in arguments)
        {
            proc.StartInfo.ArgumentList.Add(arg);
        }

        proc.Start();

        // Read stdout and stderr concurrently to avoid deadlock
        var stdoutTask = proc.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = proc.StandardError.ReadToEndAsync(ct);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await Task.WhenAll(stdoutTask, stderrTask);

            if (!proc.WaitForExit((int)timeout.TotalMilliseconds))
            {
                try { proc.Kill(); } catch { }
                return (string.Empty, $"Timeout after {timeout.TotalSeconds}s", -1);
            }

            // Ensure async reads complete
            await stdoutTask;
            await stderrTask;

            return (stdoutTask.Result, stderrTask.Result, proc.ExitCode);
        }
        catch (OperationCanceledException)
        {
            try { proc.Kill(); } catch { }
            return (string.Empty, "Cancelled or timed out", -1);
        }
        catch (Exception ex)
        {
            return (string.Empty, ex.Message, -1);
        }
    }
}
