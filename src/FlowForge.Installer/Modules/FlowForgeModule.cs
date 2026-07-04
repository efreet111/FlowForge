using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using Spectre.Console;

namespace FlowForge.Installer.Modules;

/// <summary>
/// Instala los skills/agents/commands de FlowForge en los IDEs seleccionados.
/// </summary>
public sealed class FlowForgeModule(InstallerContext ctx)
{
    public const string InstallerVersion = "0.1.0-alpha.6";

    public void Install(List<string> selectedIdes)
    {
        AnsiConsole.MarkupLine("[bold]Instalando FlowForge skills...[/]");
        ctx.Log.Info($"FlowForgeModule.Install: ides={string.Join(",", selectedIdes)}");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var locator = new FlowForgeRepoLocator(ctx.Log);

        if (!locator.EnsureAvailable(out var ffRepo) || ffRepo == null)
        {
            AnsiConsole.MarkupLine("[red]✗[/] No se pudo obtener el repo FlowForge.");
            AnsiConsole.MarkupLine("[yellow]![/] Necesitás [bold]git[/] en PATH.");
            if (OperatingSystem.IsWindows())
                AnsiConsole.MarkupLine("[grey]  winget install Git.Git / scoop install git[/]");
            else if (OperatingSystem.IsLinux())
                AnsiConsole.MarkupLine("[grey]  sudo apt install git / sudo pacman -S git[/]");
            else if (OperatingSystem.IsMacOS())
                AnsiConsole.MarkupLine("[grey]  brew install git[/]");
            AnsiConsole.MarkupLine("[grey]  Luego reinstalá: flowforge install --yes[/]");
            return;
        }

        WarnIfExistingAgents(selectedIdes, home);
        InstallSharedParity(home, ffRepo);

        foreach (var ide in selectedIdes)
            InstallForIde(ide, home, ffRepo);
    }

    void InstallSharedParity(string home, string ffRepo)
    {
        var sharedSrc = Path.Combine(ffRepo, "ide", "shared");
        if (!Directory.Exists(sharedSrc)) return;

        var sharedDest = Path.Combine(home, ".flowforge", "shared");
        Directory.CreateDirectory(sharedDest);
        CopyGlob(sharedSrc, sharedDest, "*");
        AnsiConsole.MarkupLine($"  [green]✓[/] Paridad global → [grey]{sharedDest}[/]");
    }

    void WarnIfExistingAgents(List<string> selectedIdes, string home)
    {
        var filesByIde = new Dictionary<string, List<string>>();

        foreach (var ide in selectedIdes)
        {
            var ideLower = ide.ToLowerInvariant();
            List<string> existingFiles = [];

            switch (ideLower)
            {
                case "cursor":
                    var cursorDir = Path.Combine(home, ".cursor", "agents");
                    if (Directory.Exists(cursorDir))
                        existingFiles = [.. Directory.EnumerateFiles(cursorDir, "forge-*.md", SearchOption.TopDirectoryOnly)];
                    break;
                case "opencode":
                    var opencodeAgentsDir = Path.Combine(home, ".config", "opencode", "agents");
                    if (Directory.Exists(opencodeAgentsDir))
                        existingFiles = [.. Directory.EnumerateFiles(opencodeAgentsDir, "forge-*.md", SearchOption.TopDirectoryOnly)];
                    break;
                case "vs code":
                    var copilotAgentsDir = Path.Combine(home, ".copilot", "agents");
                    if (Directory.Exists(copilotAgentsDir))
                        existingFiles = [.. Directory.EnumerateFiles(copilotAgentsDir, "forge-*.agent.md", SearchOption.TopDirectoryOnly)];
                    break;
                case "antigravity":
                    var antigravityDir = Path.Combine(home, ".gemini", "antigravity", "rules");
                    if (Directory.Exists(antigravityDir))
                        existingFiles = [.. Directory.EnumerateFiles(antigravityDir, "*.md", SearchOption.TopDirectoryOnly)];
                    break;
            }

            if (existingFiles.Count > 0)
                filesByIde[ide] = existingFiles;
        }

        if (filesByIde.Count == 0) return;

        var totalFiles = filesByIde.Values.Sum(f => f.Count);
        AnsiConsole.MarkupLine($"[yellow]⚠️  Detecté {totalFiles} archivos forge-* existentes — serán sobrescritos.[/]");
    }

