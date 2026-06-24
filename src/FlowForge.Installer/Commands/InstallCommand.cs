using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using FlowForge.Installer.Modules;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge install — wizard interactivo multi-componente.
/// </summary>
public sealed class InstallCommand(InstallerContext ctx)
{
    const string CurrentVersion = "0.1.0-alpha.1";

    /// <param name="yes">-y / --yes: omitir confirmaciones (non-interactive)</param>
    [Command("")]
    public async Task RunAsync(bool yes = false)
    {
        AnsiConsole.Write(new Rule("[bold blue]FlowForge Stack Installer v0.1.0-alpha.1[/]").LeftJustified());
        AnsiConsole.WriteLine();

        // ── Verificar compatibilidad con manifest remoto ───────────────────────
        var remoteManifest = await ctx.Manifest.FetchAsync();
        if (remoteManifest.IsRemote)
        {
            var selfError = ManifestClient.CheckInstallerCompatibility(remoteManifest, CurrentVersion);
            if (selfError != null)
            {
                var formatted = Verbosity.FormatError(selfError);
                AnsiConsole.MarkupLine($"[red]⚠️  {formatted}[/]");
                if (!yes && !AnsiConsole.Confirm("¿Continuar de todos modos?", defaultValue: false))
                    return;
            }
        }

        var cfg = ctx.Store.Load();

        // ── 1. Selección de componentes ───────────────────────────────────────
        AnsiConsole.MarkupLine("[bold]¿Qué componentes instalar?[/]");
        var components = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("")
                .InstructionsText("[grey](Espacio para seleccionar, Enter para confirmar)[/]")
                .AddChoices([
                    "engram-dotnet (backend de memoria)",
                    "FlowForge (skills + agents para IDEs)",
                    "FlowDoc (estructura docs/ en proyecto actual)",
                ]));

        bool installEngram   = components.Any(c => c.StartsWith("engram-dotnet"));
        bool installFlowForge = components.Any(c => c.StartsWith("FlowForge"));
        bool installFlowDoc  = components.Any(c => c.StartsWith("FlowDoc"));

        AnsiConsole.WriteLine();

        // ── 2. Modo engram ────────────────────────────────────────────────────
        string engramMode = "local";
        if (installEngram)
        {
            engramMode = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[bold]Modo de uso de engram-dotnet:[/]")
                    .AddChoices([
                        "Local (SQLite, sin sync)",
                        "Offline-first sync (SQLite + servidor)",
                    ]));
            engramMode = engramMode.StartsWith("Local") ? "local" : "sync";
        }

        // ── 3. IDEs para FlowForge ────────────────────────────────────────────
        List<string> selectedIdes = [];
        if (installFlowForge)
        {
            AnsiConsole.MarkupLine("[bold]¿Dónde instalar los skills de FlowForge?[/]");
            selectedIdes = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("")
                    .InstructionsText("[grey](Espacio para seleccionar, Enter para confirmar)[/]")
                    .AddChoices(["Cursor", "OpenCode", "VS Code", "Claude Desktop"]));
        }

        // ── 4. FlowDoc opt-in ─────────────────────────────────────────────────
        bool flowDocEnabled = cfg.FlowDoc.Enabled;
        if (installFlowDoc)
        {
            var cwd = Directory.GetCurrentDirectory();
            AnsiConsole.MarkupLine($"  [grey]Directorio actual: {cwd}[/]");

            var suspiciousSegments = new[] { "system32", "SysWOW64", "Windows", "Temp", "AppData\\Local\\Temp", "Program Files" };
            bool isSuspicious = suspiciousSegments.Any(s => cwd.Contains(s, StringComparison.OrdinalIgnoreCase));
            bool hasGit = Directory.Exists(Path.Combine(cwd, ".git"));

            if (isSuspicious)
            {
                AnsiConsole.MarkupLine("  [red]⚠️  El directorio actual parece un directorio del sistema — no es un proyecto.[/]");
                AnsiConsole.MarkupLine("  [yellow]Consejo:[/] Ejecutá el instalador desde la raíz de tu proyecto:");
                AnsiConsole.MarkupLine("  [grey]  cd E:\\Proyectos\\mi-app && flowforge install[/]");
                flowDocEnabled = false;
            }
            else
            {
                if (!hasGit)
                    AnsiConsole.MarkupLine("  [yellow]![/] No se detectó repositorio git — FlowDoc se instalará aquí de todos modos.");

                flowDocEnabled = AnsiConsole.Confirm(
                    $"[bold]¿Crear estructura docs/ en [green]{cwd}[/]?[/]", defaultValue: true);
            }
        }

        // ── 5. Resumen + confirmación ─────────────────────────────────────────
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Resumen[/]").LeftJustified());
        if (installEngram)   AnsiConsole.MarkupLine($"  [green]●[/] engram-dotnet  (modo: {engramMode})");
        if (installFlowForge) AnsiConsole.MarkupLine($"  [green]●[/] FlowForge      (IDEs: {string.Join(", ", selectedIdes)})");
        if (installFlowDoc)
        {
            if (flowDocEnabled)
                AnsiConsole.MarkupLine($"  [green]●[/] FlowDoc         → {Directory.GetCurrentDirectory()}");
            else
                AnsiConsole.MarkupLine("  [grey]●[/] FlowDoc         → omitido (directorio no válido o cancelado)");
        }
        AnsiConsole.WriteLine();

        if (!yes && !AnsiConsole.Confirm("¿Proceder con la instalación?", defaultValue: true))
        {
            AnsiConsole.MarkupLine("[yellow]Instalación cancelada.[/]");
            return;
        }

        // ── 6. Ejecutar instalación ───────────────────────────────────────────
        ctx.Log.Info("install: inicio");

        if (installEngram)
        {
            var module = new EngramModule(ctx);
            await module.InstallAsync(engramMode);
        }

        if (installFlowForge)
        {
            var module = new FlowForgeModule(ctx);
            module.Install(selectedIdes);
        }

        if (installFlowDoc)
        {
            var module = new FlowDocModule(ctx);
            module.Install(flowDocEnabled);
        }

        // ── 7. Guardar config ─────────────────────────────────────────────────
        ctx.Store.Update(c =>
        {
            c.FlowDoc.Enabled = flowDocEnabled;
            if (installFlowForge && selectedIdes.Count > 0)
            {
                c.Components.FlowForge ??= new FlowForgeComponentEntry();
                c.Components.FlowForge.Installed = true;
                c.Components.FlowForge.Version = FlowForgeModule.InstallerVersion;
                c.Components.FlowForge.Ides = selectedIdes.Select(i => i.ToLowerInvariant()).ToList();
            }
        });

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Instalación completada[/]").LeftJustified());
        AnsiConsole.MarkupLine("[grey]Recargá tu IDE para activar los cambios.[/]");
        ctx.Log.Info("install: completado");
    }
}
