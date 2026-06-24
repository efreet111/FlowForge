using System.Text.Json;
using FlowForge.Installer.Models;

namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Lee y escribe ~/.engram/config.json. Idempotente y tolerante a errores.
/// </summary>
public sealed class ConfigStore
{
    readonly string _configFile;
    readonly InstallerLogger _log;

    public ConfigStore(string configFile, InstallerLogger log)
    {
        _configFile = configFile;
        _log = log;
    }

    public static ConfigStore Default =>
        new(PathHelper.ConfigFile, InstallerLogger.Default);

    /// <summary>Lee la config existente o devuelve defaults.</summary>
    public InstallerConfig Load()
    {
        if (!File.Exists(_configFile))
            return new InstallerConfig();

        try
        {
            var json = File.ReadAllText(_configFile);
            return JsonSerializer.Deserialize(json, InstallerJsonContext.Default.InstallerConfig)
                   ?? new InstallerConfig();
        }
        catch (Exception ex)
        {
            _log.Warn($"ConfigStore.Load: config.json corrupto, usando defaults. {ex.Message}");
            return new InstallerConfig();
        }
    }

    /// <summary>Guarda la config. Crea el directorio si no existe.</summary>
    public void Save(InstallerConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_configFile)!);
        var json = JsonSerializer.Serialize(config, InstallerJsonContext.Default.InstallerConfig);
        File.WriteAllText(_configFile, json + Environment.NewLine);
        _log.Info($"ConfigStore.Save: config.json actualizado");
    }

    /// <summary>Actualiza en lugar (read-modify-write).</summary>
    public InstallerConfig Update(Action<InstallerConfig> mutate)
    {
        var cfg = Load();
        mutate(cfg);
        Save(cfg);
        return cfg;
    }
}