    void InstallForIde(string ide, string home, string ffRepo)
    {
        switch (ide.ToLowerInvariant())
        {
            case "cursor":
                InstallCursor(home, ffRepo);
                break;
            case "opencode":
                InstallOpenCode(home, ffRepo);
                break;
            case "vs code":
                InstallVsCode(ffRepo);
                break;
            case "antigravity":
                InstallAntigravity(ffRepo);
                break;
            case "claude desktop":
                AnsiConsole.MarkupLine("  [grey]Claude Desktop: configura manualmente el MCP — ver docs/MCP-CONFIG.md[/]");
                break;
        }
    }

    void InstallCursor(string home, string ffRepo)
    {
        var dest = Path.Combine(home, ".cursor");
        Directory.CreateDirectory(dest);
        CopySkillsToCursor(ffRepo, dest);
        AnsiConsole.MarkupLine("  [green]✓[/] Cursor → [grey]~/.cursor/rules + agents + commands[/]");
    }

    static void CopySkillsToCursor(string ffRepo, string cursorDir)
    {
        var ideDir = Path.Combine(ffRepo, "ide", "cursor");
        if (!Directory.Exists(ideDir)) return;

        CopyGlob(Path.Combine(ideDir, "rules"), Path.Combine(cursorDir, "rules"), "*.mdc");
        CopyGlob(Path.Combine(ideDir, "agents"), Path.Combine(cursorDir, "agents"), "forge-*.md");
        CopyGlob(Path.Combine(ideDir, "commands"), Path.Combine(cursorDir, "commands"), "*.md");
    }

    void InstallOpenCode(string home, string ffRepo)
    {
        var dest = Path.Combine(home, ".config", "opencode");
        var agentsDest = Path.Combine(dest, "agents");
        var commandsDest = Path.Combine(dest, "commands");
        EnsureDirectoryWithBackup(agentsDest);
        EnsureDirectoryWithBackup(commandsDest);

        var ideAgentsSrc = Path.Combine(ffRepo, "ide", "opencode", "agents");
        CopyGlob(ideAgentsSrc, agentsDest, "*.md");

        var ideCommandsSrc = Path.Combine(ffRepo, "ide", "opencode", "commands");
        CopyGlob(ideCommandsSrc, commandsDest, "*.md");

        var oldFfDir = Path.Combine(dest, "flowforge");
        if (Directory.Exists(oldFfDir))
        {
            try { Directory.Delete(oldFfDir, recursive: true); } catch { /* best-effort */ }
        }
        var oldFfJson = Path.Combine(dest, "opencode.flowforge.json");
        if (File.Exists(oldFfJson))
        {
            try { File.Delete(oldFfJson); } catch { /* best-effort */ }
        }

        AnsiConsole.MarkupLine($"  [green]✓[/] OpenCode → [grey]~/.config/opencode/agents/ + commands/[/]");
    }

    static void InstallAntigravity(string ffRepo)
    {
        var dest = PathHelper.AntigravityDir;
        EnsureDirectoryWithBackup(dest);
        EnsureDirectoryWithBackup(Path.Combine(dest, "rules"));
        EnsureDirectoryWithBackup(Path.Combine(dest, "workflows"));

        var ideDir = Path.Combine(ffRepo, "ide", "antigravity");
        if (!Directory.Exists(ideDir)) return;

        var agentsMd = Path.Combine(ideDir, "AGENTS.md");
        if (File.Exists(agentsMd))
            File.Copy(agentsMd, Path.Combine(dest, "AGENTS.md"), overwrite: true);

        CopyGlob(Path.Combine(ideDir, "rules"), PathHelper.AntigravityRules, "*.md");
        CopyGlob(Path.Combine(ideDir, "workflows"), PathHelper.AntigravityWorkflows, "*.md");

        AnsiConsole.MarkupLine("  [green]✓[/] Antigravity → [grey]~/.gemini/antigravity/ (AGENTS.md + rules + workflows)[/]");
    }

