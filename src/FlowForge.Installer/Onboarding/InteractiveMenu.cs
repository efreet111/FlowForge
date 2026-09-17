using Spectre.Console;

namespace FlowForge.Installer.Onboarding;

/// <summary>
/// State machine for the interactive onboarding menu (FR-004, FR-009).
/// Uses Spectre.Console SelectionPrompt&lt;T&gt; for drill-down navigation.
/// States: INIT → SESSION_LIST → SESSION_DETAIL → DECISION_LIST → DECISION_DETAIL → PATTERN_LIST → PATTERN_DETAIL → EXPORT → EXIT
/// </summary>
public sealed class InteractiveMenu
{
    private enum MenuState
    {
        Init,
        SessionList,
        SessionDetail,
        DecisionList,
        DecisionDetail,
        PatternList,
        PatternDetail,
        Export,
        Exit
    }

    private readonly OnboardingData _data;
    private readonly string? _outputPath;

    public InteractiveMenu(OnboardingData data, string? outputPath = null)
    {
        _data = data;
        _outputPath = outputPath;
    }

    /// <summary>
    /// Run the interactive menu loop. Returns exit code (0 = success).
    /// Handles Ctrl+C gracefully (exit 0, no stack trace).
    /// </summary>
    public async Task<int> RunAsync()
    {
        // Handle Ctrl+C gracefully
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var state = MenuState.Init;

        try
        {
            while (state != MenuState.Exit && !cts.Token.IsCancellationRequested)
            {
                state = state switch
                {
                    MenuState.Init => ShowMainMenu(),
                    MenuState.SessionList => ShowSessionList(),
                    MenuState.SessionDetail => await ShowSessionDetailAsync(),
                    MenuState.DecisionList => ShowDecisionList(),
                    MenuState.DecisionDetail => await ShowDecisionDetailAsync(),
                    MenuState.PatternList => ShowPatternList(),
                    MenuState.PatternDetail => await ShowPatternDetailAsync(),
                    MenuState.Export => await DoExportAsync(),
                    _ => MenuState.Exit
                };
            }
        }
        catch (OperationCanceledException)
        {
            // Ctrl+C — graceful exit
        }

        return 0;
    }

    // ── State: Main Menu ─────────────────────────────────────────────────────

    private MenuState ShowMainMenu()
    {
        AnsiConsole.Clear();
        RenderHeader();

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("What would you like to explore?")
                .AddChoices(new[]
                {
                    "Recent Activity",
                    "Key Decisions",
                    "Patterns",
                    "Export to Markdown",
                    "Exit"
                }));

