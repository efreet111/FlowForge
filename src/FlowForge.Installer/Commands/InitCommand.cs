using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge init [path] — inicializa un proyecto con FlowDoc, AGENTS.md y .flowforge.json.
/// </summary>
public sealed class InitCommand(InstallerContext ctx)
{
    static readonly string[] SuspiciousSegments =
    [
        "system32", "SysWOW64", "Windows", "AppData\\Local\\Temp",
        "Program Files", "Program Files (x86)"
    ];

    /// <param name="path">Ruta del proyecto a inicializar (por defecto: directorio actual)</param>
    /// <param name="noFlowDoc">--no-flowdoc: omitir creación de estructura docs/</param>
    /// <param name="yes">-y / --yes: omitir confirmaciones (non-interactive)</param>
    [Command("")]
    public void Run([Argument] string path = ".", bool noFlowDoc = false, bool yes = false)
    {
        AnsiConsole.Write(new Rule("[bold blue]flowforge init[/]").LeftJustified());
        AnsiConsole.WriteLine();

        var projectPath = Path.GetFullPath(path);

        // ── Validar que no sea un directorio del sistema ───────────────────────
        if (IsSystemPath(projectPath))
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Ruta rechazada: [grey]{projectPath}[/]");
            AnsiConsole.MarkupLine("  [red]El directorio parece pertenecer al sistema operativo.[/]");
            AnsiConsole.MarkupLine("  Usá la ruta de tu repositorio, por ejemplo:");
            AnsiConsole.MarkupLine("  [blue]flowforge init E:\\Proyectos\\mi-app[/]");
            ctx.Log.Info($"init: abortado — ruta del sistema: {projectPath}");
            return;
        }

        // ── Crear directorio si no existe ──────────────────────────────────────
        if (!Directory.Exists(projectPath))
        {
            AnsiConsole.MarkupLine($"  [yellow]![/] El directorio no existe. ¿Crearlo?");
            AnsiConsole.MarkupLine($"  [grey]{projectPath}[/]");
            if (!yes && !AnsiConsole.Confirm("¿Crear directorio?", defaultValue: true))
            {
                AnsiConsole.MarkupLine("[yellow]Cancelado.[/]");
                return;
            }
            Directory.CreateDirectory(projectPath);
            AnsiConsole.MarkupLine($"  [green]✓[/] Directorio creado.");
        }

        // ── Resumen ───────────────────────────────────────────────────────────
        AnsiConsole.MarkupLine($"[bold]Proyecto:[/] [grey]{projectPath}[/]");
        AnsiConsole.MarkupLine($"  [green]●[/] .flowforge.json");
        AnsiConsole.MarkupLine($"  [green]●[/] AGENTS.md");
        if (!noFlowDoc)
            AnsiConsole.MarkupLine($"  [green]●[/] docs/ + .ai-work/");
        else
            AnsiConsole.MarkupLine($"  [grey]●[/] docs/          → omitido (--no-flowdoc)");
        AnsiConsole.WriteLine();

        if (!yes && !AnsiConsole.Confirm("¿Proceder?", defaultValue: true))
        {
            AnsiConsole.MarkupLine("[yellow]Cancelado.[/]");
            return;
        }

        ctx.Log.Info($"init: inicio → {projectPath}");

        // ── 1. .flowforge.json ────────────────────────────────────────────────
        CreateFlowForgeJson(projectPath, !noFlowDoc, ctx);

        // ── 2. .ai-work/ (siempre — capa FlowForge, independiente de FlowDoc) ─
        EnsureAiWorkDir(projectPath);

        // ── 3. FlowDoc (docs/ + AGENTS.md) ───────────────────────────────────
        if (!noFlowDoc)
        {
            var flowDoc = new FlowDocModule(ctx);
            flowDoc.Install(projectPath);
        }
        else
        {
            EnsureAgentsMdMinimal(projectPath);
        }

