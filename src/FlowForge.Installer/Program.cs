using ConsoleAppFramework;
using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using Spectre.Console;

// ── Detectar --verbose/-v ANTES de pasarlo a CAF para sincronizar Verbosity.IsVerbose
if (args.Contains("--verbose") || args.Contains("-v"))
{
    Verbosity.IsVerbose = true;
}

// ── CLI routing via ConsoleAppFramework ──────────────────────────────────────
var app = ConsoleApp.Create();

// Registrar --verbose como opción global de CAF (solo para help text - no para parsing)
app.ConfigureGlobalOptions((ref ConsoleApp.GlobalOptionsBuilder builder) =>
{
    // Registrar para help text, pero NO retornar valor (el parsing ya ocurrió arriba)
    builder.AddGlobalOption<bool>("-v|--verbose", "Enable verbose output");
    // Retornar una lambda vacía que no se ejecuta - el filtering ocurre antes
    return CreateVerbositySettings(false);
});

// Función helper para crear VerbositySettings
VerbositySettings CreateVerbositySettings(bool verbose) => new(verbose);

// ── Bootstrap services ───────────────────────────────────────────────────────
var log      = InstallerLogger.Default;
var store    = ConfigStore.Default;
var http     = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var gh       = new GitHubReleasesClient(http, log);
var manifest = new ManifestClient(http, log);
var ctx      = new InstallerContext(log, store, gh, manifest);

// flowforge  (sin args) → status
app.Add("", ([FromServices] InstallerContext c) =>
{
    StatusCommand.Run(c);
});

// flowforge install
app.Add<InstallCommand>("install");

// flowforge update [--check]
app.Add<UpdateCommand>("update");

// flowforge uninstall
app.Add<UninstallCommand>("uninstall");

// flowforge config set <key> <value>
app.Add<ConfigCommand>("config");

// Filtrar --verbose/-v de args ANTES de pasar a CAF para evitar NRE en command dispatch
var filteredArgs = args.Where(a => a != "--verbose" && a != "-v").ToArray();

try
{
    app.Run(filteredArgs);
}
catch (Exception ex)
{
    // Error final: mostrar mensaje formateado según Verbosity
    var msg = Verbosity.FormatError("Error fatal", ex);
    AnsiConsole.MarkupLine($"[red]{msg}[/]");
    Environment.Exit(1);
}

// ── Shared context passed to all commands ─────────────────────────────────────
namespace FlowForge.Installer.Commands
{
    public sealed record InstallerContext(
        InstallerLogger      Log,
        ConfigStore          Store,
        GitHubReleasesClient GitHub,
        ManifestClient       Manifest
    );
}

// ── Verbosity settings record for CAF global options ────────────────────────────────
internal record VerbositySettings(bool Verbose);