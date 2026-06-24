using FlowForge.Installer.Commands;
using Spectre.Console;

namespace FlowForge.Installer.Modules;

/// <summary>
/// Instala la estructura FlowDoc (docs/ + AGENTS.md) en el directorio de trabajo.
/// Respeta flowdoc.enabled de config.json.
/// </summary>
public sealed class FlowDocModule(InstallerContext ctx)
{
    public void Install(bool enabled)
    {
        if (!enabled)
        {
            AnsiConsole.MarkupLine("  [grey]FlowDoc deshabilitado en config — omitido[/]");
            ctx.Log.Info("FlowDocModule.Install: omitido (disabled)");
            return;
        }

        AnsiConsole.MarkupLine("[bold]Instalando FlowDoc...[/]");
        ctx.Log.Info("FlowDocModule.Install: inicio");

        var cwd = Directory.GetCurrentDirectory();
        var docsDir = Path.Combine(cwd, "docs");

        // Si ya existe docs/, preguntar antes de sobrescribir
        if (Directory.Exists(docsDir))
        {
            AnsiConsole.MarkupLine($"  [yellow]![/] El directorio [grey]{docsDir}[/] ya existe.");
            if (!AnsiConsole.Confirm("¿Sobrescribir la estructura docs/ existente?", defaultValue: false))
            {
                AnsiConsole.MarkupLine("  [grey]FlowDoc omitido.[/]");
                return;
            }
        }

        // Buscar templates en el repo FlowForge
        var ffRepo = LocateFlowForgeRepo();
        if (ffRepo == null)
        {
            ScaffoldMinimal(cwd);
        }
        else
        {
            ScaffoldFromTemplates(cwd, ffRepo);
        }

        // AGENTS.md — crear si no existe
        EnsureAgentsMd(cwd);

        ctx.Log.Info("FlowDocModule.Install: completado");
        AnsiConsole.MarkupLine($"  [green]✓[/] FlowDoc instalado en [grey]{cwd}[/]");
    }

    static void ScaffoldFromTemplates(string cwd, string ffRepo)
    {
        var templates = Path.Combine(ffRepo, "templates", "project");
        if (!Directory.Exists(templates))
        {
            ScaffoldMinimal(cwd);
            return;
        }

        var projectName = Path.GetFileName(cwd);
        var today       = DateTime.Now.ToString("yyyy-MM-dd");

        // Estructura docs/
        var docsDirs = new[]
        {
            "docs/architecture/adr",
            "docs/architecture/rfc",
            "docs/tasks",
            "docs/templates",
        };

        foreach (var d in docsDirs)
            Directory.CreateDirectory(Path.Combine(cwd, d));

        // .gitkeep en dirs vacíos
        File.WriteAllText(Path.Combine(cwd, "docs/architecture/adr/.gitkeep"), "");
        File.WriteAllText(Path.Combine(cwd, "docs/architecture/rfc/.gitkeep"), "");

        // Copiar archivos de template con reemplazo de placeholders
        CopyTemplate(Path.Combine(templates, "docs", "PRD.md"),         Path.Combine(cwd, "docs", "PRD.md"),         projectName, today);
        CopyTemplate(Path.Combine(templates, "docs", "DEVELOPMENT.md"), Path.Combine(cwd, "docs", "DEVELOPMENT.md"), projectName, today);
        CopyTemplate(Path.Combine(templates, "docs", "tasks", "HU-001-example.md"),
                     Path.Combine(cwd, "docs", "tasks", "HU-001-example.md"), projectName, today);

        foreach (var tmpl in new[] { "HU-template.md", "adr-template.md", "rfc-template.md" })
        {
            var src  = Path.Combine(templates, "docs", "templates", tmpl);
            var dest = Path.Combine(cwd, "docs", "templates", tmpl);
            if (File.Exists(src)) File.Copy(src, dest, overwrite: true);
        }

        // .ai-work/
        var aiWork = Path.Combine(cwd, ".ai-work");
        Directory.CreateDirectory(aiWork);
        var gitkeep = Path.Combine(aiWork, ".gitkeep");
        if (!File.Exists(gitkeep)) File.WriteAllText(gitkeep, "");

        AnsiConsole.MarkupLine("  [green]✓[/] Estructura docs/ creada desde templates FlowForge");
    }

    static void ScaffoldMinimal(string cwd)
    {
        // Sin repo FlowForge: crear estructura mínima
        var dirs = new[] { "docs/tasks", "docs/architecture/adr", "docs/architecture/rfc" };
        foreach (var d in dirs) Directory.CreateDirectory(Path.Combine(cwd, d));

        File.WriteAllText(Path.Combine(cwd, "docs", "README.md"),
            $"# Documentación\n\nCreado por FlowForge installer el {DateTime.Now:yyyy-MM-dd}.\n");

        AnsiConsole.MarkupLine("  [green]✓[/] Estructura docs/ mínima creada");
    }

    static void EnsureAgentsMd(string cwd)
    {
        var agentsPath = Path.Combine(cwd, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            AnsiConsole.MarkupLine("  [grey]AGENTS.md ya existe — sin modificar[/]");
            return;
        }

        // Crear AGENTS.md mínimo con sección FlowDoc
        var projectName = Path.GetFileName(cwd);
        File.WriteAllText(agentsPath,
            $"""
            # AGENTS.md — {projectName}

            Guía para agentes de IA trabajando en este repositorio.

            ## Stack

            - **Lenguaje**: (definir)
            - **Framework**: (definir)

            ## FlowDoc

            Este proyecto usa FlowDoc para documentación estructurada.
            Documentación activa en `docs/`.

            ## Reglas del agente

            - Leer specs relevantes antes de implementar.
            - Mantener cambios mínimos y alineados con el estilo existente.

            """);

        AnsiConsole.MarkupLine("  [green]✓[/] AGENTS.md creado");
    }

    static void CopyTemplate(string src, string dest, string projectName, string today)
    {
        if (!File.Exists(src)) return;
        var content = File.ReadAllText(src)
            .Replace("__PROJECT_NAME__", projectName)
            .Replace("__DATE__", today);
        File.WriteAllText(dest, content);
    }

    static string? LocateFlowForgeRepo()
    {
        var envRepo = Environment.GetEnvironmentVariable("FLOWFORGE_REPO");
        if (envRepo != null && Directory.Exists(envRepo)) return envRepo;

        var dir = AppContext.BaseDirectory;
        for (int i = 0; i < 5; i++)
        {
            if (File.Exists(Path.Combine(dir, "AGENTS.md"))
                && File.ReadAllText(Path.Combine(dir, "AGENTS.md")).Contains("FlowForge"))
                return dir;
            var parent = Directory.GetParent(dir)?.FullName;
            if (parent == null) break;
            dir = parent;
        }
        return null;
    }
}