        InstallVsCodeProjectPack(projectPath, ctx);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Proyecto inicializado[/]").LeftJustified());
        AnsiConsole.MarkupLine($"[grey]{projectPath}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Próximos pasos:[/]");
        AnsiConsole.MarkupLine("  1. Abrí el proyecto en tu IDE");
        AnsiConsole.MarkupLine("  2. Ejecutá [blue]/flow-start[/] [grey]<feature>[/] para iniciar una tarea");
        if (!noFlowDoc)
            AnsiConsole.MarkupLine("  [grey]FlowDoc activo — los agentes leen docs/ vía .flowforge.json[/]");
        else
            AnsiConsole.MarkupLine("  [grey]FlowDoc desactivado — solo metodología FlowForge (.ai-work/)[/]");
        ctx.Log.Info("init: completado");
    }

    static void CreateFlowForgeJson(string projectPath, bool flowDocEnabled, InstallerContext ctx)
    {
        var configPath = Path.Combine(projectPath, ".flowforge.json");
        if (File.Exists(configPath))
        {
            AnsiConsole.MarkupLine("  [grey].flowforge.json ya existe — sin modificar[/]");
            return;
        }

        var projectName = Path.GetFileName(projectPath);
        var today       = DateTime.Now.ToString("yyyy-MM-dd");
        var ffVersion   = FlowForgeModule.InstallerVersion;

        var locator = new FlowForgeRepoLocator(ctx.Log);
        var ffRepo    = locator.Locate() ?? (locator.EnsureAvailable(out var cloned) ? cloned : null);

        string json;
        if (flowDocEnabled)
        {
            var templatePath = ffRepo != null
                ? Path.Combine(ffRepo, "templates", "project", ".flowforge.json.template")
                : null;

            if (templatePath != null && File.Exists(templatePath))
            {
                json = File.ReadAllText(templatePath)
                    .Replace("__PROJECT_NAME__", projectName, StringComparison.Ordinal)
                    .Replace("__DATE__", today, StringComparison.Ordinal)
                    .Replace("__FLOWFORGE_VERSION__", ffVersion, StringComparison.Ordinal);
            }
            else
            {
                json = BuildFlowDocEnabledJson(projectName, today, ffVersion);
            }
        }
        else
        {
            json = BuildFlowDocDisabledJson(projectName, today, ffVersion);
        }

        File.WriteAllText(configPath, json);
        AnsiConsole.MarkupLine("  [green]✓[/] .flowforge.json creado");
    }

    static string BuildFlowDocEnabledJson(string projectName, string today, string ffVersion) =>
        $$"""
        {
          "version": "1",
          "workflow": "flowforge",
          "flowforge_version": "{{ffVersion}}",
          "docs_framework": "flowdoc",
          "docs_framework_version": "2.0",
          "upstream": {
            "repo": "https://github.com/crhistianmdz/FlowDocs",
            "status": "private"
          },
          "adoption_level": 1,
          "project": "{{projectName}}",
          "created": "{{today}}",
          "engram": {
            "enabled": true,
            "project": "{{projectName}}"
          },
          "paths": {
            "prd": "docs/PRD.md",
            "backlog": "docs/tasks",
            "decisions": "docs/architecture/adr",
            "rfcs": "docs/architecture/rfc",
            "development": "docs/DEVELOPMENT.md",
            "features": ".ai-work",
            "templates": "docs/templates"
          }
        }
        """;

    static string BuildFlowDocDisabledJson(string projectName, string today, string ffVersion) =>
        $$"""
        {
          "version": "1",
          "workflow": "flowforge",
          "flowforge_version": "{{ffVersion}}",
          "adoption_level": 1,
          "project": "{{projectName}}",
          "created": "{{today}}",
          "engram": {
            "enabled": true,
            "project": "{{projectName}}"
          },
          "paths": {
            "features": ".ai-work"
          }
        }
        """;

    static void EnsureAiWorkDir(string projectPath)
    {
        var aiWork = Path.Combine(projectPath, ".ai-work");
        Directory.CreateDirectory(aiWork);
        var gitkeep = Path.Combine(aiWork, ".gitkeep");
        if (!File.Exists(gitkeep))
            File.WriteAllText(gitkeep, "");
    }

    static void EnsureAgentsMdMinimal(string projectPath)
    {
        var agentsPath = Path.Combine(projectPath, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            AnsiConsole.MarkupLine("  [grey]AGENTS.md ya existe — sin modificar[/]");
            return;
        }

        var projectName = Path.GetFileName(projectPath);
        File.WriteAllText(agentsPath,
            $"""
            # AGENTS.md — {projectName}

            Guía para agentes de IA trabajando en este repositorio.

            ## Stack

            - **Lenguaje**: (definir)
            - **Framework**: (definir)

            ## Reglas del agente

            - Leer specs relevantes antes de implementar.
            - Mantener cambios mínimos y alineados con el estilo existente.

            """);

        AnsiConsole.MarkupLine("  [green]✓[/] AGENTS.md creado");
    }

    static void InstallVsCodeProjectPack(string projectPath, InstallerContext ctx)
    {
        var locator = new FlowForgeRepoLocator(ctx.Log);
        if (!locator.EnsureAvailable(out var ffRepo) || ffRepo == null)
        {
            AnsiConsole.MarkupLine("  [yellow]![/] IDE packs omitidos — no se pudo localizar el repo FlowForge");
            return;
        }

        FlowForgeModule.InstallVsCodeProject(projectPath, ffRepo);
        AnsiConsole.MarkupLine("  [green]✓[/] GitHub Copilot → [grey].github/agents + copilot-instructions.md[/]");

        FlowForgeModule.InstallOpenCodeProject(projectPath, ffRepo);
        AnsiConsole.MarkupLine("  [green]✓[/] OpenCode / Kilo → [grey].opencode/agents + .kilo/agents[/]");

        FlowForgeModule.InstallCursorProject(projectPath, ffRepo);
        AnsiConsole.MarkupLine("  [green]✓[/] Cursor → [grey].cursor/rules + .cursor/agents + commands/[/]");

        FlowForgeModule.InstallAntigravityProject(projectPath, ffRepo);
        AnsiConsole.MarkupLine("  [green]✓[/] Antigravity → [grey].agents/rules + workflows + AGENTS.md[/]");
    }

    static bool IsSystemPath(string path) =>
        SuspiciousSegments.Any(s => path.Contains(s, StringComparison.OrdinalIgnoreCase));
}
