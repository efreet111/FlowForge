using System.Diagnostics;

namespace FlowForge.Installer.Update;

/// <summary>
/// Information about a running engram process.
/// </summary>
public sealed record EngramProcessInfo(int Pid, string ProcessName);

/// <summary>
/// Detects running engram processes (MCP servers) to prevent binary swap conflicts.
/// Cross-platform: uses Process.GetProcessesByName on all platforms.
/// </summary>
public sealed class EngramProcessChecker
{
    /// <summary>
    /// Detects running engram processes.
    /// Returns list of (PID, process name) tuples.
    /// </summary>
    public IReadOnlyList<EngramProcessInfo> DetectRunningProcesses()
    {
        var results = new List<EngramProcessInfo>();

        try
        {
            // Try both "engram" and "engram.exe" (Windows)
            var processNames = OperatingSystem.IsWindows()
                ? new[] { "engram", "engram.exe" }
                : new[] { "engram" };

            foreach (var name in processNames)
            {
                var processes = Process.GetProcessesByName(name);
                foreach (var proc in processes)
                {
                    try
                    {
                        results.Add(new EngramProcessInfo(proc.Id, proc.ProcessName));
                    }
                    finally
                    {
                        proc.Dispose();
                    }
                }
            }
        }
        catch (Exception)
        {
            // Best-effort: process enumeration can fail on some platforms
        }

        return results;
    }
}
