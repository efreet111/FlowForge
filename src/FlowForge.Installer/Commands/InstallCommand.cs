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

    [Command("")]
    public async Task RunAsync(
        bool yes = false,
        bool noEngram = false,
        bool noFlowforge = false,
        bool forceFree = false,
        bool dryRun = false,
        bool jsonOnly = false,
        bool allowSymlink = false,
        bool noSudo = false
    )
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

        bool installEngram;
        bool installFlowForge;
        string engramMode;
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
            await module.InstallAsync(engramMode);
        }

        if (installFlowForge)
        {
            var module = new FlowForgeModule(ctx);
            module.Install(selectedIdes, forceFree, dryRun, jsonOnly, allowSymlink, noSudo);
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

        if (sudoUser != null && Environment.GetEnvironmentVariable("USER") == "root")
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] Instalado como root. Si el MCP falla, ejecutá:");
            AnsiConsole.MarkupLine($"[grey]  sudo chown -R {sudoUser}:{sudoUser} ~/.engram ~/.local/bin/engram ~/.local/bin/libe_sqlite3.so ~/.local/bin/flowforge[/]");
        }

        ctx.Log.Info("install: completado");
    }

    /// <summary>
    /// Detecta el modo sync adecuado: si ENGRAM_SERVER_URL está configurado → sync, sino local.
    /// </summary>
    static string DetectSyncMode()
    {
        var serverUrl = Environment.GetEnvironmentVariable("ENGRAM_SERVER_URL");
        return string.IsNullOrWhiteSpace(serverUrl) ? "local" : "sync";
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
}
