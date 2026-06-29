using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using Spectre.Console;

namespace FlowForge.Installer.Modules;

/// <summary>
/// Instala y actualiza el binario engram-dotnet y configura MCP para los IDEs.
/// </summary>
public sealed class EngramModule(InstallerContext ctx)
{
    public async Task InstallAsync(string mode)
    {
        AnsiConsole.MarkupLine("[bold]Instalando engram-dotnet...[/]");
        ctx.Log.Info($"EngramModule.Install: mode={mode}");

        // Determinar la versión más reciente
        string? version = null;
        await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync("Buscando última versión...", async _ =>
            {
                var cfg = ctx.Store.Load();
                version = await ctx.GitHub.GetLatestVersionAsync("efreet111/engram-dotnet", cfg.Channel);
            });

        if (version == null)
        {
            AnsiConsole.MarkupLine("[red]Error: no se pudo obtener la versión de engram-dotnet desde GitHub.[/]");
            AnsiConsole.MarkupLine("[grey]Verificá tu conexión a internet o instalá manualmente desde:[/]");
            AnsiConsole.MarkupLine("[grey]https://github.com/efreet111/engram-dotnet/releases[/]");
            ctx.Log.Error("EngramModule.Install: no se pudo obtener versión");
            return;
        }

        // Descargar
        bool ok = false;
        await AnsiConsole.Progress()
            .StartAsync(async progressCtx =>
            {
                var task = progressCtx.AddTask($"Descargando engram-dotnet {version}");
                task.IsIndeterminate = true;
                ok = await ctx.GitHub.DownloadEngramAsync(version, PathHelper.EngramBinary);
                task.Value = 100;
            });

        if (!ok)
        {
            AnsiConsole.MarkupLine("[red]Error al descargar engram-dotnet. Ver ~/.engram/install.log para detalles.[/]");
            return;
        }

        // Agregar al PATH si no está
        EnsureInPath(PathHelper.EngramBinDir);

        // Descargar librería nativa SQLite desde el release (e_sqlite3.so / .dll)
        // Si el release no la incluye, EnsureNativeLib crea symlink como fallback en Linux
        await ctx.GitHub.DownloadNativeSqliteLibAsync(version);
        EnsureNativeLib(PathHelper.EngramBinDir);

        // Registrar en config
        ctx.Store.Update(cfg =>
        {
            cfg.Components.EngramDotnet = new ComponentEntry
            {
                Installed    = true,
                Version      = version,
                Binary       = PathHelper.EngramBinary,
                RegisteredAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            };
        });

        AnsiConsole.MarkupLine($"  [green]✓[/] engram-dotnet {version} instalado en [grey]{PathHelper.EngramBinary}[/]");

        // Configurar MCP según modo
        ConfigureMcp(mode);
        ctx.Log.Info($"EngramModule.Install: completado {version}");
    }

    public async Task UpdateAsync(string newVersion)
    {
        AnsiConsole.MarkupLine($"Actualizando engram-dotnet a [bold]{newVersion}[/]...");
        ctx.Log.Info($"EngramModule.Update: {newVersion}");

        bool ok = false;
        await AnsiConsole.Progress()
            .StartAsync(async progressCtx =>
            {
                var task = progressCtx.AddTask($"Descargando {newVersion}");
                task.IsIndeterminate = true;
                ok = await ctx.GitHub.DownloadEngramAsync(newVersion, PathHelper.EngramBinary);
                task.Value = 100;
            });

        if (!ok)
        {
            AnsiConsole.MarkupLine("[red]Error al actualizar. Ver ~/.engram/install.log para detalles.[/]");
            return;
        }

        ctx.Store.Update(cfg =>
        {
            if (cfg.Components.EngramDotnet != null)
                cfg.Components.EngramDotnet.Version = newVersion;
        });

        AnsiConsole.MarkupLine($"  [green]✓[/] engram-dotnet actualizado a {newVersion}");
        ctx.Log.Info($"EngramModule.Update: completado {newVersion}");
    }

