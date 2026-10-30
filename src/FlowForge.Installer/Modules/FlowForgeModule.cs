using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules.OpenCode;
using FlowForge.Installer.Update;
using Spectre.Console;

namespace FlowForge.Installer.Modules;

/// <summary>
/// Instala los skills/agents/commands de FlowForge en los IDEs seleccionados.
/// </summary>
public sealed class FlowForgeModule(InstallerContext ctx)
{
    // Single source of truth for the installer version (see ADR-017).
    public const string InstallerVersion = "0.1.0-alpha.17";

    public void Install(
        List<string> selectedIdes,
        string provider,
        bool forceFree = false,
        bool dryRun = false,
        bool jsonOnly = false,
        bool allowSymlink = false,
        bool noSudo = false)
    {
        AnsiConsole.MarkupLine("[bold]Instalando FlowForge skills...[/]");
        ctx.Log.Info($"FlowForgeModule.Install: ides={string.Join(",", selectedIdes)}");

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var locator = new FlowForgeRepoLocator(ctx.Log);

        if (!locator.EnsureAvailable(out var ffRepo) || ffRepo == null)
        {
            AnsiConsole.MarkupLine("[red]✗[/] No se pudo obtener el repo FlowForge.");
            AnsiConsole.MarkupLine("[yellow]![/] Necesitás [bold]git[/] en PATH.");
            if (OperatingSystem.IsWindows())
                AnsiConsole.MarkupLine("[grey]  winget install Git.Git / scoop install git[/]");
            else if (OperatingSystem.IsLinux())
                AnsiConsole.MarkupLine("[grey]  sudo apt install git / sudo pacman -S git[/]");
            else if (OperatingSystem.IsMacOS())
                AnsiConsole.MarkupLine("[grey]  brew install git[/]");
            AnsiConsole.MarkupLine("[grey]  Luego reinstalá: flowforge install --yes[/]");
            return;
        }

        WarnIfExistingAgents(selectedIdes, home);
        InstallSharedParity(home, ffRepo);

        foreach (var ide in selectedIdes)
            InstallForIde(ide, home, ffRepo, provider, forceFree, dryRun, jsonOnly, allowSymlink, noSudo);
    }

    void InstallSharedParity(string home, string ffRepo)
    {
        var sharedSrc = Path.Combine(ffRepo, "ide", "shared");
        if (!Directory.Exists(sharedSrc)) return;

        var sharedDest = Path.Combine(home, ".flowforge", "shared");
        Directory.CreateDirectory(sharedDest);
        CopyGlob(sharedSrc, sharedDest, "*");
        AnsiConsole.MarkupLine($"  [green]✓[/] Paridad global → [grey]{sharedDest}[/]");
    }

    void WarnIfExistingAgents(List<string> selectedIdes, string home)
    {
        var filesByIde = new Dictionary<string, List<string>>();

        foreach (var ide in selectedIdes)
        {
            var ideLower = ide.ToLowerInvariant();
            List<string> existingFiles = [];

            switch (ideLower)
            {
                case "cursor":
                    var cursorDir = Path.Combine(home, ".cursor", "agents");
                    if (Directory.Exists(cursorDir))
                        existingFiles = [.. Directory.EnumerateFiles(cursorDir, "forge-*.md", SearchOption.TopDirectoryOnly)];
                    break;
                case "opencode":
                    var opencodeAgentsDir = Path.Combine(home, ".config", "opencode", "agents");
                    if (Directory.Exists(opencodeAgentsDir))
                        existingFiles = [.. Directory.EnumerateFiles(opencodeAgentsDir, "forge-*.md", SearchOption.TopDirectoryOnly)];
                    break;
                case "vs code":
                    var copilotAgentsDir = Path.Combine(home, ".copilot", "agents");
                    if (Directory.Exists(copilotAgentsDir))
                        existingFiles = [.. Directory.EnumerateFiles(copilotAgentsDir, "forge-*.agent.md", SearchOption.TopDirectoryOnly)];
                    break;
                case "antigravity":
                    var antigravityRulesDir = PathHelper.AntigravityRules;
                    if (Directory.Exists(antigravityRulesDir))
                        existingFiles = [.. Directory.EnumerateFiles(antigravityRulesDir, "*.md", SearchOption.TopDirectoryOnly)];
                    break;
            }

            if (existingFiles.Count > 0)
                filesByIde[ide] = existingFiles;
        }

        if (filesByIde.Count == 0) return;

        var totalFiles = filesByIde.Values.Sum(f => f.Count);
        AnsiConsole.MarkupLine($"[yellow]⚠️  Detecté {totalFiles} archivos forge-* existentes — serán sobrescritos.[/]");
    }

    void InstallForIde(
        string ide,
        string home,
        string ffRepo,
        string provider,
        bool forceFree,
        bool dryRun,
        bool jsonOnly,
        bool allowSymlink,
        bool noSudo)
    {
        switch (ide.ToLowerInvariant())
        {
            case "cursor":
                InstallCursor(home, ffRepo);
                break;
            case "opencode":
                InstallOpenCode(ffRepo, home, provider, forceFree, dryRun, jsonOnly, allowSymlink, noSudo);
                break;
            case "vs code":
                InstallVsCode(ffRepo);
                break;
            case "antigravity":
                InstallAntigravity(ffRepo);
                break;
            case "claude desktop":
                AnsiConsole.MarkupLine("  [grey]Claude Desktop: configura manualmente el MCP — ver docs/MCP-CONFIG.md[/]");
                break;
        }
    }

