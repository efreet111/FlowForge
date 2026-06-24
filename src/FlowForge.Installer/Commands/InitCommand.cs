using ConsoleAppFramework;
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
        CreateFlowForgeJson(projectPath, !noFlowDoc);

        // ── 2. FlowDoc (docs/ + AGENTS.md + .ai-work/) ───────────────────────
        if (!noFlowDoc)
        {
            var flowDoc = new FlowDocModule(ctx);
            flowDoc.Install(projectPath);
        }
        else
        {
            EnsureAgentsMdMinimal(projectPath);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Proyecto inicializado[/]").LeftJustified());
        AnsiConsole.MarkupLine($"[grey]{projectPath}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Próximos pasos:[/]");
        AnsiConsole.MarkupLine("  1. Abrí el proyecto en tu IDE");
        AnsiConsole.MarkupLine("  2. Ejecutá [blue]/flow-start <feature>[/] para iniciar una tarea");
        ctx.Log.Info("init: completado");
    }

    static void CreateFlowForgeJson(string projectPath, bool flowDocEnabled)
    {
        var configPath = Path.Combine(projectPath, ".flowforge.json");
        if (File.Exists(configPath))
        {
            AnsiConsole.MarkupLine("  [grey].flowforge.json ya existe — sin modificar[/]");
            return;
        }

        var projectName = Path.GetFileName(projectPath);
        var today       = DateTime.Now.ToString("yyyy-MM-dd");

        var docsFrameworkLine = flowDocEnabled
            ? $"""
                  "docs_framework": "flowdoc@1.1",
              """
            : "";

        var json =
            $$"""
            {
              "project": "{{projectName}}",
              "created": "{{today}}",
              "version": "1.0",
            {{docsFrameworkLine}}
              "forge": {
                "persona": {
                  "teacher_mode": false
                }
              }
            }
            """;

        File.WriteAllText(configPath, json);
        AnsiConsole.MarkupLine("  [green]✓[/] .flowforge.json creado");
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

    static bool IsSystemPath(string path) =>
        SuspiciousSegments.Any(s => path.Contains(s, StringComparison.OrdinalIgnoreCase));
}
