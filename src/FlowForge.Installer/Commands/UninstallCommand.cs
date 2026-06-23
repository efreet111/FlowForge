using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge uninstall — elimina binario, config y skills de IDEs.
/// </summary>
public sealed class UninstallCommand(InstallerContext ctx)
{
    /// <param name="yes">-y: confirmar sin prompt</param>
    [Command("")]
    public void Run(bool yes = false)
    {
        AnsiConsole.Write(new Rule("[bold red]FlowForge Uninstall[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine("Esto eliminará:");
        AnsiConsole.MarkupLine($"  [red]●[/] Binario: [grey]{PathHelper.EngramBinary}[/]");
        AnsiConsole.MarkupLine($"  [red]●[/] Config:  [grey]{PathHelper.EngramDir}[/]");
        AnsiConsole.MarkupLine("  [red]●[/] Skills de FlowForge de los IDEs instalados");
        AnsiConsole.WriteLine();

        if (!yes && !AnsiConsole.Confirm("[bold red]¿Confirmar desinstalación?[/]", defaultValue: false))
        {
            AnsiConsole.MarkupLine("[yellow]Desinstalación cancelada.[/]");
            return;
        }

        ctx.Log.Info("uninstall: inicio");

        var cfg = ctx.Store.Load();
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var idePaths = PathHelper.GetIdePaths(home);

        // Binario engram
        RemoveFile(PathHelper.EngramBinary, "engram binary");

        // Binario flowforge
        RemoveFile(PathHelper.InstallerBinary, "flowforge binary");

        // Skills de FlowForge por IDE
        if (cfg.Components.FlowForge?.Installed == true)
        {
            foreach (var ide in cfg.Components.FlowForge.Ides)
            {
                RemoveFlowForgeSkills(ide, home);
            }
        }

        // MCP entries de engram en archivos de config de IDEs
        RemoveMcpEntry(idePaths.Cursor, "cursor");
        RemoveMcpEntry(idePaths.OpenCode, "opencode");

        // Directorio ~/.engram/ completo (última operación — contiene el log)
        if (Directory.Exists(PathHelper.EngramDir))
        {
            try
            {
                Directory.Delete(PathHelper.EngramDir, recursive: true);
                AnsiConsole.MarkupLine($"  [green]✓[/] Eliminado: {PathHelper.EngramDir}");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"  [yellow]! No se pudo eliminar {PathHelper.EngramDir}: {ex.Message}[/]");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]Desinstalación completada. ¡Gracias por usar FlowForge![/]");
    }

    void RemoveFile(string path, string label)
    {
        if (!File.Exists(path)) return;
        try
        {
            File.Delete(path);
            ctx.Log.Info($"uninstall: eliminado {path}");
            AnsiConsole.MarkupLine($"  [green]✓[/] Eliminado {label}: {path}");
        }
        catch (Exception ex)
        {
            ctx.Log.Warn($"uninstall: no se pudo eliminar {path}: {ex.Message}");
            AnsiConsole.MarkupLine($"  [yellow]! No se pudo eliminar {label}: {ex.Message}[/]");
        }
    }

    void RemoveFlowForgeSkills(string ide, string home)
    {
        var dir = ide.ToLowerInvariant() switch
        {
            "cursor"   => Path.Combine(home, ".cursor", "agents"),
            "opencode" => Path.Combine(home, ".config", "opencode", "flowforge"),
            "vs code"  => Path.Combine(home, ".vscode", "agents"),
            _          => null
        };
        if (dir == null || !Directory.Exists(dir)) return;
        try
        {
            // Solo elimina archivos forge-*.md para no borrar contenido del usuario
            foreach (var f in Directory.GetFiles(dir, "forge-*.md"))
                File.Delete(f);
            AnsiConsole.MarkupLine($"  [green]✓[/] Skills FlowForge eliminadas de {ide}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"  [yellow]! Error limpiando {ide}: {ex.Message}[/]");
        }
    }

    static void RemoveMcpEntry(string configPath, string ide)
    {
        // Elimina el bloque "engram" de mcpServers en el JSON del IDE.
        // Usa manipulación de texto mínima para no corromper el archivo.
        if (!File.Exists(configPath)) return;
        try
        {
            var json = File.ReadAllText(configPath);
            if (!json.Contains("\"engram\"")) return;
            // Estrategia simple: si solo hay engram, dejar mcpServers vacío
            // Para una implementación completa se necesita un JSON parser
            // Por ahora: advertir al usuario
            AnsiConsole.MarkupLine($"  [yellow]![/] MCP de engram en {ide}: editar manualmente {configPath}");
        }
        catch { }
    }
}