    void InstallCursor(string home, string ffRepo)
    {
        var dest = Path.Combine(home, ".cursor");
        Directory.CreateDirectory(dest);
        CopySkillsToCursor(ffRepo, dest);
        AnsiConsole.MarkupLine("  [green]✓[/] Cursor → [grey]~/.cursor/rules + agents + commands[/]");
    }

    static void CopySkillsToCursor(string ffRepo, string cursorDir)
    {
        var ideDir = Path.Combine(ffRepo, "ide", "cursor");
        if (!Directory.Exists(ideDir)) return;

        CopyGlob(Path.Combine(ideDir, "rules"), Path.Combine(cursorDir, "rules"), "*.mdc");
        CopyGlob(Path.Combine(ideDir, "agents"), Path.Combine(cursorDir, "agents"), "forge-*.md");
        CopyGlob(Path.Combine(ideDir, "commands"), Path.Combine(cursorDir, "commands"), "*.md");
    }

    void InstallOpenCode(
        string ffRepo,
        string home,
        string provider,
        bool forceFree = false,
        bool dryRun = false,
        bool jsonOnly = false,
        bool allowSymlink = false,
        bool noSudo = false)
    {
        var opencodeDir = Path.Combine(home, ".config", "opencode");
        var agentsDest = Path.Combine(opencodeDir, "agents");
        var commandsDest = Path.Combine(opencodeDir, "commands");
        var rulesDest = Path.Combine(opencodeDir, ".agents", "rules");
        EnsureDirectoryWithBackup(agentsDest);
        EnsureDirectoryWithBackup(commandsDest);
        EnsureDirectoryWithBackup(rulesDest);

        var templatesDir = Path.Combine(ffRepo, "ide", "opencode", "templates");
        var configDir = Path.Combine(ffRepo, "ide", "opencode", "config");
        var agentModelsPath = Path.Combine(configDir, "agent-models.json");
        var managedPathsPath = Path.Combine(templatesDir, "managed-paths.json");
        var configJsonc = Path.Combine(opencodeDir, "opencode.jsonc");
        var configJson = Path.Combine(opencodeDir, "opencode.json");
        var configPath = File.Exists(configJsonc) ? configJsonc : configJson;
        var sidecarPath = PathHelper.OpenCodeSidecarPath;

        Directory.CreateDirectory(PathHelper.FlowForgeBackupDir);
        var backupDir = Path.Combine(PathHelper.FlowForgeBackupDir, $"opencode-{DateTime.UtcNow:yyyyMMdd-HHmmss}");
        Directory.CreateDirectory(backupDir);
        if (File.Exists(configPath))
        {
            File.Copy(configPath, Path.Combine(backupDir, Path.GetFileName(configPath)), overwrite: true);
        }

        var piiScanner = new PiiScanner();
        if (Directory.Exists(templatesDir))
        {
            foreach (var templateFile in Directory.GetFiles(templatesDir, "*", SearchOption.AllDirectories))
                piiScanner.EnsureClean(File.ReadAllText(templateFile), templateFile);
        }

        var preHash = File.Exists(configPath) ? ComputeSha256(configPath) : null;

        var configGen = new OpenCodeConfigGenerator(ffRepo, provider, forceFree, dryRun, allowSymlink);
        var result = configGen.GenerateOrMerge(
            configPath,
            templatesDir,
            agentModelsPath,
            managedPathsPath,
            sidecarPath);

        var modifiedFiles = new List<string>();

        if (!dryRun)
        {
            var rulesPath = Path.Combine(rulesDest, "model-assignments.md");
            var modelGen = new ModelAssignmentsGenerator(agentModelsPath, rulesPath, provider);
            modelGen.Generate(configPath);
            modifiedFiles.Add(configPath);
            modifiedFiles.Add(rulesPath);

            if (!jsonOnly)
            {
                var manifest = JsonSerializer.Deserialize(
                    File.ReadAllText(agentModelsPath),
                    OpenCodeJsonContext.Default.AgentModelsManifest)
                    ?? throw new InvalidOperationException("agent-models.json inválido.");

                var patcher = new AgentFrontmatterPatcher();
                var templateAgents = Path.Combine(ffRepo, "ide", "opencode", "agents");
                if (Directory.Exists(templateAgents))
                {
                    foreach (var templateAgent in Directory.GetFiles(templateAgents, "*.md"))
                    {
                        var agentName = Path.GetFileNameWithoutExtension(templateAgent);
                        var dest = Path.Combine(agentsDest, $"{agentName}.md");
                        File.Copy(templateAgent, dest, overwrite: true);
                        if (manifest.Agents.ContainsKey(agentName))
                        {
                            var resolved = manifest.ResolveAgentModel(agentName, provider);
                            patcher.Patch(dest, resolved);
                        }
                        modifiedFiles.Add(dest);
                    }
                }

                var commandsSrc = Path.Combine(ffRepo, "ide", "opencode", "commands");
                CopyGlob(commandsSrc, commandsDest, "*.md");
                modifiedFiles.Add(commandsDest);
            }

            // ── AGENTS.md non-destructive merge (HU-025) ─────────────────────
            var agentsMdPath = PathHelper.OpenCodeAgentsMd;
            var merger = new AgentsMdMerger(
                ctx.Log,
                new BackupManager(ctx.Log),
                new AtomicWriter());
            var mergeResult = merger.Merge(agentsMdPath, opencodeDir, autoConfirm: !dryRun);

            switch (mergeResult.Action)
            {
                case MergeAction.Created:
                    AnsiConsole.MarkupLine($"  [green]✓[/] AGENTS.md → [grey]created with FlowForge pre-flight block[/]");
                    modifiedFiles.Add(agentsMdPath);
                    break;
                case MergeAction.Merged:
                    AnsiConsole.MarkupLine($"  [green]✓[/] AGENTS.md → [grey]merged (backup: {Path.GetFileName(mergeResult.BackupPath)})[/]");
                    modifiedFiles.Add(agentsMdPath);
                    break;
                case MergeAction.IdempotentNoOp:
                    AnsiConsole.MarkupLine($"  [grey]⋯[/] AGENTS.md → already up to date");
                    break;
                case MergeAction.ConflictReported:
                    AnsiConsole.MarkupLine($"  [yellow]⚠[/] AGENTS.md → [yellow]conflicts detected, not modified:[/]");
                    foreach (var conflict in mergeResult.ConflictDescriptions)
                        AnsiConsole.MarkupLine($"    [yellow]• {Markup.Escape(conflict)}[/]");
                    break;
                case MergeAction.Error:
                    AnsiConsole.MarkupLine($"  [yellow]![/] AGENTS.md → [yellow]{Markup.Escape(mergeResult.Error ?? "unknown error")}[/]");
                    break;
            }
        }

        var postHash = (!dryRun && File.Exists(configPath)) ? ComputeSha256(configPath) : null;
        var usedSudo = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SUDO_USER"));

        if (!dryRun)
        {
            var logger = new InstallLogger(InstallerVersion);
            logger.Append(modifiedFiles.ToArray(), preHash, postHash, usedSudo);
        }

        foreach (var warning in result.Warnings)
            AnsiConsole.MarkupLine($"[yellow]![/] {warning}");

        if (result.PaidProviderDetected)
            AnsiConsole.MarkupLine("[yellow]![/] Detecté provider 'opencode-go' y modelos manuales; no apliqué free-Zen. Usá --force-free para forzar downgrade.");

        AnsiConsole.MarkupLine("⚠ Free Zen models may use your prompts/data for training. Do NOT send proprietary or sensitive code in default config. See docs/PII-POLICY.md.");

        var oldFfDir = Path.Combine(opencodeDir, "flowforge");
        if (Directory.Exists(oldFfDir))
        {
            try { Directory.Delete(oldFfDir, recursive: true); } catch { /* best-effort */ }
        }
        var oldFfJson = Path.Combine(opencodeDir, "opencode.flowforge.json");
        if (File.Exists(oldFfJson))
        {
            try { File.Delete(oldFfJson); } catch { /* best-effort */ }
        }

        AnsiConsole.MarkupLine($"  [green]✓[/] OpenCode → [grey]~/.config/opencode/ (config + agents + commands)[/]");
    }

