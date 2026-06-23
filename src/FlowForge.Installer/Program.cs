using ConsoleAppFramework;
using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using Spectre.Console;

// ── Bootstrap services ───────────────────────────────────────────────────────
var log      = InstallerLogger.Default;
var store    = ConfigStore.Default;
var http     = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
var gh       = new GitHubReleasesClient(http, log);
var manifest = new ManifestClient(http, log);
var ctx      = new InstallerContext(log, store, gh, manifest);

// ── CLI routing via ConsoleAppFramework ──────────────────────────────────────
var app = ConsoleApp.Create();

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

app.Run(args);

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