    static void InstallVsCode(string ffRepo)
    {
        var hasCopilot = HasVsCodeExtension("github.copilot");
        var hasKilo = HasVsCodeExtension("kilocode.");

        if (hasCopilot)
        {
            EnsureDirectoryWithBackup(PathHelper.CopilotAgents);
            CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), PathHelper.CopilotAgents, "*.agent.md");
            EnsureDirectoryWithBackup(PathHelper.CopilotInstructions);
            WriteUserCopilotInstructions(
                ffRepo,
                Path.Combine(PathHelper.CopilotInstructions, "flowforge.instructions.md"));
            AnsiConsole.MarkupLine("  [green]✓[/] GitHub Copilot → [grey]~/.copilot/agents + instructions/[/]");
        }

        if (hasKilo)
        {
            InstallKilo(ffRepo);
            AnsiConsole.MarkupLine("  [green]✓[/] Kilo Code → [grey]~/.config/kilo/agents/[/]");
        }

        if (!hasCopilot && !hasKilo)
        {
            EnsureDirectoryWithBackup(PathHelper.CopilotAgents);
            CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), PathHelper.CopilotAgents, "*.agent.md");
            EnsureDirectoryWithBackup(PathHelper.CopilotInstructions);
            WriteUserCopilotInstructions(
                ffRepo,
                Path.Combine(PathHelper.CopilotInstructions, "flowforge.instructions.md"));
            InstallKilo(ffRepo);
            AnsiConsole.MarkupLine("  [yellow]![/] VS Code: no se detectó GitHub Copilot ni Kilo Code — instalados ambos formatos por si acaso");
        }

        AnsiConsole.MarkupLine("  [yellow]![/] Para repo: [bold]flowforge init <ruta>[/] o ide/install.sh <ruta>[/]");
    }

    static void InstallKilo(string ffRepo)
    {
        EnsureDirectoryWithBackup(PathHelper.KiloAgents);
        CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "agents"), PathHelper.KiloAgents, "*.md");
    }

    static bool HasVsCodeExtension(string home, string prefix) =>
        Directory.Exists(Path.Combine(home, ".vscode", "extensions")) &&
        Directory.EnumerateDirectories(Path.Combine(home, ".vscode", "extensions"), $"{prefix}*")
            .Any();

    /// <summary>
    /// Instala agents e instructions a nivel workspace (GitHub Copilot).
    /// </summary>
    public static void InstallVsCodeProject(string projectPath, string ffRepo)
    {
        var ghDir = Path.Combine(projectPath, ".github");
        var agentsDest = Path.Combine(ghDir, "agents");
        Directory.CreateDirectory(agentsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), agentsDest, "*.agent.md");

        var copilotSrc = Path.Combine(ffRepo, "ide", "vscode", "copilot-instructions.md");
        if (File.Exists(copilotSrc))
            File.Copy(copilotSrc, Path.Combine(ghDir, "copilot-instructions.md"), overwrite: true);
    }

    /// <summary>
    /// Instala agents OpenCode a nivel proyecto (.opencode/agents).
    /// OpenCode CLI y Kilo Code en VS Code leen esta ruta.
    /// </summary>
    public static void InstallOpenCodeProject(string projectPath, string ffRepo)
    {
        var ocDest = Path.Combine(projectPath, ".opencode", "agents");
        Directory.CreateDirectory(ocDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "agents"), ocDest, "*.md");

        // Kilo Code también lee .kilo/agents — duplicar para máxima compatibilidad
        var kiloDest = Path.Combine(projectPath, ".kilo", "agents");
        Directory.CreateDirectory(kiloDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "agents"), kiloDest, "*.md");
    }

    public static void InstallCursorProject(string projectPath, string ffRepo)
    {
        var cursorDir = Path.Combine(projectPath, ".cursor");
        EnsureDirectoryWithBackup(cursorDir);

        var rulesDest = Path.Combine(cursorDir, "rules");
        EnsureDirectoryWithBackup(rulesDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "cursor", "rules"), rulesDest, "*.mdc");

        var agentsDest = Path.Combine(cursorDir, "agents");
        EnsureDirectoryWithBackup(agentsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "cursor", "agents"), agentsDest, "forge-*.md");

        var commandsDest = Path.Combine(cursorDir, "commands");
        EnsureDirectoryWithBackup(commandsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "cursor", "commands"), commandsDest, "*.md");
    }

    public static void InstallAntigravityProject(string projectPath, string ffRepo)
    {
        var agentsRoot = Path.Combine(projectPath, ".agents");
        EnsureDirectoryWithBackup(agentsRoot);

        var rulesDest = Path.Combine(agentsRoot, "rules");
        EnsureDirectoryWithBackup(rulesDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "antigravity", "rules"), rulesDest, "*.md");

        var workflowsDest = Path.Combine(agentsRoot, "workflows");
        EnsureDirectoryWithBackup(workflowsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "antigravity", "workflows"), workflowsDest, "*.md");

        var agentsMdSrc = Path.Combine(ffRepo, "ide", "antigravity", "AGENTS.md");
        if (File.Exists(agentsMdSrc))
            File.Copy(agentsMdSrc, Path.Combine(projectPath, "AGENTS.md"), overwrite: true);
    }

    public static bool HasVsCodeExtension(string prefix)
    {
        var extensionsDir = Path.Combine(PathHelper.HomeDir, ".vscode", "extensions");
        if (!Directory.Exists(extensionsDir))
            return false;

        return Directory.EnumerateDirectories(extensionsDir, $"{prefix}*").Any();
    }

    static void EnsureDirectoryWithBackup(string path)
    {
        BackupDirectory(path);
        Directory.CreateDirectory(path);
    }

    static void BackupDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        if (!Directory.EnumerateFileSystemEntries(path).Any())
            return;

        Directory.CreateDirectory(PathHelper.FlowForgeBackupDir);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var baseName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)) ?? "flowforge";
        var backupDest = Path.Combine(PathHelper.FlowForgeBackupDir, $"{baseName}-{timestamp}");
        CopyDirectoryRecursive(path, backupDest);
        AnsiConsole.MarkupLine($"  [grey]⋯[/] Backup: {backupDest}");
    }

    static void CopyDirectoryRecursive(string source, string destination)
    {
        if (!Directory.Exists(source))
            return;

        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);
            File.Copy(file, destFile, overwrite: true);
        }
    }

    static void WriteUserCopilotInstructions(string ffRepo, string destPath)
    {
        var copilotSrc = Path.Combine(ffRepo, "ide", "vscode", "copilot-instructions.md");
        if (!File.Exists(copilotSrc)) return;

        var body = File.ReadAllText(copilotSrc).TrimStart();
        if (body.StartsWith("---", StringComparison.Ordinal))
        {
            File.WriteAllText(destPath, body + Environment.NewLine);
            return;
        }

        File.WriteAllText(destPath,
            $"---{Environment.NewLine}applyTo: '**'{Environment.NewLine}---{Environment.NewLine}{body}{Environment.NewLine}");
    }

    static void PatchOpenCodeFlowforgeJson(string dest, string repo)
    {
        if (!File.Exists(dest)) return;
        var content = File.ReadAllText(dest);
        if (!content.Contains("__FLOWFORGE_REPO__", StringComparison.Ordinal)) return;
        content = content.Replace("__FLOWFORGE_REPO__", repo.Replace('\\', '/'));
        File.WriteAllText(dest, content);
    }

    static void CopyGlob(string srcDir, string destDir, string pattern)
    {
        if (!Directory.Exists(srcDir)) return;
        Directory.CreateDirectory(destDir);
        foreach (var f in Directory.GetFiles(srcDir, pattern))
            File.Copy(f, Path.Combine(destDir, Path.GetFileName(f)), overwrite: true);
    }
}
