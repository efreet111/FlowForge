using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using FlowForge.Installer.Onboarding;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// flowforge onboard — First-day briefing command.
/// Detects project, pre-checks engram, pulls memories, renders briefing.
/// Exit codes: 0 success, 1 runtime error, 2 pre-check failure.
/// </summary>
public sealed class OnboardCommand(InstallerContext ctx)
{
    readonly InstallerContext _ctx = ctx;

    [Command("")]
    public async Task<int> RunAsync(
        string? project = null,         // --project <name>
        string? user = null,            // --user <handle> (display-only)
        string? scope = null,           // --scope team|personal (default: team)
        string? output = null,          // --output <path> (ONBOARDING.md)
        int limit = 10,                 // --limit <n> (clamped 1..20)
        bool noInteractive = false)     // --no-interactive (CI-safe)
    {
        try
        {
            AnsiConsole.MarkupLine("[bold]FlowForge Onboard[/] — first-day briefing");
            AnsiConsole.WriteLine();

            // ── Pre-flight checks (FR-002) ────────────────────────────────────
            var preCheckResult = await RunPreChecksAsync().ConfigureAwait(false);
            if (!preCheckResult)
                return 2;

            // ── Resolve project (FR-003, FR-015) ──────────────────────────────
            var workingDir = Directory.GetCurrentDirectory();
            var resolution = ProjectResolver.Resolve(project, scope, user, workingDir);

            if (resolution.IsAmbiguous)
            {
                if (noInteractive)
                {
                    AnsiConsole.MarkupLine("[red]Ambiguous project:[/] pass --project to disambiguate.");
                    if (resolution.AvailableProjects is not null)
                    {
                        AnsiConsole.MarkupLine("[grey]Available projects:[/]");
                        foreach (var p in resolution.AvailableProjects)
                            AnsiConsole.MarkupLine($"  [grey]- {Markup.Escape(p)}[/]");
                    }
                    return 1;
                }

                // Interactive: prompt for selection
                var prompt = new SelectionPrompt<string>()
                    .Title("Multiple projects detected. Select one:")
                    .AddChoices(resolution.AvailableProjects ?? []);
                var selected = AnsiConsole.Prompt(prompt);
                resolution = ProjectResolver.Resolve(selected, scope, user, workingDir);
            }

            AnsiConsole.MarkupLine($"[grey]Project:[/] {Markup.Escape(resolution.NamespacedProject)} [grey](source: {resolution.Source})[/]");

            // ── Resolve user identity (FR-014) ────────────────────────────────
            var config = _ctx.Store.Load();
            var engramUser = ResolveUserIdentity(config);
            var displayUser = user ?? engramUser;

            // ── Select client: HTTP-first, CLI fallback (FR-013) ──────────────
            var useHttp = ShouldUseHttp(config);
            IEngramClient client;

            if (useHttp)
            {
                var remoteUrl = config.Sync?.RemoteUrl ?? "";
                var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var httpClient = new HttpEngramClient(http, remoteUrl, engramUser, ownsHttpClient: true);

                // Health check
                var healthy = await httpClient.HealthCheckAsync().ConfigureAwait(false);
                if (healthy)
                {
                    client = httpClient;
                    AnsiConsole.MarkupLine("[grey]Using HTTP API (sync server reachable)[/]");
                }
                else
                {
                    AnsiConsole.MarkupLine("[yellow]Sync server unreachable, falling back to local CLI...[/]");
                    httpClient.Dispose();
                    client = CreateCliClient();
                }
            }
            else
            {
                AnsiConsole.MarkupLine("[grey]Using local CLI (sync.mode=local)[/]");
                client = CreateCliClient();
            }

            // ── Aggregate briefing (FR-004 to FR-008) ─────────────────────────
            var aggregator = new BriefingAggregator(client);
            var data = await aggregator.AggregateAsync(resolution.NamespacedProject, scope, limit).ConfigureAwait(false);

            // ── Empty-memory path (FR-012) ────────────────────────────────────
            if (!data.HasData)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[yellow]No memories found for {Markup.Escape(resolution.NamespacedProject)}.[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]First steps:[/]");
                AnsiConsole.MarkupLine("[grey]  1. Run `flowforge install` to set up the project.[/]");
                AnsiConsole.MarkupLine("[grey]  2. Capture decisions using the FlowForge methodology.[/]");
                AnsiConsole.MarkupLine("[grey]  3. Re-run `flowforge onboard` to see the briefing.[/]");
                if (string.IsNullOrEmpty(config.Sync?.RemoteUrl))
                {
                    AnsiConsole.MarkupLine("[grey]  4. Consider enabling team sync: `flowforge config set sync.remote_url <url>`[/]");
                }
                return 0;
            }

            // ── Render briefing (FR-009, FR-010) ──────────────────────────────
            var interactive = !noInteractive;
            var renderer = new BriefingRenderer(client, interactive);
            await renderer.RenderAsync(data, displayUser).ConfigureAwait(false);

            // ── Export (FR-011, NFR-005) ──────────────────────────────────────
            if (!string.IsNullOrEmpty(output))
            {
                if (string.Equals(scope, "personal", StringComparison.OrdinalIgnoreCase))
                {
                    AnsiConsole.MarkupLine("[yellow]Warning: --scope personal combined with --output. Export skipped (team scope only).[/]");
                }
                else
                {
                    var exporter = new MarkdownExporter();
                    var exported = await exporter.ExportAsync(data, output, displayUser).ConfigureAwait(false);
                    if (exported)
                        AnsiConsole.MarkupLine($"[green]Exported to {Markup.Escape(output)}[/]");
                    else
                        AnsiConsole.MarkupLine("[yellow]Export skipped (personal scope).[/]");
                }
            }

            // Cleanup
            if (client is IDisposable disposable)
                disposable.Dispose();

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }

