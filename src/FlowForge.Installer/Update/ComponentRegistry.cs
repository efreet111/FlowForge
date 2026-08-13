using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;

namespace FlowForge.Installer.Update;

/// <summary>
/// Per-component version tracking. Reads/writes config.json via ConfigStore
/// for atomic writes. AOT-safe (no reflection).
/// </summary>
public sealed class ComponentRegistry
{
    readonly ConfigStore _store;

    public ComponentRegistry(ConfigStore store)
    {
        _store = store;
    }

    /// <summary>Get installed version for a component (null if not installed).</summary>
    public string? GetVersion(UpdateComponent component)
    {
        var cfg = _store.Load();
        return component switch
        {
            UpdateComponent.Engram => cfg.Components.EngramDotnet?.Installed == true
                ? cfg.Components.EngramDotnet.Version
                : null,
            UpdateComponent.FlowForgeSkills => cfg.Components.FlowForge?.Installed == true
                ? cfg.Components.FlowForge.Version
                : null,
            UpdateComponent.FlowDoc => cfg.Components.FlowDoc?.Installed == true
                ? cfg.Components.FlowDoc.Version
                : null,
            UpdateComponent.Installer => cfg.Components.Installer?.Installed == true
                ? cfg.Components.Installer.Version
                : null,
            _ => null
        };
    }

    /// <summary>Set version for a component (atomic write via ConfigStore).</summary>
    public void SetVersion(UpdateComponent component, string version)
    {
        _store.Update(cfg =>
        {
            switch (component)
            {
                case UpdateComponent.Engram:
                    cfg.Components.EngramDotnet ??= new ComponentEntry();
                    cfg.Components.EngramDotnet.Installed = true;
                    cfg.Components.EngramDotnet.Version = version;
                    cfg.Components.EngramDotnet.Binary = PathHelper.EngramBinary;
                    cfg.Components.EngramDotnet.RegisteredAt =
                        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
                    break;

                case UpdateComponent.FlowForgeSkills:
                    cfg.Components.FlowForge ??= new FlowForgeComponentEntry();
                    cfg.Components.FlowForge.Installed = true;
                    cfg.Components.FlowForge.Version = version;
                    break;

                case UpdateComponent.FlowDoc:
                    cfg.Components.FlowDoc ??= new FlowDocEntry();
                    cfg.Components.FlowDoc.Installed = true;
                    cfg.Components.FlowDoc.Version = version;
                    break;

                case UpdateComponent.Installer:
                    cfg.Components.Installer ??= new InstallerEntry();
                    cfg.Components.Installer.Installed = true;
                    cfg.Components.Installer.Version = version;
                    cfg.Components.Installer.Binary = PathHelper.InstallerBinary;
                    break;
            }
        });
    }

    /// <summary>Check if a component is at the target version (idempotency check).</summary>
    public bool IsAtVersion(UpdateComponent component, string targetVersion)
    {
        var current = GetVersion(component);
        return string.Equals(current, targetVersion, StringComparison.Ordinal);
    }

    /// <summary>Get all component versions for status display.</summary>
    public Dictionary<string, string?> GetAllVersions()
    {
        var cfg = _store.Load();
        return new Dictionary<string, string?>
        {
            ["engram"] = cfg.Components.EngramDotnet?.Installed == true
                ? cfg.Components.EngramDotnet.Version
                : null,
            ["flowforge-skills"] = cfg.Components.FlowForge?.Installed == true
                ? cfg.Components.FlowForge.Version
                : null,
            ["flowdoc"] = cfg.Components.FlowDoc?.Installed == true
                ? cfg.Components.FlowDoc.Version
                : null,
            ["installer"] = cfg.Components.Installer?.Installed == true
                ? cfg.Components.Installer.Version
                : null,
        };
    }
}
