using System.Text.Json;

namespace FlowForge.Installer.Modules.OpenCode;

public sealed class ManagedPathsSidecar
{
    static readonly string[] LegacyDefaults = { "mcp.engram" };

    /// <summary>Default constructor — uses the OpenCode sidecar path.</summary>
    public ManagedPathsSidecar()
        : this(FlowForge.Installer.Infrastructure.PathHelper.OpenCodeSidecarPath)
    {
    }

    /// <summary>Constructor with custom path (task 8.2).</summary>
    public ManagedPathsSidecar(string customPath)
    {
        Path = customPath ?? throw new ArgumentNullException(nameof(customPath));
    }

    public string Path { get; }

    public string[] ReadManagedPaths()
    {
        if (!File.Exists(Path))
            return LegacyDefaults;

        try
        {
            var text = File.ReadAllText(Path);
            var data = JsonSerializer.Deserialize<string[]>(text);
            if (data == null || data.Length == 0)
                return LegacyDefaults;
            return data;
        }
        catch
        {
            return LegacyDefaults;
        }
    }

    public void WriteManagedPaths(IEnumerable<string> paths)
    {
        var dir = System.IO.Path.GetDirectoryName(Path);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        var array = paths.ToArray();
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(Path, JsonSerializer.Serialize(array, options));
    }

    public bool IsManaged(string jsonPath)
    {
        var managed = ReadManagedPaths();
        return managed.Contains(jsonPath, StringComparer.OrdinalIgnoreCase);
    }
}
