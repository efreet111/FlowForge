using System.Diagnostics;
using System.Reflection;
using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using FlowForge.Installer.Modules;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge install — wizard interactivo multi-componente.
/// Modo headless (--yes) usa defaults sin requerir TTY interactivo.
/// Flags --no-engram / --no-flowforge permiten omitir componentes.
/// </summary>
public sealed class InstallCommand(InstallerContext ctx)
{
    // Read version from assembly metadata (set by csproj <Version> or -p:Version= during publish).
    // Falls back to "dev" if not available.
    static readonly string CurrentVersion =
        typeof(InstallCommand).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(InstallCommand).Assembly.GetName().Version?.ToString()
        ?? "dev";

    /// <summary>
    /// -y / --yes: omitir confirmaciones (non-interactive)
    /// --no-engram: omitir instalación de engram-dotnet
    /// --no-flowforge: omitir instalación de skills FlowForge
    /// </summary>
    [Command("")]
    public async Task RunAsync(
        bool yes = false,
        bool noEngram = false,
        bool noFlowforge = false,
        bool forceFree = false,
        bool dryRun = false,
        bool jsonOnly = false,
        bool allowSymlink = false,
        bool noSudo = false,
        string? serverUrl = null,
        string provider = "opencode-zen")
    {
        AnsiConsole.Write(new Rule($"[bold blue]FlowForge Stack Installer[/] [grey]v{FlowForgeModule.InstallerVersion}[/]").LeftJustified());
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Conectando a GitHub...[/]");
        AnsiConsole.WriteLine();

        // Detect true headless: --yes flag OR no interactive console (CI/CD, scripts)
        // Use Spectre.Console's capability check — more reliable than Environment.UserInteractive
        // because it correctly detects whether prompts can actually be rendered.
        var canShowPrompts = AnsiConsole.Profile.Capabilities.Interactive;
        var isHeadless = yes || !canShowPrompts;

        var normalizedProvider = (provider ?? string.Empty).Trim().ToLowerInvariant();
        if (normalizedProvider != "opencode-zen" && normalizedProvider != "opencode-go")
        {
            AnsiConsole.MarkupLine("[red]✗[/] --provider debe ser 'opencode-zen' o 'opencode-go'.");
            return;
        }

        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (noSudo && !string.IsNullOrEmpty(sudoUser))
        {
            AnsiConsole.MarkupLine("[red]✗[/] --no-sudo solicitado pero el instalador corre bajo sudo. Ejecutá sin sudo.");
            Environment.Exit(3);
            return;
        }

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

        // Read existing sync config to prefill the prompt on re-runs.
        var existingSync = cfg.Sync;
        var existingUrl = existingSync?.RemoteUrl ?? "";

        bool installEngram;
        bool installFlowForge;
        string engramMode;
        string? engramSyncUrl = null;
        List<string> selectedIdes;

        if (isHeadless)
        {
            // ── Headless mode: use sensible defaults ───────────────────────────
            AnsiConsole.MarkupLine("[bold]Modo no-interactivo (--yes)[/]");
            AnsiConsole.MarkupLine("[grey]Usando defaults — ambos componentes, IDEs auto-detectados[/]");
            if (noEngram)    AnsiConsole.MarkupLine("[grey]  --no-engram: omitiendo engram-dotnet[/]");
            if (noFlowforge) AnsiConsole.MarkupLine("[grey]  --no-flowforge: omitiendo skills FlowForge[/]");
            AnsiConsole.WriteLine();

            installEngram    = !noEngram;
            installFlowForge = !noFlowforge;
            engramMode       = DetectSyncMode();
            selectedIdes     = DetectInstalledIdes();

            if (!installEngram && !installFlowForge)
            {
                AnsiConsole.MarkupLine("[red]Error: --no-engram y --no-flowforge juntos no instalan nada.[/]");
                return;
            }

            // Resolve sync URL in priority order: --server-url flag, env, persisted config.
            // In sync mode, a missing URL is a warning (not a hard error) so that
            // context7 MCP auto-install (HU-042) can still proceed. The engram module
            // and sync subsystem handle the missing URL at runtime.
            if (engramMode == "sync")
            {
                engramSyncUrl = !string.IsNullOrWhiteSpace(serverUrl)
                    ? serverUrl
                    : Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL");
                if (string.IsNullOrWhiteSpace(engramSyncUrl))
                    engramSyncUrl = existingUrl;
                if (string.IsNullOrWhiteSpace(engramSyncUrl))
                {
                    AnsiConsole.MarkupLine("[yellow]⚠ Sync mode sin URL configurada. El sync fallará hasta que se configure una URL.[/]");
                    AnsiConsole.MarkupLine("[grey]  Configurar después: flowforge install --server-url http://your-relay:7437[/]");
                    ctx.Log.Warn("install: sync mode sin URL (ENGRAM_SERVER_URL, config, o --server-url)");
                }
            }
        }
        else
        {
            // ── 1. Selección de componentes (global) ──────────────────────────
            AnsiConsole.MarkupLine("[bold]¿Qué componentes instalar?[/]");
            AnsiConsole.MarkupLine("[grey]Este wizard instala componentes globales (binario + IDE agents).[/]");
            AnsiConsole.MarkupLine("[grey]Para inicializar un proyecto usa:[/] [blue]flowforge init[/] [grey]<ruta>[/]");
            AnsiConsole.WriteLine();
            var components = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                    .Title("")
                    .InstructionsText("[grey](Espacio para seleccionar, Enter para confirmar)[/]")
                    .AddChoices([
                        "engram-dotnet (backend de memoria)",
                        "FlowForge (skills + agents para IDEs)",
                    ]));

            installEngram    = components.Any(c => c.StartsWith("engram-dotnet"));
            installFlowForge = components.Any(c => c.StartsWith("FlowForge"));

            AnsiConsole.WriteLine();

            // ── 2. Modo engram ────────────────────────────────────────────────
            engramMode = "local";
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

                if (engramMode == "sync")
                {
                    // Prompt for the relay server URL. Prefill with persisted value
                    // (re-install) or the --server-url flag if provided.
                    var promptDefault = !string.IsNullOrWhiteSpace(serverUrl)
                        ? serverUrl
                        : existingUrl;
                    engramSyncUrl = AnsiConsole.Prompt(
                        new TextPrompt<string>("[bold]URL del servidor sync[/] [grey](http://host:port)[/]:")
                            .PromptStyle("yellow")
                            .DefaultValue(promptDefault ?? "")
                            .ValidationErrorMessage("[red]URL inválida — debe ser http(s)://host:port[/]")
                            .Validate(url =>
                            {
                                if (string.IsNullOrWhiteSpace(url))
                                    return ValidationResult.Error("[red]Requerido en modo sync[/]");
                                if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                                    return ValidationResult.Error("[red]URL no parseable[/]");
                                if (uri.Scheme != "http" && uri.Scheme != "https")
                                    return ValidationResult.Error("[red]Scheme debe ser http o https[/]");
                                return ValidationResult.Success();
                            }));

                    // Warn (but don't fail) if the URL is unreachable — the user
                    // may be installing on a machine without VPN to the relay.
                    if (!ProbeServerHealth(engramSyncUrl))
                    {
                        AnsiConsole.MarkupLine($"[yellow]⚠[/] {engramSyncUrl} no responde a GET /health");
                        AnsiConsole.MarkupLine("[grey]  La instalación continúa; el sync reintentará en cada ciclo.[/]");
                    }
                }
            }

