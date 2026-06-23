using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge update [--check]
/// </summary>
public sealed class UpdateCommand(InstallerContext ctx)
{
    const string InstallerVersion = "0.1.0-alpha.1";

    /// <param name="check">Solo verificar — no instalar</param>
    /// <param name="yes">-y: confirmar sin prompt</param>
    [Command("")]
    public async Task RunAsync(bool check = false, bool yes = false)
    {
        var cfg = ctx.Store.Load();
        var channel = cfg.Channel;

        AnsiConsole.MarkupLine($"[grey]Verificando actualizaciones (canal: {channel})...[/]");

        // Descargar manifest remoto en paralelo con la consulta de versión
        var manifestTask = ctx.Manifest.FetchAsync();
        var latestEngram = await ctx.GitHub.GetLatestVersionAsync("efreet111/engram-dotnet", channel);
        var remoteManifest = await manifestTask;
        var currentEngram = cfg.Components.EngramDotnet?.Version ?? "(no instalado)";

        bool hasUpdate = latestEngram != null
                         && latestEngram != currentEngram
                         && currentEngram != "(no instalado)";

        // Verificar compatibilidad de la nueva versión con el manifest
        string? compatError = null;
        if (hasUpdate && latestEngram != null)
        {
            compatError = ManifestClient.CheckEngramCompatibility(remoteManifest, latestEngram);
        }

        if (check)
        {
            if (hasUpdate)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️  engram-dotnet {latestEngram} disponible[/] (instalado: {currentEngram})");
                AnsiConsole.MarkupLine($"    Corré [bold]flowforge update[/] para actualizar");
                if (compatError != null)
                    AnsiConsole.MarkupLine($"    [red]Nota de compatibilidad: {compatError}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]✓[/] engram-dotnet {currentEngram} (última versión)");
            }

            // También verificar si el installer mismo tiene update
            var latestInstaller = await ctx.GitHub.GetLatestVersionAsync("efreet111/FlowForge", channel);
            if (latestInstaller != null && latestInstaller != InstallerVersion)
                AnsiConsole.MarkupLine($"[yellow]⚠️  flowforge installer {latestInstaller} disponible[/] (instalado: {InstallerVersion})");

            return;
        }

        if (compatError != null)
        {
            AnsiConsole.MarkupLine($"[red]Incompatibilidad de versión:[/] {compatError}");
            return;
        }

        if (!hasUpdate)
        {
            AnsiConsole.MarkupLine($"[green]✓[/] Ya tenés la última versión de engram-dotnet ({currentEngram}).");
            return;
        }

        AnsiConsole.MarkupLine($"[bold]engram-dotnet[/] {currentEngram} → [green]{latestEngram}[/]");

        if (!cfg.AutoUpdate && !yes)
        {
            if (!AnsiConsole.Confirm($"¿Actualizar a {latestEngram}?", defaultValue: true))
            {
                AnsiConsole.MarkupLine("[yellow]Actualización cancelada.[/]");
                return;
            }
        }

        var module = new EngramModule(ctx);
        await module.UpdateAsync(latestEngram!);
    }
}
