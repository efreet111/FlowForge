using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Update;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge (sin args) — muestra versión instalada, estado de componentes
/// y notificación de update si hay versión más nueva.
/// </summary>
public static class StatusCommand
{
    const string InstallerVersion = "0.1.0-alpha.13";

    public static void Run(InstallerContext ctx)
    {
        var cfg = ctx.Store.Load();
        var registry = new ComponentRegistry(ctx.Store);
        var versions = registry.GetAllVersions();

        AnsiConsole.Write(new Rule("[bold blue]FlowForge[/]").LeftJustified());
        AnsiConsole.MarkupLine($"  Versión: [bold]v{InstallerVersion}[/]  |  Canal: [cyan]{cfg.Channel}[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Simple).HideHeaders();
        table.AddColumn("Componente");
        table.AddColumn("Estado");
        table.AddColumn("Versión");

        var engram = cfg.Components.EngramDotnet;
        table.AddRow(
            "engram-dotnet",
            engram?.Installed == true ? "[green]instalado[/]" : "[grey]no instalado[/]",
            engram?.Installed == true ? engram.Version : "-"
        );

        var ff = cfg.Components.FlowForge;
        table.AddRow(
            "FlowForge",
            ff?.Installed == true ? "[green]instalado[/]" : "[grey]no instalado[/]",
            ff?.Installed == true ? ff.Version : "-"
        );

        var flowdoc = cfg.Components.FlowDoc;
        table.AddRow(
            "FlowDoc",
            flowdoc?.Installed == true ? "[green]instalado[/]" : (cfg.FlowDoc.Enabled ? "[green]habilitado[/]" : "[grey]deshabilitado[/]"),
            flowdoc?.Installed == true ? flowdoc.Version : (cfg.FlowDoc.Enabled ? "-" : "-")
        );

        var installer = cfg.Components.Installer;
        table.AddRow(
            "Installer",
            installer?.Installed == true ? "[green]instalado[/]" : "[green]activo[/]",
            installer?.Installed == true ? installer.Version : InstallerVersion
        );

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Para instalar: [/][bold]flowforge install[/]");
        AnsiConsole.MarkupLine("[grey]Para actualizar: [/][bold]flowforge update[/]");
        AnsiConsole.MarkupLine("[grey]Para actualizar un componente: [/][bold]flowforge update --component engram[/]");
    }
}