            // ── 3. IDEs para FlowForge ────────────────────────────────────────
            selectedIdes = [];
            if (installFlowForge)
            {
                AnsiConsole.MarkupLine("[bold]¿Dónde instalar los skills de FlowForge?[/]");
                selectedIdes = AnsiConsole.Prompt(
                    new MultiSelectionPrompt<string>()
                        .Title("")
                        .InstructionsText("[grey](Espacio para seleccionar, Enter para confirmar)[/]")
                        .AddChoices(["Cursor", "OpenCode", "VS Code", "Antigravity", "Claude Desktop"]));
            }
        }

        // ── 4. Resumen + confirmación (solo en modo interactivo) ───────────────
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold]Resumen[/]").LeftJustified());
        if (installEngram)    AnsiConsole.MarkupLine($"  [green]●[/] engram-dotnet  (modo: {engramMode})");
        if (engramMode == "sync" && !string.IsNullOrWhiteSpace(engramSyncUrl))
            AnsiConsole.MarkupLine($"                  [grey]sync server: {engramSyncUrl}[/]");
        if (installFlowForge) AnsiConsole.MarkupLine($"  [green]●[/] FlowForge      (IDEs: {string.Join(", ", selectedIdes)})");
        AnsiConsole.WriteLine();

        // Solo pedir confirmación en modo interactivo
        // En headless mode: ya sabemos los defaults, no preguntar
        if (!isHeadless && !AnsiConsole.Confirm("¿Proceder con la instalación?", defaultValue: true))
        {
            AnsiConsole.MarkupLine("[yellow]Instalación cancelada.[/]");
            return;
        }

        // ── 5. Ejecutar instalación ───────────────────────────────────────────
        ctx.Log.Info("install: inicio");

        if (installEngram)
        {
            var module = new EngramModule(ctx);
            await module.InstallAsync(engramMode, engramSyncUrl);
        }

        if (installFlowForge)
        {
            var module = new FlowForgeModule(ctx);
            module.Install(selectedIdes, normalizedProvider, forceFree, dryRun, jsonOnly, allowSymlink, noSudo);
        }

        // ── 5b. Auto-install context7 MCP for non-local profiles (HU-042) ────
        if (engramMode != "local" && selectedIdes.Any(i => i.Equals("OpenCode", StringComparison.OrdinalIgnoreCase)))
        {
            await InstallContext7McpAsync(engramMode, engramSyncUrl);
        }

        // ── 6. Guardar config ─────────────────────────────────────────────────
        ctx.Store.Update(c =>
        {
            if (installEngram)
            {
                // Persist sync config (the source of truth since ADR-010).
                var dataDir = PathHelper.EngramDir;
                var user    = Environment.GetEnvironmentVariable("ENGRAM_USER")
                              ?? $"{Environment.UserName}@local.dev";
                c.Sync = new SyncConfig
                {
                    Mode        = engramMode,
                    RemoteUrl   = engramSyncUrl ?? "",
                    User        = user,
                    DataDir     = dataDir,
                    ConnectedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                };
            }
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

        if (sudoUser != null && Environment.GetEnvironmentVariable("USER") == "root")
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] Instalado como root. Si el MCP falla, ejecutá:");
            AnsiConsole.MarkupLine($"[grey]  sudo chown -R {sudoUser}:{sudoUser} ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so ~/.local/bin/flowforge[/]");
        }

        ctx.Log.Info("install: completado");
    }

    /// <summary>
    /// Detecta el modo sync adecuado. Prioridad:
    ///   1. ENGRAM_SERVER_URL env var (CI / scripts)
    ///   2. ENGRAM_PROFILE env var != "local" (selección de perfil)
    ///   3. ~/.engram/config.json → sync.remote_url (re-install con persistencia)
    ///   4. local (sin URL en ningún lado)
    /// </summary>
    string DetectSyncMode()
    {
        var fromEnv = Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL");
        if (!string.IsNullOrWhiteSpace(fromEnv)) return "sync";

        var fromProfile = Environment.GetEnvironmentVariable("ENGRAM_PROFILE");
        if (!string.IsNullOrWhiteSpace(fromProfile) &&
            !fromProfile.Equals("local", StringComparison.OrdinalIgnoreCase))
            return "sync";

        try
        {
            var cfg = ctx.Store.Load();
            if (cfg.Sync != null && !string.IsNullOrWhiteSpace(cfg.Sync.RemoteUrl))
                return "sync";
        }
        catch
        {
            // No config or unreadable — fall through to local
        }

        return "local";
    }

    /// <summary>
    /// Probes the sync server's /health endpoint with a short timeout.
    /// Used to warn (not fail) the user if the URL is unreachable during install.
    /// </summary>
    static bool ProbeServerHealth(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        try
        {
            var healthUrl = $"{uri.Scheme}://{uri.Host}:{uri.Port}/health";
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var resp = http.GetAsync(healthUrl).GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Detecta IDEs instalados en el sistema sin requerir interacción del usuario.
    /// </summary>
    static List<string> DetectInstalledIdes()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var ides = new List<string>();

        if (Directory.Exists(Path.Combine(home, ".cursor")))
            ides.Add("Cursor");
        if (Directory.Exists(Path.Combine(home, ".config", "opencode")))
            ides.Add("OpenCode");
        if (Directory.Exists(Path.Combine(home, ".vscode")) ||
            Directory.Exists(Path.Combine(home, ".vscode-server")))
            ides.Add("VS Code");
        // ~/.gemini is Antigravity (Google's IDE), not Claude Desktop
        if (Directory.Exists(Path.Combine(home, ".gemini")))
            ides.Add("Antigravity");
        // Claude Desktop uses ~/.config/Claude/ on Linux, %APPDATA%\Claude on Windows
        if (Directory.Exists(Path.Combine(home, ".config", "Claude")) ||
            (OperatingSystem.IsWindows() && Directory.Exists(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Claude"))))
            ides.Add("Claude Desktop");

        return ides;
    }

    /// <summary>
    /// Si el perfil de Engram es non-local (sync/remote), instala automáticamente
    /// el servidor MCP context7 requerido por OpenCode v2 para conexiones MCP remotas (HU-042).
    /// </summary>
    /// <param name="engramMode">Modo resuelto por el wizard ("local" o "sync").</param>
    /// <param name="engramSyncUrl">URL de sync resuelta (puede ser null/empty).</param>
    /// <remarks>
    /// Lógica de detección (fuente de verdad = parámetros ya resueltos):
    /// - engramMode != "local" → non-local (interactivo o headless con ENGRAM_PROFILE/ENGRAM_SERVER_URL)
    /// - engramSyncUrl no vacío → non-local (URL explícita)
    /// Fallback a env vars/config para casos donde el método se llama sin contexto.
    /// </remarks>
    async Task InstallContext7McpAsync(string engramMode, string? engramSyncUrl)
    {
        AnsiConsole.MarkupLine("[grey]Verificando perfil de Engram para auto-instalación de context7 MCP...[/]");

        // 1. Determinar si el perfil es non-local usando la fuente de verdad
        //    (parámetros ya resueltos por el wizard o DetectSyncMode).
        var isNonLocal = engramMode != "local" || !string.IsNullOrWhiteSpace(engramSyncUrl);

        // Fallback a env vars/config si los parámetros no indican non-local
        // (casos edge: método llamado sin contexto completo)
        if (!isNonLocal)
        {
            var envUrl = Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL");
            if (!string.IsNullOrWhiteSpace(envUrl))
            {
                isNonLocal = true;
                AnsiConsole.MarkupLine($"[green]✓[/] Perfil non-local detectado vía ENGRAM_SERVER_URL: {envUrl}");
            }
            else
            {
                var envProfile = Environment.GetEnvironmentVariable("ENGRAM_PROFILE") ?? "local";
                if (!envProfile.Equals("local", StringComparison.OrdinalIgnoreCase))
                {
                    isNonLocal = true;
                    AnsiConsole.MarkupLine($"[green]✓[/] Perfil non-local detectado vía ENGRAM_PROFILE: {envProfile}");
                }
                else
                {
                    var configPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        ".engram", "config.json");

                    if (File.Exists(configPath))
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(configPath);
                            if (json.Contains("sync", StringComparison.OrdinalIgnoreCase) &&
                                json.Contains("remote_url", StringComparison.OrdinalIgnoreCase))
                            {
                                isNonLocal = true;
                                AnsiConsole.MarkupLine($"[green]✓[/] Perfil non-local detectado vía {configPath}");
                            }
                        }
                        catch
                        {
                            // Ignorar errores de lectura; continuar con perfil local
                        }
                    }
                }
            }
        }
        else
        {
            // Parámetros ya indicaban non-local — loggear la fuente
            if (engramMode == "sync")
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Perfil non-local detectado (engramMode=sync)");
                ctx.Log.Info($"context7-auto-install: engramMode=sync, syncUrl={engramSyncUrl ?? "(null)"}");
            }
            else if (!string.IsNullOrWhiteSpace(engramSyncUrl))
            {
                AnsiConsole.MarkupLine($"[green]✓[/] Perfil non-local detectado (syncUrl={engramSyncUrl})");
                ctx.Log.Info($"context7-auto-install: engramMode={engramMode}, syncUrl={engramSyncUrl}");
            }
        }

        if (!isNonLocal)
        {
            AnsiConsole.MarkupLine("[grey]→ Perfil local detectado; omitiendo context7 MCP[/]");
            ctx.Log.Info("context7-auto-install: skipped (local profile)");
            return;
        }

        // 2. Verificar si opencode está instalado
        if (!IsCommandAvailable("opencode"))
        {
            AnsiConsole.MarkupLine("[yellow]![/] OpenCode no está instalado; omitiendo context7 MCP");
            ctx.Log.Warn("context7-auto-install: skipped (opencode not found)");
            return;
        }

        // 3. Verificar idempotencia: ¿ya está instalado?
        try
        {
            var mcpList = await RunCommandAsync("opencode", "mcp list");
            if (mcpList.Contains("context7", StringComparison.OrdinalIgnoreCase))
            {
                AnsiConsole.MarkupLine("[green]✓[/] context7 MCP ya está instalado");
                ctx.Log.Info("context7-auto-install: already installed (idempotent)");
                return;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]![/] No se pudo verificar idempotencia: {ex.Message}");
            ctx.Log.Warn($"context7-auto-install: idempotency check failed: {ex.Message}");
            // Continuar con la instalación de todas formas
        }

        // 4. Instalar context7 MCP
        AnsiConsole.MarkupLine("[grey]→ Instalando context7 MCP...[/]");
        try
        {
            var output = await RunCommandAsync(
                "opencode",
                "mcp add context7 --url https://mcp.context7.com/mcp --global");

            if (output.Contains("added", StringComparison.OrdinalIgnoreCase) ||
                output.Contains("success", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(output)) // Algunos CLIs no imprimen nada en éxito
            {
                AnsiConsole.MarkupLine("[green]✓[/] context7 MCP instalado correctamente");
                ctx.Log.Info("context7-auto-install: installed successfully");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]![/] context7 MCP instalado con advertencias: {output}");
                ctx.Log.Warn($"context7-auto-install: installed with warnings: {output}");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] No se pudo instalar context7 MCP: {ex.Message}");
            AnsiConsole.MarkupLine("[grey]  Instalación manual requerida:[/]");
            AnsiConsole.MarkupLine("[grey]    opencode mcp add context7 --url https://mcp.context7.com/mcp --global[/]");
            ctx.Log.Error($"context7-auto-install: failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Verifica si un comando está disponible en el PATH del sistema.
    /// </summary>
    static bool IsCommandAvailable(string command)
    {
        try
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "where" : "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = startInfo };
            process.Start();
            process.WaitForExit(5000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Ejecuta un comando y retorna su output completo (stdout).
    /// Timeout de 10s para evitar hangs. Drena stdout/stderr en paralelo para evitar deadlock.
    /// </summary>
    static async Task<string> RunCommandAsync(string command, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        
        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to start command '{command} {arguments}': {ex.Message}", ex);
        }

        // Drain stdout and stderr in parallel to avoid deadlock
        // (if stderr buffer fills while we're reading stdout, process blocks)
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        // Wait for process exit with timeout
        try
        {
            await process.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* ignore */ }
            throw new InvalidOperationException($"Command '{command} {arguments}' timed out after 10 seconds");
        }

        // Wait for output streams to complete
        var output = await stdoutTask;
        var error = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Command '{command} {arguments}' failed with exit code {process.ExitCode}: {error}");
        }

        return output;
    }
}
