using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules;
using FlowForge.Installer.Update;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge update [--component engram|flowforge-skills|all] [--check] [--force] [--tag TAG]
/// </summary>
public sealed class UpdateCommand(InstallerContext ctx)
{
    const string InstallerVersion = "0.1.0-alpha.13";

    /// <param name="check">Solo verificar — no instalar</param>
    /// <param name="yes">-y: confirmar sin prompt</param>
    /// <param name="component">Componente a actualizar: engram, flowforge-skills, flowdoc, all</param>
    /// <param name="force">Forzar actualización incluso con procesos en ejecución</param>
    /// <param name="tag">Git tag para skills pinning (FR-004/SEC-003)</param>
    [Command("")]
    public async Task RunAsync(
        bool check = false,
        bool yes = false,
        string component = "engram",
        bool force = false,
        string? tag = null)
    {
        var cfg = ctx.Store.Load();
        var channel = cfg.Channel;

        // Parse component
        if (!TryParseComponent(component, out var updateComponent))
        {
            AnsiConsole.MarkupLine($"[red]Componente inválido: {component}[/]");
            AnsiConsole.MarkupLine("[grey]Valores válidos: engram, flowforge-skills, flowdoc, all[/]");
            return;
        }

        // --check mode: show versions without updating
        if (check)
        {
            await ShowCheckStatusAsync(channel, updateComponent);
            return;
        }

        // Delegate to orchestrator
        var orchestrator = new UpdateOrchestrator(ctx);
        var options = new UpdateOptions(
            updateComponent, yes, force, tag, null);

        AnsiConsole.MarkupLine($"[grey]Verificando actualizaciones (canal: {channel})...[/]");

        var results = await orchestrator.RunAsync(options);

        // Display results
        AnsiConsole.WriteLine();
        foreach (var result in results)
        {
            var statusText = result.Status switch
            {
                UpdateStatus.Success => "[green]✓ actualizado[/]",
                UpdateStatus.SkippedAlreadyLatest => "[grey]⋯ ya actualizado[/]",
                UpdateStatus.SkippedUserChoice => "[yellow]⊘ cancelado[/]",
                UpdateStatus.Failed => $"[red]✗ error[/]",
                UpdateStatus.RolledBack => "[yellow]↩ rollback[/]",
                _ => "[grey]?[/]"
            };

            var componentName = result.Component switch
            {
                UpdateComponent.Engram => "engram-dotnet",
                UpdateComponent.FlowForgeSkills => "FlowForge skills",
                UpdateComponent.FlowDoc => "FlowDoc",
                UpdateComponent.Installer => "Installer",
                _ => result.Component.ToString()
            };

            AnsiConsole.MarkupLine($"  {statusText} {componentName}: {result.OldVersion} → {result.NewVersion}");

            if (result.ErrorMessage != null)
                AnsiConsole.MarkupLine($"    [grey]{result.ErrorMessage}[/]");
        }
    }

    async Task ShowCheckStatusAsync(string channel, UpdateComponent component)
    {
        var registry = new ComponentRegistry(ctx.Store);
        var versions = registry.GetAllVersions();

        AnsiConsole.MarkupLine($"[grey]Estado de componentes (canal: {channel}):[/]");
        AnsiConsole.WriteLine();

        var table = new Table().Border(TableBorder.Simple);
        table.AddColumn("Componente");
        table.AddColumn("Instalado");
        table.AddColumn("Última");

        if (component == UpdateComponent.Engram || component == UpdateComponent.All)
        {
            var current = versions.GetValueOrDefault("engram") ?? "(no instalado)";
            var latest = await ctx.GitHub.GetLatestVersionAsync("efreet111/engram-dotnet", channel)
                ?? "(error)";
            var isLatest = current == latest ? "[green]✓[/]" : "[yellow]⚠ update available[/]";
            table.AddRow("engram-dotnet", current, $"{latest} {isLatest}");
        }

        if (component == UpdateComponent.FlowForgeSkills || component == UpdateComponent.All)
        {
            var current = versions.GetValueOrDefault("flowforge-skills") ?? "(no instalado)";
            table.AddRow("FlowForge skills", current, "-");
        }

        if (component == UpdateComponent.FlowDoc || component == UpdateComponent.All)
        {
            var current = versions.GetValueOrDefault("flowdoc") ?? "(no instalado)";
            table.AddRow("FlowDoc", current, "-");
        }

        AnsiConsole.Write(table);
    }

    static bool TryParseComponent(string input, out UpdateComponent component)
    {
        component = input.ToLowerInvariant() switch
        {
            "engram" => UpdateComponent.Engram,
            "flowforge-skills" or "skills" or "flowforge" => UpdateComponent.FlowForgeSkills,
            "flowdoc" => UpdateComponent.FlowDoc,
            "installer" => UpdateComponent.Installer,
            "all" => UpdateComponent.All,
            _ => (UpdateComponent)(-1)
        };
        return (int)component >= 0;
    }
}