    // ── Pre-flight checks (FR-002) ────────────────────────────────────────────

    async Task<bool> RunPreChecksAsync()
    {
        var checks = new List<(string Name, Func<Task<(bool, string?)>> Check)>
        {
            ("engram binary", () => CheckFileAsync(PathHelper.EngramBinary, "Instalá con `flowforge install`.")),
            ("engram config", () => CheckConfigAsync()),
        };

        var allPassed = true;
        foreach (var (name, check) in checks)
        {
            var (passed, hint) = await check().ConfigureAwait(false);
            var status = passed ? "[green]✓ OK[/]" : "[red]✗ FAIL[/]";
            var detail = hint ?? string.Empty;
            AnsiConsole.MarkupLine($"  {name} — {status} {detail}");
            if (!passed) allPassed = false;
        }

        if (!allPassed)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[red]Pre-checks failed. Fix the issues above and retry.[/]");
        }

        return allPassed;
    }

    static Task<(bool, string?)> CheckFileAsync(string path, string hint)
    {
        var exists = File.Exists(path);
        return Task.FromResult((exists, exists ? null : hint));
    }

    Task<(bool, string?)> CheckConfigAsync()
    {
        try
        {
            var config = _ctx.Store.Load();
            // Config is readable if we got here (Load returns defaults on failure)
            // Check if sync.user or ENGRAM_USER is resolvable
            var user = config.Sync?.User;
            if (string.IsNullOrEmpty(user))
                user = Environment.GetEnvironmentVariable("ENGRAM_USER");
            if (string.IsNullOrEmpty(user))
                user = Environment.UserName;

            return Task.FromResult((true, (string?)null));
        }
        catch
        {
            return Task.FromResult((false, (string?)"Config unreadable. Run `flowforge install` or `flowforge config`."));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string ResolveUserIdentity(InstallerConfig config)
    {
        // Priority: sync.user → ENGRAM_USER → Environment.UserName
        var user = config.Sync?.User;
        if (!string.IsNullOrEmpty(user)) return user;

        user = Environment.GetEnvironmentVariable("ENGRAM_USER");
        if (!string.IsNullOrEmpty(user)) return user;

        return Environment.UserName;
    }

    static bool ShouldUseHttp(InstallerConfig config)
    {
        var mode = config.Sync?.Mode;
        if (string.IsNullOrEmpty(mode)) return false;
        return string.Equals(mode, "sync", StringComparison.OrdinalIgnoreCase);
    }

    static CliEngramClient CreateCliClient()
    {
        return new CliEngramClient(PathHelper.EngramBinary);
    }
}