    static void InstallAntigravity(string ffRepo)
    {
        var ideDir = Path.Combine(ffRepo, "ide", "antigravity");
        if (!Directory.Exists(ideDir)) return;

        var configDir = PathHelper.AntigravityConfigDir;
        EnsureDirectoryWithBackup(configDir);
        EnsureDirectoryWithBackup(PathHelper.AntigravityRules);
        EnsureDirectoryWithBackup(PathHelper.AntigravitySkills);
        EnsureDirectoryWithBackup(PathHelper.AntigravityAgents);
        MigrateLegacyWorkflowsDir();

        var workspaceAgents = PathHelper.AntigravityWorkspaceAgents;
        EnsureDirectoryWithBackup(Path.Combine(workspaceAgents, "rules"));
        EnsureDirectoryWithBackup(Path.Combine(workspaceAgents, "skills"));
        EnsureDirectoryWithBackup(Path.Combine(workspaceAgents, "agents"));

        var agentsMd = Path.Combine(ideDir, "AGENTS.md");
        if (File.Exists(agentsMd))
        {
            File.Copy(agentsMd, Path.Combine(configDir, "AGENTS.md"), overwrite: true);
            File.Copy(agentsMd, Path.Combine(workspaceAgents, "AGENTS.md"), overwrite: true);
        }

        CopyGlob(Path.Combine(ideDir, "rules"), PathHelper.AntigravityRules, "*.md");
        CopyGlob(Path.Combine(ideDir, "rules"), Path.Combine(workspaceAgents, "rules"), "*.md");
        InstallAntigravityAgents(PathHelper.AntigravityAgents, ideDir);
        InstallAntigravityAgents(Path.Combine(workspaceAgents, "agents"), ideDir);

        InstallAntigravitySkills(PathHelper.AntigravitySkills, ffRepo);
        InstallAntigravitySkills(Path.Combine(workspaceAgents, "skills"), ffRepo);
        InstallAntigravityCommandSkills(PathHelper.AntigravitySkills, Path.Combine(ideDir, "workflows"));
        InstallAntigravityCommandSkills(Path.Combine(workspaceAgents, "skills"), Path.Combine(ideDir, "workflows"));

        var workflowRule = Path.Combine(ideDir, "rules", "workflow.md");
        if (File.Exists(workflowRule))
        {
            var geminiDir = Path.Combine(PathHelper.HomeDir, ".gemini");
            Directory.CreateDirectory(geminiDir);
            // GEMINI.md must NOT have frontmatter — the whole file is always_on.
            File.WriteAllText(
                Path.Combine(geminiDir, "GEMINI.md"),
                StripFrontmatter(File.ReadAllText(workflowRule)));
        }

        CleanupLegacyAntigravityPack();
        AnsiConsole.MarkupLine("  [green]✓[/] Antigravity → [grey]~/.gemini/config/ (AGENTS + rules + agents + skills)[/]");
    }

