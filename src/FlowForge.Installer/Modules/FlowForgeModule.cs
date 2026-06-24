using FlowForge.Installer.Commands;
using Spectre.Console;

namespace FlowForge.Installer.Modules;

/// <summary>
/// Instala los skills/agents/commands de FlowForge en los IDEs seleccionados.
/// Replica la lógica de ide/install.sh en C# para funcionar cross-platform.
/// </summary>
public sealed class FlowForgeModule(InstallerContext ctx)
{
    public const string InstallerVersion = "0.1.0-alpha.4";

    public void Install(List<string> selectedIdes)
    {
        AnsiConsole.MarkupLine("[bold]Instalando FlowForge skills...[/]");
        ctx.Log.Info($"FlowForgeModule.Install: ides={string.Join(",", selectedIdes)}");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        // ── Advertencia: archivos forge-* existentes ─────────────────────────────
        WarnIfExistingAgents(selectedIdes, home);

        // El repo FlowForge puede estar en varios lugares:
        // 1. Junto al binario (si se clonó)
        // 2. Variable FLOWFORGE_REPO
        // 3. No disponible (installer standalone descargado)
        var ffRepo = LocateFlowForgeRepo();

        foreach (var ide in selectedIdes)
        {
            InstallForIde(ide, home, ffRepo);
        }
    }

    /// <summary>
    /// Escanea y warns si existen archivos forge-* pre-existentes en los directorios de IDE.
    /// No bloquea la instalación — solo emite advertencia informativa.
    /// </summary>
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
                    {
                        existingFiles = [.. Directory.EnumerateFiles(cursorDir, "forge-*.md", SearchOption.TopDirectoryOnly)];
                    }
                    break;
                case "opencode":
                    var opencodeDir = Path.Combine(home, ".config", "opencode");
                    if (Directory.Exists(opencodeDir))
                    {
                        existingFiles = [.. Directory.EnumerateFiles(opencodeDir, "forge-*.md", SearchOption.AllDirectories)];
                    }
                    break;
                case "vs code":
                    var vscodeDir = Path.Combine(home, ".vscode", "agents");
                    if (Directory.Exists(vscodeDir))
                    {
                        existingFiles = [.. Directory.EnumerateFiles(vscodeDir, "*.agent.md", SearchOption.TopDirectoryOnly)];
                    }
                    break;
                case "claude desktop":
                    // Claude Desktop no tiene archivos de agente a sobrescribir (solo skills)
                    continue;
            }

            if (existingFiles.Count > 0)
            {
                filesByIde[ide] = existingFiles;
            }
        }

        if (filesByIde.Count == 0) return;

        var totalFiles = filesByIde.Values.Sum(f => f.Count);
        AnsiConsole.MarkupLine($"[yellow]⚠️  Detecté {totalFiles} archivos forge-* existentes en los siguientes directorios:[/]");

        foreach (var kvp in filesByIde)
        {
            foreach (var file in kvp.Value)
            {
                AnsiConsole.MarkupLine($"  • {file}");
            }
        }

        AnsiConsole.MarkupLine("[yellow]Estos archivos serán sobrescritos. Si los personalizaste, hacé un backup antes de continuar.[/]");
    }

    void InstallForIde(string ide, string home, string? ffRepo)
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

    void InstallCursor(string home, string? ffRepo)
    {
        var dest = Path.Combine(home, ".cursor");
        if (!Directory.Exists(dest))
        {
            AnsiConsole.MarkupLine("  [yellow]![/] Cursor no detectado — omitido");
            return;
        }

        if (ffRepo != null)
        {
            CopySkillsToCursor(ffRepo, dest);
        }
        else
        {
            // Standalone: advertir que el usuario instale manualmente
            AnsiConsole.MarkupLine("  [yellow]![/] Cursor: ejecutá [bold]bash ide/install.sh[/] desde el repo FlowForge para instalar skills");
        }
    }

    static void CopySkillsToCursor(string ffRepo, string cursorDir)
    {
        var ideDir = Path.Combine(ffRepo, "ide", "cursor");
        if (!Directory.Exists(ideDir)) return;

        CopyGlob(Path.Combine(ideDir, "rules"), Path.Combine(cursorDir, "rules"), "*.mdc");
        CopyGlob(Path.Combine(ideDir, "agents"), Path.Combine(cursorDir, "agents"), "forge-*.md");
        CopyGlob(Path.Combine(ideDir, "commands"), Path.Combine(cursorDir, "commands"), "*.md");

        AnsiConsole.MarkupLine("  [green]✓[/] Cursor skills instalados");
    }

    void InstallOpenCode(string home, string? ffRepo)
    {
        var dest = Path.Combine(home, ".config", "opencode");
        if (!Directory.Exists(dest))
        {
            AnsiConsole.MarkupLine("  [yellow]![/] OpenCode no detectado — omitido");
            return;
        }

        if (ffRepo != null)
        {
            var ideDir = Path.Combine(ffRepo, "ide", "opencode");
            var ffDest = Path.Combine(dest, "flowforge");
            Directory.CreateDirectory(ffDest);
            CopyGlob(ideDir, ffDest, "*.md");
            CopyGlob(Path.Combine(ffRepo, "ide", "shared"), ffDest, "*");
            AnsiConsole.MarkupLine("  [green]✓[/] OpenCode skills instalados");
            AnsiConsole.MarkupLine("  [yellow]![/] Merge manual: agrega el bloque agent{} de opencode.flowforge.json → opencode.json");
        }
        else
        {
            AnsiConsole.MarkupLine("  [yellow]![/] OpenCode: ejecutá [bold]bash ide/install.sh[/] desde el repo FlowForge");
        }
    }

    static void InstallVsCode(string home, string? ffRepo)
    {
        if (ffRepo != null)
        {
            var dest = Path.Combine(home, ".vscode", "agents");
            Directory.CreateDirectory(dest);
            CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), dest, "*.agent.md");
            AnsiConsole.MarkupLine("  [green]✓[/] VS Code agents instalados");
        }
        else
        {
            AnsiConsole.MarkupLine("  [yellow]![/] VS Code: ejecutá [bold]bash ide/install.sh[/] desde el repo FlowForge");
        }
    }

    static void CopyGlob(string srcDir, string destDir, string pattern)
    {
        if (!Directory.Exists(srcDir)) return;
        Directory.CreateDirectory(destDir);
        foreach (var f in Directory.GetFiles(srcDir, pattern))
        {
            File.Copy(f, Path.Combine(destDir, Path.GetFileName(f)), overwrite: true);
        }
    }

    static string? LocateFlowForgeRepo()
    {
        // 1. Variable de entorno explícita
        var envRepo = Environment.GetEnvironmentVariable("FLOWFORGE_REPO");
        if (envRepo != null && Directory.Exists(envRepo)) return envRepo;

        // 2. Junto al binario del installer
        var exeDir = AppContext.BaseDirectory;
        // Subir hasta encontrar AGENTS.md con "FlowForge"
        var dir = exeDir;
        for (int i = 0; i < 5; i++)
        {
            var agents = Path.Combine(dir, "AGENTS.md");
            if (File.Exists(agents) && File.ReadAllText(agents).Contains("FlowForge"))
                return dir;
            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }

        return null;
    }
}
