using ConsoleAppFramework;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge config set &lt;key&gt; &lt;value&gt;
/// flowforge config get &lt;key&gt;
/// </summary>
public sealed class ConfigCommand(InstallerContext ctx)
{
    /// <param name="key">Clave: channel | auto_update | flowdoc.enabled</param>
    /// <param name="value">Valor a asignar</param>
    [Command("set")]
    public void Set(string key, string value)
    {
        ctx.Store.Update(cfg =>
        {
            switch (key.ToLowerInvariant())
            {
                case "channel":
                    if (value is not ("stable" or "beta" or "nightly"))
                    {
                        AnsiConsole.MarkupLine("[red]Canal inválido. Válidos: stable, beta, nightly[/]");
                        return;
                    }
                    cfg.Channel = value;
                    AnsiConsole.MarkupLine($"[green]✓[/] channel = {value}");
                    break;

                case "auto_update":
                    cfg.AutoUpdate = value.ToLowerInvariant() is "true" or "1" or "yes";
                    AnsiConsole.MarkupLine($"[green]✓[/] auto_update = {cfg.AutoUpdate}");
                    break;

                case "flowdoc.enabled":
                    cfg.FlowDoc.Enabled = value.ToLowerInvariant() is "true" or "1" or "yes";
                    AnsiConsole.MarkupLine($"[green]✓[/] flowdoc.enabled = {cfg.FlowDoc.Enabled}");
                    break;

                default:
                    AnsiConsole.MarkupLine($"[red]Clave desconocida: {key}[/]");
                    AnsiConsole.MarkupLine("[grey]Claves disponibles: channel, auto_update, flowdoc.enabled[/]");
                    break;
            }
        });
    }

    /// <param name="key">Clave a consultar</param>
    [Command("get")]
    public void Get(string key)
    {
        var cfg = ctx.Store.Load();
        var val = key.ToLowerInvariant() switch
        {
            "channel"        => cfg.Channel,
            "auto_update"    => cfg.AutoUpdate.ToString().ToLower(),
            "flowdoc.enabled"=> cfg.FlowDoc.Enabled.ToString().ToLower(),
            _                => null
        };

        if (val == null)
            AnsiConsole.MarkupLine($"[red]Clave desconocida: {key}[/]");
        else
            AnsiConsole.MarkupLine($"{key} = [bold]{val}[/]");
    }
}
