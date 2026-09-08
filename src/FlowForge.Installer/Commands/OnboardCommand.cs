using ConsoleAppFramework;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Models;
using Spectre.Console;

namespace FlowForge.Installer.Commands;

/// <summary>
/// Entry point for `flowforge onboard` (FR-001).
/// Orchestrates detection → retrieval → interactive menu → export.
/// </summary>
public sealed class OnboardCommand(InstallerContext ctx)
{
    readonly InstallerContext _ctx = ctx;

    [Command("")]
    public async Task<int> RunAsync(
        string? project = null,
        string? output = null,
        string? user = null)
    {
        try
        {
            // ── FR-007: Check engram binary exists ──────────────────────────
            if (!File.Exists(PathHelper.EngramBinary))
            {
                AnsiConsole.MarkupLine("[red]Error:[/] engram binary not found.");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("To install engram, run:");
                AnsiConsole.MarkupLine("  [bold]flowforge install[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[grey]This will install the engram CLI and configure MCP for your editors.[/]");
                return 1;
            }

            // ── FR-002: Detect project ──────────────────────────────────────
            var detectedProject = Onboarding.ProjectDetector.Detect(project);
            AnsiConsole.MarkupLine($"[bold blue]flowforge onboard[/] — Project: [bold]{Markup.Escape(detectedProject)}[/]");

            if (!Onboarding.ProjectDetector.IsFlowForgeProject(Directory.GetCurrentDirectory()))
            {
                AnsiConsole.MarkupLine("[yellow]Warning: Not a FlowForge project (no .flowforge.json found). Using detected project name.[/]");
            }

            // ── Resolve user ────────────────────────────────────────────────
            var config = _ctx.Store.Load();
            var resolvedUser = user
                ?? config.Sync?.User
                ?? Environment.GetEnvironmentVariable("ENGRAM_USER")
                ?? "unknown";

            // ── FR-003: Retrieve memories from engram ───────────────────────
            AnsiConsole.MarkupLine("[grey]Retrieving memories from engram...[/]");

            var subprocess = new Onboarding.EngramSubprocess();
            var rawOutput = await subprocess.RetrieveAllAsync(detectedProject, TimeSpan.FromSeconds(15));

            // ── Parse output ────────────────────────────────────────────────
            var sessions = Onboarding.EngramOutputParser.ParseContext(rawOutput.ContextOutput);
            var decisions = Onboarding.EngramOutputParser.ParseSearchResults(rawOutput.DecisionOutput, "decision");
            var patterns = Onboarding.EngramOutputParser.ParseSearchResults(rawOutput.PatternOutput, "pattern");

            // ── FR-008: Empty-memory handling ───────────────────────────────
            if (sessions.Count == 0 && decisions.Count == 0 && patterns.Count == 0)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[yellow]No memories found for this project yet.[/]");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("To start building project memory:");
                AnsiConsole.MarkupLine("  1. Start using the FlowForge methodology (install → init → work with AI agents)");
                AnsiConsole.MarkupLine("  2. Decisions and patterns are automatically captured by the memory agent");
                AnsiConsole.MarkupLine("  3. Re-run [bold]flowforge onboard[/] after a few sessions");
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[grey]Project: {Markup.Escape(detectedProject)}[/]");
                return 0;
            }

            // ── Build OnboardingData ────────────────────────────────────────
            var data = new Onboarding.OnboardingData(
                Project: detectedProject,
                User: resolvedUser,
                GeneratedAt: DateTime.Now,
                RecentSessions: sessions.Take(5).ToList(),
                Decisions: decisions.Take(10).ToList(),
                Patterns: patterns.Take(10).ToList()
            );

            // ── FR-004, FR-009: Launch interactive menu ─────────────────────
            var menu = new Onboarding.InteractiveMenu(data, output);
            return await menu.RunAsync();
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C — graceful exit
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
            return 1;
        }
    }
}
