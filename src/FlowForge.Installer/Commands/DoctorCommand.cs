using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules;
using FlowForge.Installer.Modules.OpenCode;
using Spectre.Console;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace FlowForge.Installer.Commands;

public sealed class DoctorCommand(InstallerContext ctx)
{
    // ctx disponible para futura extensión (logs, store)
    readonly InstallerContext _ctx = ctx;

    [Command("")]
    public async Task<int> RunAsync(bool refreshModels = false, bool refreshSchema = false)
    {
        if (refreshModels)
        {
            AnsiConsole.MarkupLine("[grey]--refresh-models[/]: No implementado en v1; follow-up PR.[/]");
            return 0;
        }

        if (refreshSchema)
            return await RefreshSchemaAsync();

        try
        {
            AnsiConsole.MarkupLine("[bold]FlowForge Doctor[/] — diagnóstico del sistema");
            AnsiConsole.WriteLine();

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var projectRoot = Directory.GetCurrentDirectory();
            var checks = new List<(string Name, Func<Task<(bool Passed, string? Hint)>> Check)>
            {
                ("flowforge binary",    () => CheckFileAsync(PathHelper.InstallerBinary)),
                ("engram binary",       () => CheckFileAsync(PathHelper.EngramBinary)),
                ("engram en PATH",      () => Task.FromResult(CheckPath(PathHelper.EngramBinDir))),
                ("MCP configurado",     () => Task.FromResult(CheckMcp())),
                ("GitHub reachable",    () => CheckGitHubAsync()),
                ("VS Code: github.copilot", () => Task.FromResult(CheckVsCodeExtension("github.copilot"))),
                ("VS Code: kilocode.*",     () => Task.FromResult(CheckVsCodeExtension("kilocode."))),
                ("~/.copilot/agents/",      () => Task.FromResult(CheckDirectory(PathHelper.CopilotAgents))),
                ("~/.config/kilo/agents/",  () => Task.FromResult(CheckDirectory(PathHelper.KiloAgents))),
                ("~/.config/opencode/agents/", () => Task.FromResult(CheckDirectory(Path.Combine(home, ".config", "opencode", "agents")))),
                ("~/.cursor/agents/",       () => Task.FromResult(CheckDirectory(Path.Combine(home, ".cursor", "agents")))),
                ("~/.gemini/config/",       () => Task.FromResult(CheckDirectory(PathHelper.AntigravityConfigDir))),
            };

            if (IsFlowForgeProject(projectRoot))
            {
                checks.Add(("Project: .github/agents/", () => Task.FromResult(CheckDirectory(Path.Combine(projectRoot, ".github", "agents")))));
                checks.Add(("Project: .opencode/agents/", () => Task.FromResult(CheckDirectory(Path.Combine(projectRoot, ".opencode", "agents")))));
                checks.Add(("Project: .kilo/agents/", () => Task.FromResult(CheckDirectory(Path.Combine(projectRoot, ".kilo", "agents")))));
                checks.Add(("Project: .cursor/agents/", () => Task.FromResult(CheckDirectory(Path.Combine(projectRoot, ".cursor", "agents")))));
                checks.Add(("Project: .agents/", () => Task.FromResult(CheckDirectory(Path.Combine(projectRoot, ".agents")))));
            }

            var results = new List<DoctorCheck>();
            foreach (var (name, check) in checks)
            {
                var (passed, hint) = await check();
                results.Add(new DoctorCheck(name, passed, hint));
            }

            var openCodeResults = CheckOpenCode(home);
            results.AddRange(openCodeResults);

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

    static (bool, string?) CheckDirectory(string path)
    {
        if (!Directory.Exists(path))
            return (false, $"No existe {path}. Ejecutá `flowforge install` o `flowforge init`.");
        if (!Directory.EnumerateFileSystemEntries(path).Any())
            return (false, $"El directorio {path} está vacío.");
        return (true, null);
    }

    static (bool, string?) CheckVsCodeExtension(string prefix)
    {
        if (FlowForgeModule.HasVsCodeExtension(prefix))
            return (true, null);
        return (false, $"No se detectó VS Code: {prefix} en ~/.vscode/extensions/");
    }

    static bool IsFlowForgeProject(string root)
    {
        return Directory.Exists(Path.Combine(root, ".git"))
            || File.Exists(Path.Combine(root, "flowforge.yaml"))
            || File.Exists(Path.Combine(root, ".flowforge.json"));
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

    static async Task<int> RefreshSchemaAsync()
    {
        var schemaPath = Path.Combine(Directory.GetCurrentDirectory(), "ide", "opencode", "templates", "config.schema.json");
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.Add("User-Agent", "flowforge-doctor/0.1.0");
            var content = await http.GetStringAsync("https://opencode.ai/config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(schemaPath) ?? string.Empty);
            var header = $"/* Source: https://opencode.ai/config.json (refreshed {DateTime.UtcNow:yyyy-MM-dd}) */{Environment.NewLine}";
            File.WriteAllText(schemaPath, header + content);
            AnsiConsole.MarkupLine("[green]✓[/] Schema refrescado en ide/opencode/templates/config.schema.json.");
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] No se pudo refrescar schema: {ex.Message}");
            return 1;
        }
    }

    static List<DoctorCheck> CheckOpenCode(string home)
    {
        var results = new List<DoctorCheck>();
        var opencodeDir = Path.Combine(home, ".config", "opencode");
        if (!Directory.Exists(opencodeDir))
        {
            results.Add(new DoctorCheck("OpenCode: directory", false, "No existe ~/.config/opencode. Ejecutá `flowforge install --ide opencode`."));
            return results;
        }

        var configPath = File.Exists(Path.Combine(opencodeDir, "opencode.jsonc"))
            ? Path.Combine(opencodeDir, "opencode.jsonc")
            : Path.Combine(opencodeDir, "opencode.json");

        if (!File.Exists(configPath))
        {
            results.Add(new DoctorCheck("OpenCode config", false, "No existe opencode.json/.jsonc. Ejecutá `flowforge install --ide opencode`."));
            return results;
        }

        var text = File.ReadAllText(configPath);
        JsonNode? root = null;
        try
        {
            root = JsonNode.Parse(text);
            results.Add(new DoctorCheck("OpenCode config parse", true));
        }
        catch (Exception ex)
        {
            results.Add(new DoctorCheck("OpenCode config parse", false, $"No parsea JSON: {ex.Message}"));
            return results;
        }

        var required = new[] { "$schema", "instructions", "agent", "provider", "permission", "mcp" };
        var missing = required.Where(key => root?[key] is null).ToList();
        if (missing.Any())
        {
            results.Add(new DoctorCheck("OpenCode schema keys", false, $"Faltan keys: {string.Join(", ", missing)}"));
        }
        else
        {
            results.Add(new DoctorCheck("OpenCode schema keys", true));
        }

        var mcp = root?["mcp"] as JsonObject;
        var engram = mcp?["engram"] as JsonObject;
        var type = engram?["type"]?.GetValue<string>();
        var enabled = engram?["enabled"]?.GetValue<bool>() ?? false;
        if (string.Equals(type, "local", StringComparison.OrdinalIgnoreCase) && enabled)
            results.Add(new DoctorCheck("OpenCode mcp.engram", true));
        else
            results.Add(new DoctorCheck("OpenCode mcp.engram", false, "mcp.engram debe ser local/true. Ejecutá flowforge install."));

        var agentsNode = root?["agent"] as JsonObject;
        var expectedAgents = new[] { "flowforge", "forge-discovery", "forge-arch", "forge-plan", "forge-dev", "forge-verify", "forge-memory", "forge-teacher" };
        if (agentsNode is null)
        {
            results.Add(new DoctorCheck("OpenCode agents section", false, "Falta la sección agent."));
        }
        else
        {
            var missingAgents = expectedAgents.Where(agent => agentsNode[agent] is null).ToList();
            if (missingAgents.Any())
            {
                results.Add(new DoctorCheck("OpenCode agents count", false, $"Faltan agentes: {string.Join(", ", missingAgents)}"));
            }
            else
            {
                results.Add(new DoctorCheck("OpenCode agents count", true));
            }
        }

        var providerNode = root?["provider"] as JsonObject;
        var providerModels = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        if (providerNode != null)
        {
            foreach (var kvp in providerNode)
            {
                if (kvp.Value is JsonObject providerObj && providerObj["models"] is JsonArray array)
                {
                    providerModels[kvp.Key] = array
                        .Select(v => v?.GetValue<string>())
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Select(m => m!.Split('/', StringSplitOptions.RemoveEmptyEntries).Last())
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                }
            }
        }

        var modelResolutionFailed = false;
        if (agentsNode != null && providerModels.Count > 0)
        {
            foreach (var agentName in expectedAgents)
            {
                var agentEntry = agentsNode[agentName] as JsonObject;
                var model = agentEntry?["model"]?.GetValue<string>();
                if (string.IsNullOrWhiteSpace(model) || !model.Contains('/'))
                {
                    modelResolutionFailed = true;
                    break;
                }

                var parts = model.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2)
                {
                    modelResolutionFailed = true;
                    break;
                }

                var providerName = parts[0];
                var modelId = parts[1];
                if (!providerModels.TryGetValue(providerName, out var set) || !set.Contains(modelId))
                {
                    modelResolutionFailed = true;
                    break;
                }
            }
        }

        if (modelResolutionFailed)
            results.Add(new DoctorCheck("OpenCode agent models", false, "Revisa provider/models y agent.*.model."));
        else
            results.Add(new DoctorCheck("OpenCode agent models", true));

        var scanner = new PiiScanner();
        var pii = scanner.ScanGenerated(text, home);
        results.Add(new DoctorCheck("OpenCode PII scan", pii.Clean, pii.Clean ? null : "PII detectada en opencode.json"));

        var modelAssignmentsPath = Path.Combine(opencodeDir, ".agents", "rules", "model-assignments.md");
        if (!File.Exists(modelAssignmentsPath))
        {
            results.Add(new DoctorCheck("OpenCode model-assignments", false, "No existe .agents/rules/model-assignments.md"));
        }
        else
        {
            var stalePattern = new Regex(@"claude-|gpt-|opencode-go/", RegexOptions.IgnoreCase);
            var content = File.ReadAllText(modelAssignmentsPath);
            var stale = stalePattern.IsMatch(content);
            if (stale)
                results.Add(new DoctorCheck("OpenCode model-assignments", false, "Archivo stale con modelos no disponibles."));
            else
                results.Add(new DoctorCheck("OpenCode model-assignments", true));
        }

        var agentsDir = Path.Combine(opencodeDir, "agents");
        var frontFailures = 0;
        if (!Directory.Exists(agentsDir))
        {
            results.Add(new DoctorCheck("OpenCode agents files", false, "No existe ~/.config/opencode/agents/"));
        }
        else
        {
            foreach (var file in Directory.GetFiles(agentsDir, "*.md"))
            {
                var agentName = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(agentName)) continue;
                var frontModel = GetModelFromFrontmatter(File.ReadAllText(file));
                var expectedModel = agentsNode?[agentName]?["model"]?.GetValue<string>();
                if (expectedModel is null)
                {
                    results.Add(new DoctorCheck($"OpenCode agent {agentName}", false, "No hay modelo en opencode.json"));
                    frontFailures++;
                    continue;
                }

                if (!string.Equals(frontModel, expectedModel, StringComparison.Ordinal))
                {
                    results.Add(new DoctorCheck($"OpenCode agent {agentName}", false, "Frontmatter model desalineado"));
                    frontFailures++;
                }
            }
        }

        if (frontFailures == 0)
            results.Add(new DoctorCheck("OpenCode agent frontmatter", true));

        var sidecarPath = PathHelper.OpenCodeSidecarPath;
        if (!File.Exists(sidecarPath))
        {
            results.Add(new DoctorCheck("OpenCode sidecar", false, "No existe .flowforge-managed.json"));
        }
        else
        {
            var list = JsonSerializer.Deserialize(File.ReadAllText(sidecarPath), OpenCodeJsonContext.Default.StringArray);
            if (list == null || !list.Any(p => p.Equals("mcp.engram", StringComparison.OrdinalIgnoreCase)))
                results.Add(new DoctorCheck("OpenCode sidecar", false, "Sidecar no contiene mcp.engram"));
            else
                results.Add(new DoctorCheck("OpenCode sidecar", true));
        }

        return results;
    }

    static string? GetModelFromFrontmatter(string content)
    {
        using var reader = new StringReader(content);
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.Trim().StartsWith("model:", StringComparison.OrdinalIgnoreCase))
            {
                return line.Split(':', 2)[1].Trim();
            }
            if (string.IsNullOrWhiteSpace(line))
                break;
        }

        return null;
    }
}