    /// <summary>
    /// ADR-019 Amendment: legacy workflow dirs are dead (2026-10-19). Migrate
    /// flow-*.md into config/skills/&lt;name&gt;/SKILL.md and delete the sources.
    /// </summary>
    static void MigrateLegacyWorkflowsDir()
    {
        foreach (var legacyDir in new[] { PathHelper.AntigravityLegacyWorkflowsDir, PathHelper.AntigravityWorkflows })
        {
            if (!Directory.Exists(legacyDir))
                continue;

            foreach (var file in Directory.GetFiles(legacyDir, "flow-*.md"))
            {
                try
                {
                    WriteCommandSkill(file, PathHelper.AntigravitySkills);
                    File.Delete(file);
                }
                catch
                {
                    /* best-effort */
                }
            }

            try
            {
                if (!Directory.EnumerateFileSystemEntries(legacyDir).Any())
                    Directory.Delete(legacyDir);
            }
            catch
            {
                /* best-effort */
            }
        }
    }

    static void CleanupLegacyAntigravityPack()
    {
        var legacy = PathHelper.AntigravityLegacyDir;
        if (!Directory.Exists(legacy)) return;

        var legacyAgents = Path.Combine(legacy, "AGENTS.md");
        if (File.Exists(legacyAgents))
        {
            try { File.Delete(legacyAgents); } catch { /* best-effort */ }
        }

        foreach (var sub in new[] { "rules", "workflows" })
        {
            var path = Path.Combine(legacy, sub);
            if (!Directory.Exists(path)) continue;
            try { Directory.Delete(path, recursive: true); } catch { /* best-effort */ }
        }

        var legacySkillsJson = Path.Combine(PathHelper.AntigravityConfigDir, "skills.json");
        if (File.Exists(legacySkillsJson))
        {
            try { File.Delete(legacySkillsJson); } catch { /* best-effort */ }
        }
    }

    internal static void InstallAntigravitySkills(string destDir, string ffRepo)
    {
        var skillsSrc = Path.Combine(ffRepo, "skills");
        if (!Directory.Exists(skillsSrc)) return;

        Directory.CreateDirectory(destDir);
        // ADR-019 Amendment: install every skill with a SKILL.md — including `flowforge`.
        foreach (var skillDir in Directory.GetDirectories(skillsSrc))
        {
            if (!File.Exists(Path.Combine(skillDir, "SKILL.md")))
                continue;

            var name = Path.GetFileName(skillDir);
            var target = Path.Combine(destDir, name);
            // A dangling symlink makes both Exists() checks false while the path
            // still exists, so CreateSymbolicLink throws. Remove the entry first
            // (dir, file or link) — see Docker E2E 2026-10-30.
            RemoveEntryForReplacement(target);

            try
            {
                Directory.CreateSymbolicLink(target, skillDir);
            }
            catch
            {
                CopyDirectoryRecursive(skillDir, target);
            }
        }
    }

    /// <summary>ADR-019 Amendment: install the 7 native Antigravity subagents.</summary>
    static void InstallAntigravityAgents(string destDir, string ideDir)
    {
        var srcDir = Path.Combine(ideDir, "agents");
        if (!Directory.Exists(srcDir)) return;

        Directory.CreateDirectory(destDir);
        foreach (var file in Directory.GetFiles(srcDir, "*.md"))
        {
            File.Copy(file, Path.Combine(destDir, Path.GetFileName(file)), overwrite: true);
        }
    }

    /// <summary>
    /// ADR-019 Amendment: legacy workflows are dead. Generate one skill per
    /// ide/antigravity/workflows/flow-*.md into &lt;destSkillsDir&gt;/&lt;name&gt;/SKILL.md.
    /// </summary>
    static void InstallAntigravityCommandSkills(string destSkillsDir, string workflowsDir)
    {
        if (!Directory.Exists(workflowsDir)) return;

        Directory.CreateDirectory(destSkillsDir);
        foreach (var workflow in Directory.GetFiles(workflowsDir, "flow-*.md"))
        {
            WriteCommandSkill(workflow, destSkillsDir);
        }
    }