    void ConfigureMcp(string mode)
    {
        var home     = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var idePaths = PathHelper.GetIdePaths(home);
        var dataDir  = PathHelper.EngramDir;
        var user     = Environment.GetEnvironmentVariable("ENGRAM_USER")
                       ?? $"{Environment.UserName}@local.dev";
        var syncEnabled = mode == "sync";

        // Configurar Cursor si existe ~/.cursor/
        if (Directory.Exists(Path.Combine(home, ".cursor")))
        {
            WriteMcpJson(idePaths.Cursor, user, dataDir, syncEnabled, "cursor");
        }

        // OpenCode: merge into existing opencode.jsonc (or create opencode.json)
        if (Directory.Exists(Path.Combine(home, ".config", "opencode")))
        {
            MergeOpenCodeMcp(Path.Combine(home, ".config", "opencode"),
                             user, dataDir, syncEnabled);
        }

        AnsiConsole.MarkupLine("[grey]  MCP configurado para los IDEs detectados.[/]");
    }

    /// <summary>
    /// Merge engram MCP config into the user's opencode.jsonc (or opencode.json).
    /// Preserves all existing keys (mcp, provider, agent, permission, etc).
    /// </summary>
    static void MergeOpenCodeMcp(string opencodeDir, string user, string dataDir, bool syncEnabled)
    {
        try
        {
            // Prefer .jsonc (user-editable with comments), fall back to .json
            var configPath = File.Exists(Path.Combine(opencodeDir, "opencode.jsonc"))
                ? Path.Combine(opencodeDir, "opencode.jsonc")
                : Path.Combine(opencodeDir, "opencode.json");

            Directory.CreateDirectory(opencodeDir);

            // Parse existing config as JsonDocument (AOT-safe with source-gen).
            // We don't try to preserve exact types — we just preserve the text and
            // do a surgical replacement of the engram MCP block.
            var existingText = File.Exists(configPath)
                ? File.ReadAllText(configPath)
                : "{}";

            // If existing file is empty or not JSON, start fresh
            string canonicalText;
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(existingText);
                canonicalText = doc.RootElement.GetRawText();
            }
            catch
            {
                canonicalText = "{}";
            }

            // Re-parse into a mutable JsonNode tree
            var node = System.Text.Json.Nodes.JsonNode.Parse(canonicalText)!;

            // Build env dict
            var env = new Dictionary<string, string>
            {
                ["ENGRAM_DATA_DIR"]     = dataDir,
                ["ENGRAM_USER"]         = user,
                ["ENGRAM_SYNC_ENABLED"] = syncEnabled.ToString().ToLower(),
            };
            if (syncEnabled)
            {
                var serverUrl = Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL")
                                ?? "http://192.168.0.178:7437";
                env["ENGRAM_SERVER_URL"] = serverUrl;
            }

            // Build engram MCP entry as JsonNode
            var engramNode = new System.Text.Json.Nodes.JsonObject
            {
                ["type"]    = "stdio",
                ["command"] = new System.Text.Json.Nodes.JsonArray(
                    PathHelper.EngramBinary, "mcp"),
                ["environment"] = new System.Text.Json.Nodes.JsonObject()
                {
                    ["ENGRAM_DATA_DIR"]     = dataDir,
                    ["ENGRAM_USER"]         = user,
                    ["ENGRAM_SYNC_ENABLED"] = syncEnabled.ToString().ToLower(),
                },
            };
            if (syncEnabled)
            {
                ((System.Text.Json.Nodes.JsonObject)engramNode["environment"]!)
                    ["ENGRAM_SERVER_URL"] = env["ENGRAM_SERVER_URL"];
            }

            // Get or create mcp section as JsonObject
            if (node["mcp"] is not System.Text.Json.Nodes.JsonObject mcpNode)
            {
                mcpNode = new System.Text.Json.Nodes.JsonObject();
                node["mcp"] = mcpNode;
            }
            mcpNode["engram"] = engramNode;

            // Serialize back
            var json = node.ToJsonString(
                new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json + Environment.NewLine);
        }
        catch
        {
            // Non-fatal
        }
    }

    static void WriteMcpJson(string configPath, string user, string dataDir, bool syncEnabled, string format)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);

            var env = new Dictionary<string, string>
            {
                ["ENGRAM_DATA_DIR"]     = dataDir,
                ["ENGRAM_USER"]         = user,
                ["ENGRAM_SYNC_ENABLED"] = syncEnabled.ToString().ToLower(),
            };

            if (syncEnabled)
            {
                var serverUrl = Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL")
                                ?? "http://192.168.0.178:7437";
                env["ENGRAM_SERVER_URL"] = serverUrl;
            }

            string json;
            if (format == "opencode")
            {
                var cfg = new McpOpenCodeConfig
                {
                    Mcp = new Dictionary<string, McpOpenCodeEntry>
                    {
                        ["engram"] = new McpOpenCodeEntry
                        {
                            Type        = "stdio",
                            Command     = [PathHelper.EngramBinary, "mcp"],
                            Environment = env,
                        }
                    }
                };
                json = System.Text.Json.JsonSerializer.Serialize(cfg, McpJsonContext.Default.McpOpenCodeConfig);
            }
            else
            {
                var cfg = new McpCursorConfig
                {
                    McpServers = new Dictionary<string, McpStdioEntry>
                    {
                        ["engram"] = new McpStdioEntry
                        {
                            Type    = "stdio",
                            Command = PathHelper.EngramBinary,
                            Args    = ["mcp"],
                            Env     = env,
                        }
                    }
                };
                json = System.Text.Json.JsonSerializer.Serialize(cfg, McpJsonContext.Default.McpCursorConfig);
            }

            File.WriteAllText(configPath, json + Environment.NewLine);
        }
        catch
        {
            // No-op — MCP config failure is non-fatal
        }
    }

    static void EnsureInPath(string dir)
    {
        if (OperatingSystem.IsWindows())
        {
            var current = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? "";
            if (!current.Contains(dir, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable("PATH",
                    current + ";" + dir, EnvironmentVariableTarget.User);
                AnsiConsole.MarkupLine($"  [green]✓[/] Agregado al PATH: {dir} (reiniciá la terminal)");
            }
        }
        else
        {
            // En Linux/macOS ~/.local/bin suele ya estar en PATH para la mayoría de distros
            AnsiConsole.MarkupLine($"  [grey]ℹ[/]  Asegurate de tener [bold]~/.local/bin[/] en tu $PATH");
        }
    }

    /// <summary>
    /// Asegura que la librería nativa SQLite (e_sqlite3.so / e_sqlite3.dll)
    /// esté disponible en el directorio del binario. PublishSingleFile deja
    /// las dependencias nativas sueltas; si no se copiaron al release, creamos
    /// un symlink a la librería del sistema como fallback.
    /// </summary>
    void EnsureNativeLib(string binDir)
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                var nativeLib = Path.Combine(binDir, "libe_sqlite3.so");
                if (File.Exists(nativeLib))
                {
                    AnsiConsole.MarkupLine($"  [green]✓[/] libe_sqlite3.so encontrado");
                    return;
                }

                // Buscar libsqlite3.so en el sistema
                string? systemLib = null;
                foreach (var candidate in new[] {
                    "/usr/lib/libsqlite3.so",
                    "/usr/lib/libsqlite3.so.0",
                    "/usr/lib/x86_64-linux-gnu/libsqlite3.so",
                    "/usr/lib/x86_64-linux-gnu/libsqlite3.so.0",
                })
                {
                    if (File.Exists(candidate)) { systemLib = candidate; break; }
                }

                if (systemLib != null)
                {
                    File.CreateSymbolicLink(nativeLib, systemLib);
                    AnsiConsole.MarkupLine($"  [green]✓[/] Symlink creado: libe_sqlite3.so → {systemLib}");
                }
                else
                {
                    AnsiConsole.MarkupLine("  [yellow]⚠[/] libsqlite3.so no encontrada en el sistema.");
                    AnsiConsole.MarkupLine("  [grey]   Instalá sqlite3: sudo apt install libsqlite3-0 / sudo pacman -S sqlite3[/]");
                }
            }
            else if (OperatingSystem.IsWindows())
            {
                var nativeLib = Path.Combine(binDir, "e_sqlite3.dll");
                if (!File.Exists(nativeLib))
                {
                    AnsiConsole.MarkupLine("  [yellow]⚠[/] e_sqlite3.dll no encontrado — SQLite puede fallar.");
                    AnsiConsole.MarkupLine("  [grey]   Asegurate de descargar e_sqlite3.dll del release de engram-dotnet.[/]");
                }
            }
        }
        catch (Exception ex)
        {
            ctx.Log.Error($"EnsureNativeLib: {ex.Message}");
            AnsiConsole.MarkupLine($"  [yellow]⚠[/] No se pudo verificar lib nativa SQLite: {ex.Message}");
        }
    }
}
