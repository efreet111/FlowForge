namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Rutas estándar del installer, cross-platform.
/// </summary>
public static class PathHelper
{
    public static string HomeDir =>
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static string EngramDir =>
        Path.Combine(HomeDir, ".engram");

    public static string ConfigFile =>
        Path.Combine(EngramDir, "config.json");

    public static string LogFile =>
        Path.Combine(EngramDir, "install.log");

    public static string FlowForgeBackupDir =>
        Path.Combine(HomeDir, ".flowforge-backups");

    public static string CopilotAgents =>
        Path.Combine(HomeDir, ".copilot", "agents");

    public static string CopilotInstructions =>
        Path.Combine(HomeDir, ".copilot", "instructions");

    public static string KiloConfigDir =>
        Path.Combine(HomeDir, ".config", "kilo");

    public static string KiloAgents =>
        Path.Combine(KiloConfigDir, "agents");

    public static string AntigravityDir =>
        Path.Combine(HomeDir, ".gemini", "antigravity");

    public static string AntigravityRules =>
        Path.Combine(AntigravityDir, "rules");

    public static string AntigravityWorkflows =>
        Path.Combine(AntigravityDir, "workflows");

    /// <summary>Directorio de instalación del binario flowforge.</summary>
    public static string InstallerBinDir =>
        OperatingSystem.IsWindows()
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                           "Programs", "FlowForge")
            : Path.Combine(HomeDir, ".local", "bin");

    /// <summary>Ruta completa al binario flowforge instalado.</summary>
    public static string InstallerBinary =>
        OperatingSystem.IsWindows()
            ? Path.Combine(InstallerBinDir, "flowforge.exe")
            : Path.Combine(InstallerBinDir, "flowforge");

    /// <summary>Directorio de instalación del binario engram.</summary>
    public static string EngramBinDir => InstallerBinDir;

    /// <summary>Ruta completa al binario engram instalado.</summary>
    public static string EngramBinary =>
        OperatingSystem.IsWindows()
            ? Path.Combine(EngramBinDir, "engram.exe")
            : Path.Combine(EngramBinDir, "engram");

    /// <summary>Librería SQLite nativa instalada junto a los binarios.</summary>
    public static string NativeSqliteLib =>
        OperatingSystem.IsWindows()
            ? Path.Combine(InstallerBinDir, "e_sqlite3.dll")
            : Path.Combine(InstallerBinDir, "libe_sqlite3.so");

    /// <summary>Conjunto de rutas que deben pertenecer al usuario en caso de sudo.</summary>
    public static IEnumerable<string> OwnershipTargets =>
        new[]
        {
            InstallerBinary,
            EngramBinary,
            NativeSqliteLib,
            EngramDir,
        };

    /// <summary>Rutas de config MCP por editor.</summary>
    public static IdeConfigPaths GetIdePaths(string homeDir) => new(homeDir);
}

/// <summary>Paths de configuración MCP para cada editor soportado.</summary>
public sealed class IdeConfigPaths(string home)
{
    public string Cursor    => Path.Combine(home, ".cursor", "mcp.json");
    // OpenCode uses opencode.jsonc (or opencode.json). EngramModule merges into the existing file.
    public string OpenCode  => Path.Combine(home, ".config", "opencode", "opencode.json");
    public string VsCode    => Path.Combine(home, ".vscode", "mcp.json");

    /// <summary>VS Code Copilot user-level custom agents (see code.visualstudio.com/docs/copilot/customization/custom-agents).</summary>
    public string CopilotAgents => Path.Combine(home, ".copilot", "agents");

    /// <summary>VS Code Copilot user-level instructions directory.</summary>
    public string CopilotInstructionsDir => Path.Combine(home, ".copilot", "instructions");

    /// <summary>
    /// Antigravity (Google): ~/.gemini/antigravity/mcp_config.json
    /// Uses mcpServers format (same as Cursor).
    /// </summary>
    public string Antigravity => Path.Combine(home, ".gemini", "antigravity", "mcp_config.json");

    /// <summary>
    /// Claude Desktop: ruta varía por plataforma.
    /// macOS: ~/Library/Application Support/Claude/claude_desktop_config.json
    /// Windows: %APPDATA%\Claude\claude_desktop_config.json
    /// Linux: ~/.config/Claude/claude_desktop_config.json
    /// </summary>
    public string ClaudeDesktop =>
        OperatingSystem.IsMacOS()
            ? Path.Combine(home, "Library", "Application Support", "Claude", "claude_desktop_config.json")
            : OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                               "Claude", "claude_desktop_config.json")
                : Path.Combine(home, ".config", "Claude", "claude_desktop_config.json");
}
