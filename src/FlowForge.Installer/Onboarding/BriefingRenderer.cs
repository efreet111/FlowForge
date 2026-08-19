using Spectre.Console;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// Renders BriefingData using Spectre.Console.
/// Escapes all memory text with Markup.Escape() to prevent injection (NFR-004).
/// </summary>
public sealed class BriefingRenderer
{
    readonly IEngramClient _client;
    readonly bool _interactive;

    public BriefingRenderer(IEngramClient client, bool interactive = true)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _interactive = interactive;
    }

    /// <summary>
    /// Renders the briefing to the console.
    /// </summary>
    /// <param name="data">Aggregated briefing data.</param>
    /// <param name="displayUser">Display-only user name for the header.</param>
    public async Task RenderAsync(BriefingData data, string? displayUser)
    {
        // Header
        AnsiConsole.WriteLine();
        var header = $"[bold blue]Onboarding Briefing[/] — [green]{Markup.Escape(data.Project)}[/]";
        if (!string.IsNullOrEmpty(displayUser))
            header += $"  [grey](for {Markup.Escape(displayUser)})[/]";
        if (!string.IsNullOrEmpty(data.Scope))
            header += $"  [grey]scope: {Markup.Escape(data.Scope)}[/]";
        AnsiConsole.MarkupLine(header);
        AnsiConsole.WriteLine();

        // Stats header
        if (data.Stats is not null)
        {
            AnsiConsole.MarkupLine($"[grey]Memory stats:[/] {data.Stats.TotalSessions} sessions, {data.Stats.TotalObservations} observations, {data.Stats.TotalPrompts} prompts");
            AnsiConsole.WriteLine();
        }

        // Collect all items for drill-down
        var allItems = new List<EngramSearchResult>();

        // Recent Activity
        if (!string.IsNullOrWhiteSpace(data.RecentActivity))
        {
            AnsiConsole.MarkupLine("[bold]Recent Activity[/]");
            AnsiConsole.MarkupLine("[grey]────────────────────────────────────────[/]");
            // Escape the markdown content to prevent Spectre markup injection
            AnsiConsole.WriteLine(Markup.Escape(data.RecentActivity));
            AnsiConsole.WriteLine();
        }

        // Key Decisions
        if (data.Decisions.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Key Architectural Decisions[/]");
            AnsiConsole.MarkupLine("[grey]────────────────────────────────────────[/]");
            RenderSearchResultsTable(data.Decisions, allItems);
            AnsiConsole.WriteLine();
        }

        // Conventions / Patterns
        if (data.Patterns.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Conventions & Patterns[/]");
            AnsiConsole.MarkupLine("[grey]────────────────────────────────────────[/]");
            RenderSearchResultsTable(data.Patterns, allItems);
            AnsiConsole.WriteLine();
        }

        // Blockers / Gotchas
        if (data.Blockers.Count > 0)
        {
            AnsiConsole.MarkupLine("[bold]Known Blockers / Gotchas[/]");
            AnsiConsole.MarkupLine("[grey]────────────────────────────────────────[/]");
            RenderSearchResultsTable(data.Blockers, allItems);
            AnsiConsole.WriteLine();
        }

        // Interactive drill-down
        if (_interactive && allItems.Count > 0)
        {
            await DrillDownLoopAsync(allItems).ConfigureAwait(false);
        }
    }

    void RenderSearchResultsTable(IReadOnlyList<EngramSearchResult> results, List<EngramSearchResult> allItems)
    {
        var table = new Table().Border(TableBorder.Simple);
        table.AddColumn("[grey]#[/]");
        table.AddColumn("[grey]ID[/]");
        table.AddColumn("[grey]Type[/]");
        table.AddColumn("[grey]Title[/]");
        table.AddColumn("[grey]Preview[/]");

        for (int i = 0; i < results.Count; i++)
        {
            var r = results[i];
            allItems.Add(r);
            var index = allItems.Count;
            var preview = Truncate(r.Preview, 80);
            table.AddRow(
                index.ToString(),
                $"#{r.Id}",
                Markup.Escape(r.Type),
                Markup.Escape(Truncate(r.Title, 40)),
                Markup.Escape(preview));
        }

        AnsiConsole.Write(table);
    }

    async Task DrillDownLoopAsync(List<EngramSearchResult> allItems)
    {
        while (true)
        {
            AnsiConsole.MarkupLine("[grey]Type a number to drill down, Enter to exit:[/]");
            var input = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(input))
                break;

            if (!int.TryParse(input, out var index) || index < 1 || index > allItems.Count)
            {
                AnsiConsole.MarkupLine("[red]Invalid selection.[/]");
                continue;
            }

            var item = allItems[index - 1];
            await ShowObservationAsync(item).ConfigureAwait(false);
        }
    }

    async Task ShowObservationAsync(EngramSearchResult item)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Observation #{item.Id}[/] — [grey]{Markup.Escape(item.Type)}[/]");
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(item.Title)}[/]");
        AnsiConsole.MarkupLine("[grey]────────────────────────────────────────[/]");

        try
        {
            var obs = await _client.GetObservationAsync(item.Id).ConfigureAwait(false);
            if (obs is not null)
            {
                AnsiConsole.WriteLine(Markup.Escape(obs.Content));
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"[grey]Project: {Markup.Escape(obs.Project)} | Scope: {Markup.Escape(obs.Scope ?? "—")} | Created: {Markup.Escape(obs.CreatedAt)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[yellow]Could not retrieve full content.[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {Markup.Escape(ex.Message)}");
        }

        AnsiConsole.WriteLine();
    }

    static string Truncate(string text, int maxLen)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLen) return text;
        return text[..maxLen] + "…";
    }
}