        return choice switch
        {
            "Recent Activity" => MenuState.SessionList,
            "Key Decisions" => MenuState.DecisionList,
            "Patterns" => MenuState.PatternList,
            "Export to Markdown" => MenuState.Export,
            "Exit" => MenuState.Exit,
            _ => MenuState.Init
        };
    }

    // ── State: Session List ──────────────────────────────────────────────────

    private MenuState ShowSessionList()
    {
        if (_data.RecentSessions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No recent sessions found.[/]");
            AnsiConsole.MarkupLine("[grey]Press Enter to return to menu...[/]");
            Console.ReadLine();
            return MenuState.Init;
        }

        // Use plain WriteLine to avoid all markup conflicts
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine($"  Recent Activity");
        for (int i = 0; i < _data.RecentSessions.Count; i++)
        {
            var s = _data.RecentSessions[i];
            var displayTitle = StripMarkdownBold(s.Title);
            AnsiConsole.WriteLine($"  [{i + 1}] {displayTitle} ({s.Timestamp:yyyy-MM-dd})");
        }
        AnsiConsole.WriteLine($"  [0] Back");
        AnsiConsole.WriteLine();

        AnsiConsole.Write("  Select option: ");
        var input = Console.ReadLine();

        int selectedIndex;
        if (!int.TryParse(input, out selectedIndex) || selectedIndex < 0 || selectedIndex > _data.RecentSessions.Count)
        {
            return MenuState.Init;
        }

        if (selectedIndex == 0)
            return MenuState.Init;

        _selectedSession = _data.RecentSessions[selectedIndex - 1];
        return MenuState.SessionDetail;
    }

    // ── State: Session Detail ────────────────────────────────────────────────

    private Session? _selectedSession;

    private Task<MenuState> ShowSessionDetailAsync()
    {
        if (_selectedSession == null)
            return Task.FromResult(MenuState.Init);

        AnsiConsole.Clear();
        RenderHeader();

        var sessionTitle = StripMarkdownBold(_selectedSession.Title);
        AnsiConsole.MarkupLine($"[bold]{Markup.Escape(sessionTitle)}[/]");
        AnsiConsole.MarkupLine($"[grey]{_selectedSession.Timestamp:yyyy-MM-dd}[/]");
        AnsiConsole.WriteLine();

        if (!string.IsNullOrWhiteSpace(_selectedSession.Content))
        {
            var sessionContent = StripMarkdownBold(_selectedSession.Content);
            var panel = new Panel(Markup.Escape(sessionContent))
            {
                Border = BoxBorder.Rounded,
                Header = new PanelHeader("Session Details")
            };
            AnsiConsole.Write(panel);
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]No content available.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press Enter to return to menu...[/]");
        Console.ReadLine();

        _selectedSession = null;
        return Task.FromResult(MenuState.Init);
    }

    // ── State: Decision List ─────────────────────────────────────────────────

    private MenuState ShowDecisionList()
    {
        if (_data.Decisions.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No decisions recorded.[/]");
            AnsiConsole.MarkupLine("[grey]Press Enter to return to menu...[/]");
            Console.ReadLine();
            return MenuState.Init;
        }

        // Show notice if fewer than 3 decisions (AC-4 threshold)
        if (_data.Decisions.Count < 3)
        {
            AnsiConsole.MarkupLine($"[yellow]Only {_data.Decisions.Count} decision(s) recorded.[/]");
            AnsiConsole.WriteLine();
        }

        // Use plain WriteLine to avoid all markup conflicts with Spectre
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine($"  Key Decisions");
        for (int i = 0; i < _data.Decisions.Count; i++)
        {
            var d = _data.Decisions[i];
            var displayTitle = StripMarkdownBold(d.Title);
            AnsiConsole.WriteLine($"  [{i + 1}] {displayTitle} ({d.Timestamp:yyyy-MM-dd})");
        }
        AnsiConsole.WriteLine($"  [0] Back");
        AnsiConsole.WriteLine();

        AnsiConsole.Write("  Select option: ");
        var input = Console.ReadLine();

        int selectedIndex;
        if (!int.TryParse(input, out selectedIndex) || selectedIndex < 0 || selectedIndex > _data.Decisions.Count)
        {
            return MenuState.Init;
        }

        if (selectedIndex == 0)
            return MenuState.Init;

        _selectedObservation = _data.Decisions[selectedIndex - 1];
        return MenuState.DecisionDetail;
    }

    // ── State: Decision Detail ───────────────────────────────────────────────

    private Observation? _selectedObservation;

    private Task<MenuState> ShowDecisionDetailAsync()
    {
        if (_selectedObservation == null)
            return Task.FromResult(MenuState.Init);

        RenderObservationDetail(_selectedObservation, "Decision");

        _selectedObservation = null;
        return Task.FromResult(MenuState.Init);
    }

    // ── State: Pattern List ──────────────────────────────────────────────────

    private MenuState ShowPatternList()
    {
        if (_data.Patterns.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No patterns recorded.[/]");
            AnsiConsole.MarkupLine("[grey]Press Enter to return to menu...[/]");
            Console.ReadLine();
            return MenuState.Init;
        }

        // Use plain WriteLine to avoid all markup conflicts
        AnsiConsole.WriteLine();
        AnsiConsole.WriteLine($"  Reusable Patterns");
        for (int i = 0; i < _data.Patterns.Count; i++)
        {
            var p = _data.Patterns[i];
            var displayTitle = StripMarkdownBold(p.Title);
            AnsiConsole.WriteLine($"  [{i + 1}] {displayTitle} ({p.Timestamp:yyyy-MM-dd})");
        }
        AnsiConsole.WriteLine($"  [0] Back");
        AnsiConsole.WriteLine();

        AnsiConsole.Write("  Select option: ");
        var input = Console.ReadLine();

        int selectedIndex;
        if (!int.TryParse(input, out selectedIndex) || selectedIndex < 0 || selectedIndex > _data.Patterns.Count)
        {
            return MenuState.Init;
        }

        if (selectedIndex == 0)
            return MenuState.Init;

        _selectedObservation = _data.Patterns[selectedIndex - 1];
        return MenuState.PatternDetail;
    }

    // ── State: Pattern Detail ────────────────────────────────────────────────

    private Task<MenuState> ShowPatternDetailAsync()
    {
        if (_selectedObservation == null)
            return Task.FromResult(MenuState.Init);

        RenderObservationDetail(_selectedObservation, "Pattern");

        _selectedObservation = null;
        return Task.FromResult(MenuState.Init);
    }

    // ── State: Export ────────────────────────────────────────────────────────

    private Task<MenuState> DoExportAsync()
    {
        if (string.IsNullOrEmpty(_outputPath))
        {
            AnsiConsole.MarkupLine("[yellow]⚠[/] No output path specified");
            AnsiConsole.MarkupLine("[grey]Press Enter to return to menu...[/]");
            Console.ReadLine();
            return Task.FromResult(MenuState.Init);
        }

        try
        {
            // Build markdown content from OnboardingData
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"# Onboarding Briefing: {_data.Project}");
            sb.AppendLine();
            sb.AppendLine($"**User:** {_data.User}");
            sb.AppendLine($"**Generated:** {_data.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Recent Sessions
            if (_data.RecentSessions.Count > 0)
            {
                sb.AppendLine("## Recent Sessions");
                sb.AppendLine();
                foreach (var session in _data.RecentSessions)
                {
                    sb.AppendLine($"### {session.Title}");
                    sb.AppendLine($"*{session.Timestamp:yyyy-MM-dd HH:mm}*");
                    sb.AppendLine();
                    sb.AppendLine(session.Content);
                    sb.AppendLine();
                }
            }

            // Decisions
            if (_data.Decisions.Count > 0)
            {
                sb.AppendLine("## Key Decisions");
                sb.AppendLine();
                foreach (var decision in _data.Decisions)
                {
                    sb.AppendLine($"### {decision.Title}");
                    sb.AppendLine($"*{decision.Timestamp:yyyy-MM-dd HH:mm}*");
                    sb.AppendLine();
                    sb.AppendLine(decision.Content);
                    sb.AppendLine();
                }
            }

            // Patterns
            if (_data.Patterns.Count > 0)
            {
                sb.AppendLine("## Patterns");
                sb.AppendLine();
                foreach (var pattern in _data.Patterns)
                {
                    sb.AppendLine($"### {pattern.Title}");
                    sb.AppendLine($"*{pattern.Timestamp:yyyy-MM-dd HH:mm}*");
                    sb.AppendLine();
                    sb.AppendLine(pattern.Content);
                    sb.AppendLine();
                }
            }

            // Write to file
            var dir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(_outputPath, sb.ToString());
            AnsiConsole.MarkupLine($"[green]✓[/] ONBOARDING.md exported to: [bold]{Markup.Escape(_outputPath)}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗[/] Export failed: {Markup.Escape(ex.Message)}");
        }

        AnsiConsole.MarkupLine("[grey]Press Enter to return to menu...[/]");
        Console.ReadLine();
        return Task.FromResult(MenuState.Init);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private void RenderHeader()
    {
        AnsiConsole.MarkupLine($"[bold blue]Onboarding: {Markup.Escape(_data.Project)}[/]");
        AnsiConsole.MarkupLine($"[grey]User: {Markup.Escape(_data.User)} | Generated: {_data.GeneratedAt:yyyy-MM-dd HH:mm}[/]");
        AnsiConsole.WriteLine();
    }

    private static void RenderObservationDetail(Observation obs, string label)
    {
        AnsiConsole.Clear();

        // Strip markdown bold and escape for Spectre.Console (no bold markup to avoid conflicts)
        var displayTitle = StripMarkdownBold(obs.Title);
        AnsiConsole.MarkupLine(Markup.Escape(displayTitle));
        AnsiConsole.MarkupLine($"[grey]{label} | {obs.Timestamp:yyyy-MM-dd}[/]");
        AnsiConsole.WriteLine();

        if (!string.IsNullOrWhiteSpace(obs.Content))
        {
            var displayContent = StripMarkdownBold(obs.Content);
            var panel = new Panel(Markup.Escape(displayContent))
            {
                Border = BoxBorder.Rounded,
                Header = new PanelHeader($"{label} Details")
            };
            AnsiConsole.Write(panel);
        }
        else
        {
            AnsiConsole.MarkupLine("[grey]No content available.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Press Enter to return to menu...[/]");
        Console.ReadLine();
    }

    /// <summary>
    /// Strips markdown bold syntax (**text**) to prevent Spectre.Console markup conflicts.
    /// Robust: removes ALL ** markers, including orphaned ones.
    /// </summary>
    private static string StripMarkdownBold(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        // First pass: replace **...** with just the content
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\*\*(.+?)\*\*", "$1");

        // Second pass: remove any remaining ** that didn't form a proper pair
        text = text.Replace("**", "");

        return text;
    }
}
