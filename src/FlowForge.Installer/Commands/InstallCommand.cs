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
    const string CurrentVersion = "0.1.0-alpha.4";

    /// <param name="yes">-y / --yes: omitir confirmaciones (non-interactive)</param>
    [Command("")]
    public async Task RunAsync(bool yes = false)
    {
        AnsiConsole.Write(new Rule("[bold blue]FlowForge Stack Installer v0.1.0-alpha.4[/]").LeftJustified());
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

        // ── 1. Selección de componentes (global) ──────────────────────────────
        AnsiConsole.MarkupLine("[bold]¿Qué componentes instalar?[/]");
        AnsiConsole.MarkupLine("[grey]Este wizard instala componentes globales (binario + IDE agents).[/]");
        AnsiConsole.MarkupLine("[grey]Para inicializar un proyecto usa: [/][blue]flowforge init <ruta>[/]");
        AnsiConsole.WriteLine();
        var components = AnsiConsole.Prompt(
            new MultiSelectionPrompt<string>()
                .Title("")
                .InstructionsText("[grey](Espacio para seleccionar, Enter para confirmar)[/]")
                .AddChoices([
                    "engram-dotnet (backend de memoria)",
                    "FlowForge (skills + agents para IDEs)",
                ]));

        bool installEngram    = components.Any(c => c.StartsWith("engram-dotnet"));
        bool installFlowForge = components.Any(c => c.StartsWith("FlowForge"));

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

        // ── 4. Resumen + confirmación ─────────────────────────────────────────
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Resumen[/]").LeftJustified());
        if (installEngram)    AnsiConsole.MarkupLine($"  [green]●[/] engram-dotnet  (modo: {engramMode})");
        if (installFlowForge) AnsiConsole.MarkupLine($"  [green]●[/] FlowForge      (IDEs: {string.Join(", ", selectedIdes)})");
        AnsiConsole.WriteLine();

        if (!yes && !AnsiConsole.Confirm("¿Proceder con la instalación?", defaultValue: true))
        {
            AnsiConsole.MarkupLine("[yellow]Instalación cancelada.[/]");
            return;
        }

        // ── 5. Ejecutar instalación ───────────────────────────────────────────
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

        // ── 6. Guardar config ─────────────────────────────────────────────────
        ctx.Store.Update(c =>
        {
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
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold]Próximo paso — inicializar un proyecto:[/]");
        AnsiConsole.MarkupLine("  [blue]flowforge init[/] [grey]<ruta-del-proyecto>[/]");
        AnsiConsole.MarkupLine("  [grey]Crea AGENTS.md, .flowforge.json, docs/ y packs IDE para ese repositorio.[/]");
        ctx.Log.Info("install: completado");
    }
}