    static void WriteCommandSkill(string workflowPath, string destSkillsDir)
    {
        var name = Path.GetFileNameWithoutExtension(workflowPath);
        var content = File.ReadAllText(workflowPath);
        var description = ExtractFrontmatterValue(content, "description")
                          ?? $"FlowForge command {name}";
        var nl = Environment.NewLine;
        var skillDir = Path.Combine(destSkillsDir, name);
        // A dangling symlink (legacy install pointing at a moved clone) makes
        // CreateDirectory throw and would abort the whole install (ADR-019).
        RemoveDanglingLinkOrFile(skillDir);
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            $"---{nl}name: {name}{nl}description: {description}{nl}---{nl}{nl}{StripFrontmatter(content)}");
    }

    /// <summary>
    /// Removes whatever occupies <paramref name="path"/> so it can be replaced:
    /// a real directory, a file, or a symlink (including dangling ones, where
    /// <c>Directory.Exists</c>/<c>File.Exists</c> both return false).
    /// <c>File.Delete</c> unlinks symlinks without following them; on a real
    /// directory it throws, so we fall back to a recursive delete.
    /// </summary>
    static void RemoveEntryForReplacement(string path)
    {
        try
        {
            File.Delete(path); // file, symlink (dangling or not), or no-op
            return;
        }
        catch
        {
            /* it is a real directory */
        }

        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch
        {
            /* best-effort: caller falls back to copy */
        }
    }

    /// <summary>
    /// Removes a dangling symlink (or a stray file) sitting where a skill directory
    /// must be created. Real directories are left untouched (the caller overwrites
    /// their contents). <c>Directory.CreateDirectory</c> throws EEXIST on dangling
    /// links, which aborts the install on legacy <c>~/.gemini/config/skills</c>.
    /// </summary>
    static void RemoveDanglingLinkOrFile(string path)
    {
        try
        {
            if (Directory.Exists(path))
                return; // real directory — caller overwrites its contents

            File.Delete(path); // file or symlink (dangling or not) — unlinks only
        }
        catch
        {
            /* best-effort */
        }
    }

    /// <summary>Removes a leading YAML frontmatter block (GEMINI.md must not carry one).</summary>
    static string StripFrontmatter(string content)
    {
        var text = content.TrimStart('\uFEFF');
        var firstBreak = text.IndexOf('\n');
        if (firstBreak < 0 || text[..firstBreak].TrimEnd('\r').Trim() != "---")
            return text;

        var index = firstBreak + 1;
        while (index <= text.Length)
        {
            var nextBreak = text.IndexOf('\n', index);
            var line = nextBreak < 0 ? text[index..] : text[index..nextBreak];
            if (line.TrimEnd('\r').Trim() == "---")
            {
                var body = nextBreak < 0 ? string.Empty : text[(nextBreak + 1)..];
                return body.TrimStart('\r', '\n');
            }

            if (nextBreak < 0) break;
            index = nextBreak + 1;
        }

        return text;
    }

    static string? ExtractFrontmatterValue(string content, string key)
    {
        var text = content.TrimStart('\uFEFF');
        var lines = text.Split('\n');
        if (lines.Length < 2 || lines[0].TrimEnd('\r').Trim() != "---")
            return null;

        var prefix = key + ":";
        for (var i = 1; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            if (line.Trim() == "---") break;
            if (!line.TrimStart().StartsWith(prefix, StringComparison.Ordinal)) continue;

            var value = line.TrimStart()[prefix.Length..].Trim();
            if (value.Length > 0) return value;
        }

        return null;
    }

    static void InstallVsCode(string ffRepo)
    {
        var hasCopilot = HasVsCodeExtension("github.copilot");
        var hasKilo = HasVsCodeExtension("kilocode.");

        if (hasCopilot)
        {
            EnsureDirectoryWithBackup(PathHelper.CopilotAgents);
            CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), PathHelper.CopilotAgents, "*.agent.md");
            EnsureDirectoryWithBackup(PathHelper.CopilotInstructions);
            WriteUserCopilotInstructions(
                ffRepo,
                Path.Combine(PathHelper.CopilotInstructions, "flowforge.instructions.md"));
            AnsiConsole.MarkupLine("  [green]✓[/] GitHub Copilot → [grey]~/.copilot/agents + instructions/[/]");
        }

        if (hasKilo)
        {
            InstallKilo(ffRepo);
            AnsiConsole.MarkupLine("  [green]✓[/] Kilo Code → [grey]~/.config/kilo/agents/[/]");
        }

        if (!hasCopilot && !hasKilo)
        {
            EnsureDirectoryWithBackup(PathHelper.CopilotAgents);
            CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), PathHelper.CopilotAgents, "*.agent.md");
            EnsureDirectoryWithBackup(PathHelper.CopilotInstructions);
            WriteUserCopilotInstructions(
                ffRepo,
                Path.Combine(PathHelper.CopilotInstructions, "flowforge.instructions.md"));
            InstallKilo(ffRepo);
            AnsiConsole.MarkupLine("  [yellow]![/] VS Code: no se detectó GitHub Copilot ni Kilo Code — instalados ambos formatos por si acaso");
        }

        AnsiConsole.MarkupLine("  [yellow]![/] Para repo: [bold]flowforge init[/] [grey]<ruta>[/] o [bold]ide/install.sh[/] [grey]<ruta>[/]");
    }

    static void InstallKilo(string ffRepo)
    {
        EnsureDirectoryWithBackup(PathHelper.KiloAgents);
        CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "agents"), PathHelper.KiloAgents, "*.md");
    }

    static bool HasVsCodeExtension(string home, string prefix) =>
        Directory.Exists(Path.Combine(home, ".vscode", "extensions")) &&
        Directory.EnumerateDirectories(Path.Combine(home, ".vscode", "extensions"), $"{prefix}*")
            .Any();

    /// <summary>
    /// Instala agents e instructions a nivel workspace (GitHub Copilot).
    /// </summary>
    public static void InstallVsCodeProject(string projectPath, string ffRepo)
    {
        var ghDir = Path.Combine(projectPath, ".github");
        var agentsDest = Path.Combine(ghDir, "agents");
        Directory.CreateDirectory(agentsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "vscode", "agents"), agentsDest, "*.agent.md");

        var copilotSrc = Path.Combine(ffRepo, "ide", "vscode", "copilot-instructions.md");
        if (File.Exists(copilotSrc))
            File.Copy(copilotSrc, Path.Combine(ghDir, "copilot-instructions.md"), overwrite: true);
    }

    /// <summary>
    /// Instala agents OpenCode a nivel proyecto (.opencode/agents).
    /// OpenCode CLI y Kilo Code en VS Code leen esta ruta.
    /// </summary>
    public static void InstallOpenCodeProject(string projectPath, string ffRepo)
    {
        var ocAgentsDest = Path.Combine(projectPath, ".opencode", "agents");
        Directory.CreateDirectory(ocAgentsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "agents"), ocAgentsDest, "*.md");

        var ocCommandsDest = Path.Combine(projectPath, ".opencode", "commands");
        Directory.CreateDirectory(ocCommandsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "commands"), ocCommandsDest, "*.md");

        // Kilo Code también lee .kilo/agents — duplicar para máxima compatibilidad
        var kiloDest = Path.Combine(projectPath, ".kilo", "agents");
        Directory.CreateDirectory(kiloDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "opencode", "agents"), kiloDest, "*.md");
    }

    public static void InstallCursorProject(string projectPath, string ffRepo)
    {
        var cursorDir = Path.Combine(projectPath, ".cursor");
        EnsureDirectoryWithBackup(cursorDir);

        var rulesDest = Path.Combine(cursorDir, "rules");
        EnsureDirectoryWithBackup(rulesDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "cursor", "rules"), rulesDest, "*.mdc");

        var agentsDest = Path.Combine(cursorDir, "agents");
        EnsureDirectoryWithBackup(agentsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "cursor", "agents"), agentsDest, "forge-*.md");

        var commandsDest = Path.Combine(cursorDir, "commands");
        EnsureDirectoryWithBackup(commandsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "cursor", "commands"), commandsDest, "*.md");
    }

    public static void InstallAntigravityProject(string projectPath, string ffRepo)
    {
        var ideDir = Path.Combine(ffRepo, "ide", "antigravity");
        var agentsRoot = Path.Combine(projectPath, ".agents");
        EnsureDirectoryWithBackup(agentsRoot);

        var rulesDest = Path.Combine(agentsRoot, "rules");
        EnsureDirectoryWithBackup(rulesDest);
        CopyGlob(Path.Combine(ideDir, "rules"), rulesDest, "*.md");

        var subagentsDest = Path.Combine(agentsRoot, "agents");
        EnsureDirectoryWithBackup(subagentsDest);
        InstallAntigravityAgents(subagentsDest, ideDir);

        var agentsMdSrc = Path.Combine(ideDir, "AGENTS.md");
        if (File.Exists(agentsMdSrc))
        {
            File.Copy(agentsMdSrc, Path.Combine(agentsRoot, "AGENTS.md"), overwrite: true);
            var rootAgents = Path.Combine(projectPath, "AGENTS.md");
            if (!File.Exists(rootAgents))
                File.Copy(agentsMdSrc, rootAgents, overwrite: true);
        }

        var skillsDest = Path.Combine(agentsRoot, "skills");
        InstallAntigravitySkills(skillsDest, ffRepo);
        InstallAntigravityCommandSkills(skillsDest, Path.Combine(ideDir, "workflows"));
    }

    public static bool HasVsCodeExtension(string prefix)
    {
        var extensionsDir = Path.Combine(PathHelper.HomeDir, ".vscode", "extensions");
        if (!Directory.Exists(extensionsDir))
            return false;

        return Directory.EnumerateDirectories(extensionsDir, $"{prefix}*").Any();
    }

    static void EnsureDirectoryWithBackup(string path)
    {
        BackupDirectory(path);
        Directory.CreateDirectory(path);
    }

    static void BackupDirectory(string path)
    {
        if (!Directory.Exists(path))
            return;

        if (!Directory.EnumerateFileSystemEntries(path).Any())
            return;

        // A backup must never abort the install (ADR-017): a weird entry in the
        // user directory (dangling symlink, permission issue) is reported, not fatal.
        try
        {
            Directory.CreateDirectory(PathHelper.FlowForgeBackupDir);
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var baseName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)) ?? "flowforge";
            var backupDest = Path.Combine(PathHelper.FlowForgeBackupDir, $"{baseName}-{timestamp}");
            CopyDirectoryRecursive(path, backupDest);
            AnsiConsole.MarkupLine($"  [grey]⋯[/] Backup: {backupDest}");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"  [yellow]![/] Backup omitido ({Path.GetFileName(path)}): {ex.Message}");
        }
    }

    internal static void CopyDirectoryRecursive(string source, string destination)
    {
        if (!Directory.Exists(source))
            return;

        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            // Symlinks (including dangling ones, whose target is gone) are recreated,
            // never followed: File.Copy throws FileNotFoundException on a dangling link
            // and used to abort the backup/install (Docker E2E 2026-10-30).
            var linkTarget = new FileInfo(file).LinkTarget;
            if (linkTarget is not null)
            {
                try { File.CreateSymbolicLink(destFile, linkTarget); } catch { /* best-effort */ }
                continue;
            }

            File.Copy(file, destFile, overwrite: true);
        }
    }

    static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    static void WriteUserCopilotInstructions(string ffRepo, string destPath)
    {
        var copilotSrc = Path.Combine(ffRepo, "ide", "vscode", "copilot-instructions.md");
        if (!File.Exists(copilotSrc)) return;

        var body = File.ReadAllText(copilotSrc).TrimStart();
        if (body.StartsWith("---", StringComparison.Ordinal))
        {
            File.WriteAllText(destPath, body + Environment.NewLine);
            return;
        }

        File.WriteAllText(destPath,
            $"---{Environment.NewLine}applyTo: '**'{Environment.NewLine}---{Environment.NewLine}{body}{Environment.NewLine}");
    }

    static void PatchOpenCodeFlowforgeJson(string dest, string repo)
    {
        if (!File.Exists(dest)) return;
        var content = File.ReadAllText(dest);
        if (!content.Contains("__FLOWFORGE_REPO__", StringComparison.Ordinal)) return;
        content = content.Replace("__FLOWFORGE_REPO__", repo.Replace('\\', '/'));
        File.WriteAllText(dest, content);
    }

    static void CopyGlob(string srcDir, string destDir, string pattern)
    {
        if (!Directory.Exists(srcDir)) return;
        Directory.CreateDirectory(destDir);
        foreach (var f in Directory.GetFiles(srcDir, pattern))
            File.Copy(f, Path.Combine(destDir, Path.GetFileName(f)), overwrite: true);
    }

    /// <summary>
    /// Copies skills/agents/commands from the cache repo to the IDE destination (task 10.3).
    /// Reusable from the update path. Returns the list of managed file paths (for sidecar).
    /// </summary>
    public static IReadOnlyList<string> CopySkillsForIde(string ide, string homeDir, string cacheRepo)
    {
        var managedPaths = new List<string>();
        var ideLower = ide.ToLowerInvariant();

        switch (ideLower)
        {
            case "cursor":
                managedPaths.AddRange(CopyCursorSkills(homeDir, cacheRepo));
                break;
            case "opencode":
                managedPaths.AddRange(CopyOpenCodeSkills(homeDir, cacheRepo));
                break;
            case "antigravity":
                managedPaths.AddRange(CopyAntigravitySkills(homeDir, cacheRepo));
                break;
            case "vs code" or "vscode" or "copilot":
                managedPaths.AddRange(CopyVsCodeSkills(homeDir, cacheRepo));
                break;
            case "kilo":
                managedPaths.AddRange(CopyKiloSkills(homeDir, cacheRepo));
                break;
        }

        return managedPaths;
    }

    static List<string> CopyCursorSkills(string homeDir, string cacheRepo)
    {
        var managed = new List<string>();
        var cursorDir = Path.Combine(homeDir, ".cursor");
        var ideDir = Path.Combine(cacheRepo, "ide", "cursor");
        if (!Directory.Exists(ideDir)) return managed;

        var rulesDest = Path.Combine(cursorDir, "rules");
        CopyGlobTracked(Path.Combine(ideDir, "rules"), rulesDest, "*.mdc", managed);

        var agentsDest = Path.Combine(cursorDir, "agents");
        CopyGlobTracked(Path.Combine(ideDir, "agents"), agentsDest, "forge-*.md", managed);

        var commandsDest = Path.Combine(cursorDir, "commands");
        CopyGlobTracked(Path.Combine(ideDir, "commands"), commandsDest, "*.md", managed);

        return managed;
    }

    static List<string> CopyOpenCodeSkills(string homeDir, string cacheRepo)
    {
        var managed = new List<string>();
        var opencodeDir = Path.Combine(homeDir, ".config", "opencode");

        var agentsDest = Path.Combine(opencodeDir, "agents");
        CopyGlobTracked(Path.Combine(cacheRepo, "ide", "opencode", "agents"), agentsDest, "*.md", managed);

        var commandsDest = Path.Combine(opencodeDir, "commands");
        CopyGlobTracked(Path.Combine(cacheRepo, "ide", "opencode", "commands"), commandsDest, "*.md", managed);

        return managed;
    }

    static List<string> CopyAntigravitySkills(string homeDir, string cacheRepo)
    {
        var managed = new List<string>();
        var configDir = Path.Combine(homeDir, ".gemini", "config");
        var ideDir = Path.Combine(cacheRepo, "ide", "antigravity");
        if (!Directory.Exists(ideDir)) return managed;

        var rulesDest = Path.Combine(configDir, "rules");
        CopyGlobTracked(Path.Combine(ideDir, "rules"), rulesDest, "*.md", managed);

        // ADR-019 Amendment: native subagents are part of the pack.
        var agentsDest = Path.Combine(configDir, "agents");
        CopyGlobTracked(Path.Combine(ideDir, "agents"), agentsDest, "*.md", managed);

        // Legacy workflow dirs are dead: never re-create them, drop stale flow-* files.
        RemoveLegacyFlowWorkflowFiles(Path.Combine(configDir, "workflows"));
        RemoveLegacyFlowWorkflowFiles(Path.Combine(configDir, "global_workflows"));
        RemoveLegacyFlowWorkflowFiles(Path.Combine(configDir, ".agents", "workflows"));

        // Skills (symlink or copy) + one generated skill per /flow-* command.
        var skillsDest = Path.Combine(configDir, "skills");
        CopyAntigravitySkillsTracked(skillsDest, cacheRepo, managed);
        InstallAntigravityCommandSkillsTracked(skillsDest, Path.Combine(ideDir, "workflows"), managed);

        // Workspace mirror (~/.gemini/config/.agents) — same artifacts.
        var workspaceAgents = Path.Combine(configDir, ".agents");
        CopyGlobTracked(Path.Combine(ideDir, "rules"), Path.Combine(workspaceAgents, "rules"), "*.md", managed);
        CopyGlobTracked(Path.Combine(ideDir, "agents"), Path.Combine(workspaceAgents, "agents"), "*.md", managed);
        var workspaceSkills = Path.Combine(workspaceAgents, "skills");
        CopyAntigravitySkillsTracked(workspaceSkills, cacheRepo, managed);
        InstallAntigravityCommandSkillsTracked(workspaceSkills, Path.Combine(ideDir, "workflows"), managed);

        return managed;
    }

    static void RemoveLegacyFlowWorkflowFiles(string dir)
    {
        if (!Directory.Exists(dir)) return;
        foreach (var file in Directory.GetFiles(dir, "flow-*.md"))
        {
            try { File.Delete(file); } catch { /* best-effort */ }
        }
    }

    static void InstallAntigravityCommandSkillsTracked(
        string destSkillsDir, string workflowsDir, List<string> tracked)
    {
        if (!Directory.Exists(workflowsDir)) return;
        Directory.CreateDirectory(destSkillsDir);
        foreach (var workflow in Directory.GetFiles(workflowsDir, "flow-*.md"))
        {
            var name = Path.GetFileNameWithoutExtension(workflow);
            WriteCommandSkill(workflow, destSkillsDir);
            tracked.Add(Path.Combine(destSkillsDir, name, "SKILL.md"));
        }
    }

    static List<string> CopyVsCodeSkills(string homeDir, string cacheRepo)
    {
        var managed = new List<string>();
        var copilotAgents = Path.Combine(homeDir, ".copilot", "agents");
        CopyGlobTracked(Path.Combine(cacheRepo, "ide", "vscode", "agents"), copilotAgents, "*.agent.md", managed);

        var copilotInstructions = Path.Combine(homeDir, ".copilot", "instructions");
        var instructionsDest = Path.Combine(copilotInstructions, "flowforge.instructions.md");
        var copilotSrc = Path.Combine(cacheRepo, "ide", "vscode", "copilot-instructions.md");
        if (File.Exists(copilotSrc))
        {
            Directory.CreateDirectory(copilotInstructions);
            WriteUserCopilotInstructionsTo(copilotSrc, instructionsDest);
            managed.Add(instructionsDest);
        }

        return managed;
    }

    static List<string> CopyKiloSkills(string homeDir, string cacheRepo)
    {
        var managed = new List<string>();
        var kiloAgents = Path.Combine(homeDir, ".config", "kilo", "agents");
        CopyGlobTracked(Path.Combine(cacheRepo, "ide", "opencode", "agents"), kiloAgents, "*.md", managed);
        return managed;
    }

    static void CopyGlobTracked(string srcDir, string destDir, string pattern, List<string> tracked)
    {
        if (!Directory.Exists(srcDir)) return;
        Directory.CreateDirectory(destDir);
        foreach (var f in Directory.GetFiles(srcDir, pattern))
        {
            var dest = Path.Combine(destDir, Path.GetFileName(f));
            File.Copy(f, dest, overwrite: true);
            tracked.Add(dest);
        }
    }

    internal static void CopyAntigravitySkillsTracked(string destDir, string ffRepo, List<string> tracked)
    {
        var skillsSrc = Path.Combine(ffRepo, "skills");
        if (!Directory.Exists(skillsSrc)) return;

        Directory.CreateDirectory(destDir);
        // ADR-019 Amendment: every skill with a SKILL.md — including `flowforge`.
        foreach (var skillDir in Directory.GetDirectories(skillsSrc))
        {
            if (!File.Exists(Path.Combine(skillDir, "SKILL.md")))
                continue;

            var name = Path.GetFileName(skillDir);
            var target = Path.Combine(destDir, name);
            // A dangling symlink makes both Exists() checks false while the path
            // still exists, so CreateSymbolicLink throws. Remove the entry first
            // (dir, file or link) — see Docker E2E 2026-10-30.
            RemoveEntryForReplacement(target);

            try
            {
                Directory.CreateSymbolicLink(target, skillDir);
                tracked.Add(target);
            }
            catch
            {
                CopyDirectoryRecursiveTracked(skillDir, target, tracked);
            }
        }
    }

    internal static void CopyDirectoryRecursiveTracked(string source, string destination, List<string> tracked)
    {
        if (!Directory.Exists(source)) return;
        Directory.CreateDirectory(destination);
        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, directory);
            Directory.CreateDirectory(Path.Combine(destination, relative));
        }
        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var destFile = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(destFile)!);

            // Same as CopyDirectoryRecursive: never follow a symlink.
            var linkTarget = new FileInfo(file).LinkTarget;
            if (linkTarget is not null)
            {
                try
                {
                    File.CreateSymbolicLink(destFile, linkTarget);
                    tracked.Add(destFile);
                }
                catch
                {
                    /* best-effort */
                }
                continue;
            }

            File.Copy(file, destFile, overwrite: true);
            tracked.Add(destFile);
        }
    }

    static void WriteUserCopilotInstructionsTo(string srcPath, string destPath)
    {
        var body = File.ReadAllText(srcPath).TrimStart();
        if (body.StartsWith("---", StringComparison.Ordinal))
        {
            File.WriteAllText(destPath, body + Environment.NewLine);
            return;
        }
        File.WriteAllText(destPath,
            $"---{Environment.NewLine}applyTo: '**'{Environment.NewLine}---{Environment.NewLine}{body}{Environment.NewLine}");
    }
}
