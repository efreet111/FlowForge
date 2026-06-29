using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using Spectre.Console;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace FlowForge.Installer.Commands;

public sealed class DoctorCommand(InstallerContext ctx)
{
    // ctx disponible para futura extensión (logs, store)
    readonly InstallerContext _ctx = ctx;

    [Command("")]
    public async Task<int> RunAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[bold]FlowForge Doctor[/] — diagnóstico del sistema");
            AnsiConsole.WriteLine();

            var checks = new List<(string Name, Func<Task<(bool Passed, string? Hint)>> Check)>
            {
                ("flowforge binary",    () => CheckFileAsync(PathHelper.InstallerBinary)),
                ("engram binary",       () => CheckFileAsync(PathHelper.EngramBinary)),
                ("engram en PATH",      () => Task.FromResult(CheckPath(PathHelper.EngramBinDir))),
                ("MCP configurado",     () => Task.FromResult(CheckMcp())),
                ("GitHub reachable",    () => CheckGitHubAsync()),
            };

            var results = new List<DoctorCheck>();
            foreach (var (name, check) in checks)
            {
                var (passed, hint) = await check();
                results.Add(new DoctorCheck(name, passed, hint));
            }

            var table = new Table().Border(TableBorder.Rounded);
            table.AddColumn("Check");
            table.AddColumn("Estado");
            table.AddColumn("Detalle");

            foreach (var (name, passed, hint) in results)
            {
                var status = passed ? "[green]✓ OK[/]" : "[red]✗ FAIL[/]";
                var detail = hint ?? (passed ? string.Empty : "ver hints abajo");
                table.AddRow(name, status, detail);
            }

            AnsiConsole.Write(table);

            var failures = results.Count(r => !r.Passed);
            if (failures > 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]Hints:[/]");
                foreach (var (name, passed, hint) in results.Where(r => !r.Passed))
                {
                    var detail = hint ?? "ejecutá `flowforge install` para reinstalar";
                    AnsiConsole.MarkupLine($"  [grey]{name}[/]: {detail}");
                }
                return 2;
            }

            AnsiConsole.MarkupLine("[green]Todo OK.[/]");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error[/] {ex.Message}");
            return 1;
        }
    }

    readonly record struct DoctorCheck(string Name, bool Passed, string? Hint = null);

    static Task<(bool, string?)> CheckFileAsync(string path)
    {
        var exists = File.Exists(path);
        var hint = exists ? null : $"No encontrado en {path}. Ejecutá `flowforge install`.";
        return Task.FromResult((exists, hint));
    }

    static (bool, string?) CheckPath(string dir)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var inPath = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(p => string.Equals(p.TrimEnd('/','\\'), dir.TrimEnd('/','\\'), OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
        string? hint = null;
        if (!inPath)
        {
            hint = OperatingSystem.IsWindows()
                ? $"Agregá a PATH (PowerShell): $env:PATH += ';{dir}'"
                : $"Agregá a PATH: export PATH=\"{dir}:$PATH\"";
        }
        return (inPath, hint);
    }

    static (bool, string?) CheckMcp()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var cursorMcp = Path.Combine(home, ".cursor", "mcp.json");
        var opencodeMcp = Path.Combine(home, ".config", "opencode", "opencode.jsonc");
        var opencodeMcp2 = Path.Combine(home, ".config", "opencode", "opencode.json");

        var configured = (File.Exists(cursorMcp) && File.ReadAllText(cursorMcp).Contains("engram"))
                       || (File.Exists(opencodeMcp) && File.ReadAllText(opencodeMcp).Contains("engram"))
                       || (File.Exists(opencodeMcp2) && File.ReadAllText(opencodeMcp2).Contains("engram"));

        var hint = configured ? null : "Ejecutá `flowforge install` para configurar MCP.";
        return (configured, hint);
    }

    static async Task<(bool, string?)> CheckGitHubAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            http.DefaultRequestHeaders.Add("User-Agent", "flowforge-doctor/0.1.0");
            using var resp = await http.GetAsync("https://api.github.com");
            return (resp.IsSuccessStatusCode, resp.IsSuccessStatusCode ? null : $"HTTP {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return (false, $"Sin conexión: {ex.Message}");
        }
    }
}
