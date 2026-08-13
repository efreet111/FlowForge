using System.Text.Json;
using FlowForge.Installer.Infrastructure;

namespace FlowForge.Installer.Update;

/// <summary>
/// Factory that creates managed-paths sidecar instances for each IDE destination.
/// Generalizes the OpenCode-only ManagedPathsSidecar pattern to all IDEs.
/// </summary>
public sealed class ManagedPathsSidecarFactory
{
    /// <summary>
    /// Gets the sidecar path for a given IDE.
    /// </summary>
    public static string GetSidecarPath(string ide)
    {
        return ide.ToLowerInvariant() switch
        {
            "opencode" => PathHelper.OpenCodeSidecarPath,
            "cursor" => PathHelper.CursorSidecarPath,
            "antigravity" => PathHelper.AntigravitySidecarPath,
            "vs code" or "vscode" or "copilot" => PathHelper.VsCodeSidecarPath,
            "kilo" => PathHelper.KiloSidecarPath,
            _ => throw new ArgumentException($"Unknown IDE: {ide}", nameof(ide))
        };
    }

    /// <summary>
    /// Writes managed paths to the sidecar file for a given IDE.
    /// </summary>
    public static void WriteSidecar(string ide, IEnumerable<string> managedPaths)
    {
        var sidecarPath = GetSidecarPath(ide);
        var dir = Path.GetDirectoryName(sidecarPath);
        if (dir != null)
            Directory.CreateDirectory(dir);

        var array = managedPaths.ToArray();
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(sidecarPath, JsonSerializer.Serialize(array, options));
    }

    /// <summary>
    /// Reads managed paths from the sidecar file for a given IDE.
    /// </summary>
    public static string[] ReadSidecar(string ide)
    {
        var sidecarPath = GetSidecarPath(ide);
        if (!File.Exists(sidecarPath))
            return [];

        try
        {
            var text = File.ReadAllText(sidecarPath);
            return JsonSerializer.Deserialize<string[]>(text) ?? [];
        }
        catch
        {
            return [];
        }
    }

    /// <summary>
    /// Checks if a given JSON path is managed by FlowForge for a specific IDE.
    /// </summary>
    public static bool IsManaged(string ide, string jsonPath)
    {
        var managed = ReadSidecar(ide);
        return managed.Contains(jsonPath, StringComparer.OrdinalIgnoreCase);
    }
}
