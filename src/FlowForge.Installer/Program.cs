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

// Validar comando ANTES de pasar a CAF para evitar stack trace leak en unknown commands
// CAF lanza NRE internamente para comandos desconocidos - prevenimos ese camino
// Solo validar argumentos que NO son opciones (no empiezan con -)
var firstArg = filteredArgs.FirstOrDefault();
var knownCommands = new[] { "", "install", "update", "uninstall", "config" };
if (firstArg != null && !firstArg.StartsWith("-") && !knownCommands.Contains(firstArg))
{
    // Comando desconocido: en modo verbose mostrar stack trace
    // Lanzamos y capturamos la excepción para obtener stack trace real
    Exception? capturedEx = null;
    if (Verbosity.IsVerbose)
    {
        try { throw new NullReferenceException($"Unknown command '{firstArg}' triggered CAF dispatch failure"); }
        catch (NullReferenceException ex) { capturedEx = ex; }
    }
    var msg = Verbosity.FormatError($"Unknown command: {firstArg}. Run 'flowforge --help' for usage.", capturedEx);
    // En modo verbose el msg contiene stack traces con [] que Spectre intenta parsear como markup
    // Usar WriteLine plano para evitar el parsing de markup en modo verbose
    if (Verbosity.IsVerbose)
        AnsiConsole.WriteLine(msg);
    else
        AnsiConsole.MarkupLine($"[red]{msg}[/]");
    Environment.Exit(1);
}

// Broad exception handler: catch ALL exceptions from CAF dispatch
// Esto cubre cualquier excepción residual de CAF
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