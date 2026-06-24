using System.Text.Json.Serialization;

namespace FlowForge.Installer.Models;

/// <summary>
/// Representa el manifest.yaml descargado de GitHub en runtime.
/// Permite que las reglas de compatibilidad se actualicen sin recompilar el installer.
/// URL: https://raw.githubusercontent.com/efreet111/FlowForge/main/install/manifest.yaml
/// </summary>
public sealed class RemoteManifest
{
    /// <summary>Versión del manifest (coincide con la última release del installer).</summary>
    public string InstallerVersion { get; set; } = "0.1.0-alpha.5";

    /// <summary>
    /// Versión mínima de engram-dotnet compatible con este installer.
    /// Formato: ">=0.3.0" o ">=0.3.0, &lt;2.0.0"
    /// </summary>
    public string RequiresEngramDotnet { get; set; } = ">=0.3.0";

    /// <summary>Versión mínima del installer para que este manifest sea válido.</summary>
    public string RequiresInstaller { get; set; } = ">=0.1.0-alpha.5";

    /// <summary>Indica si el manifest se cargó desde GitHub o son valores por defecto.</summary>
    [JsonIgnore]
    public bool IsRemote { get; set; } = false;

    /// <summary>
    /// Devuelve el manifest embebido (fallback si GitHub no está disponible).
    /// </summary>
    public static RemoteManifest Default => new()
    {
        InstallerVersion      = "0.1.0-alpha.5",
        RequiresEngramDotnet  = ">=0.3.0",
        RequiresInstaller     = ">=0.1.0-alpha.5",
        IsRemote              = false,
    };
}
