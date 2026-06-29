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
                    var vscodeDir = Path.Combine(home, ".vscode", "agents");
                    if (Directory.Exists(vscodeDir))
                        existingFiles = [.. Directory.EnumerateFiles(vscodeDir, "*.agent.md", SearchOption.TopDirectoryOnly)];
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
                InstallVsCode(home, ffRepo);
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
        // OpenCode auto-loads agents from ~/.config/opencode/agents/*.md
        // and commands from ~/.config/opencode/commands/*.md — no merge needed.
        var dest = Path.Combine(home, ".config", "opencode");
        var agentsDest = Path.Combine(dest, "agents");
        var commandsDest = Path.Combine(dest, "commands");
        Directory.CreateDirectory(agentsDest);
        Directory.CreateDirectory(commandsDest);

        // Copy agent markdown files (one per FlowForge agent)
        var ideAgentsSrc = Path.Combine(ffRepo, "ide", "opencode", "agents");
        CopyGlob(ideAgentsSrc, agentsDest, "*.md");

        // Copy commands (if we have any — currently empty)
        var ideCommandsSrc = Path.Combine(ffRepo, "ide", "opencode", "commands");
        if (Directory.Exists(ideCommandsSrc))
            CopyGlob(ideCommandsSrc, commandsDest, "*.md");

        // Clean up old approach: remove ~/.config/opencode/flowforge/ and opencode.flowforge.json
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

    static void InstallVsCode(string home, string ffRepo)
    {
        var vscodeHome = Path.Combine(home, ".vscode");
        Directory.CreateDirectory(vscodeHome);

        var agentsDest = Path.Combine(vscodeHome, "agents");
        Directory.CreateDirectory(agentsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), agentsDest, "*.agent.md");

        var copilotSrc = Path.Combine(ffRepo, "ide", "vscode", "copilot-instructions.md");
        if (File.Exists(copilotSrc))
            File.Copy(copilotSrc, Path.Combine(vscodeHome, "copilot-instructions.md"), overwrite: true);

        AnsiConsole.MarkupLine("  [green]✓[/] VS Code → [grey]~/.vscode/copilot-instructions + agents[/]");
        AnsiConsole.MarkupLine("  [yellow]![/] Para repo: [bold]flowforge init <ruta>[/] o ide/install.ps1 -ProjectPath");
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
