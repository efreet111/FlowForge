using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FlowForge.Installer.Commands;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules.OpenCode;
using Spectre.Console;

namespace FlowForge.Installer.Modules;

/// <summary>
/// Instala los skills/agents/commands de FlowForge en los IDEs seleccionados.
/// </summary>
public sealed class FlowForgeModule(InstallerContext ctx)
{
    public const string InstallerVersion = "0.1.0-alpha.6";

    public void Install(List<string> selectedIdes)
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
            InstallForIde(ide, home, ffRepo);
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

    void InstallForIde(string ide, string home, string ffRepo)
    {
        switch (ide.ToLowerInvariant())
        {
            case "cursor":
                InstallCursor(home, ffRepo);
                break;
            case "opencode":
                InstallOpenCode(ffRepo, home);
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

    void InstallOpenCode(string ffRepo, string home, bool forceFree = false, bool dryRun = false, bool jsonOnly = false)
    {
        var opencodeDir = Path.Combine(home, ".config", "opencode");
        var agentsDest = Path.Combine(opencodeDir, "agents");
        var commandsDest = Path.Combine(opencodeDir, "commands");
        var rulesDest = Path.Combine(opencodeDir, ".agents", "rules");
        EnsureDirectoryWithBackup(agentsDest);
        EnsureDirectoryWithBackup(commandsDest);
        EnsureDirectoryWithBackup(rulesDest);

        var templatesDir = Path.Combine(ffRepo, "ide", "opencode", "templates");
        var agentModelsPath = Path.Combine(templatesDir, "agent-models.json");
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

        var configGen = new OpenCodeConfigGenerator(ffRepo, forceFree, dryRun);
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
            var modelGen = new ModelAssignmentsGenerator(agentModelsPath, rulesPath);
            modelGen.Generate(configPath);
            modifiedFiles.Add(configPath);
            modifiedFiles.Add(rulesPath);

            if (!jsonOnly)
            {
                var manifest = JsonSerializer.Deserialize<OpenCodeConfigGenerator.AgentModelsManifest>(
                    File.ReadAllText(agentModelsPath))
                    ?? throw new InvalidOperationException("agent-models.json inválido.");

                var patcher = new AgentFrontmatterPatcher();
                var templateAgents = Path.Combine(ffRepo, "ide", "opencode", "agents");
                if (Directory.Exists(templateAgents))
                {
                    foreach (var templateAgent in Directory.GetFiles(templateAgents, "*.md.tpl"))
                    {
                        var agentName = Path.GetFileNameWithoutExtension(templateAgent);
                        if (agentName.EndsWith(".md"))
                            agentName = agentName[..^3];

                        var dest = Path.Combine(agentsDest, $"{agentName}.md");
                        File.Copy(templateAgent, dest, overwrite: true);
                        if (manifest.Agents.TryGetValue(agentName, out var entry))
                            patcher.Patch(dest, entry.Model.StartsWith("opencode-zen/")
                                ? entry.Model
                                : $"opencode-zen/{entry.Model}");
                        modifiedFiles.Add(dest);
                    }
                }

                var commandsSrc = Path.Combine(ffRepo, "ide", "opencode", "commands");
                CopyGlob(commandsSrc, commandsDest, "*.md");
                modifiedFiles.Add(commandsDest);
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
        EnsureDirectoryWithBackup(PathHelper.AntigravityWorkflows);
        EnsureDirectoryWithBackup(PathHelper.AntigravitySkills);

        var workspaceAgents = PathHelper.AntigravityWorkspaceAgents;
        EnsureDirectoryWithBackup(Path.Combine(workspaceAgents, "rules"));
        EnsureDirectoryWithBackup(Path.Combine(workspaceAgents, "workflows"));
        EnsureDirectoryWithBackup(Path.Combine(workspaceAgents, "skills"));

        var agentsMd = Path.Combine(ideDir, "AGENTS.md");
        if (File.Exists(agentsMd))
        {
            File.Copy(agentsMd, Path.Combine(configDir, "AGENTS.md"), overwrite: true);
            File.Copy(agentsMd, Path.Combine(workspaceAgents, "AGENTS.md"), overwrite: true);
        }

        CopyGlob(Path.Combine(ideDir, "rules"), PathHelper.AntigravityRules, "*.md");
        CopyGlob(Path.Combine(ideDir, "workflows"), PathHelper.AntigravityWorkflows, "*.md");
        CopyGlob(Path.Combine(ideDir, "rules"), Path.Combine(workspaceAgents, "rules"), "*.md");
        CopyGlob(Path.Combine(ideDir, "workflows"), Path.Combine(workspaceAgents, "workflows"), "*.md");

        InstallAntigravitySkills(PathHelper.AntigravitySkills, ffRepo);
        InstallAntigravitySkills(Path.Combine(workspaceAgents, "skills"), ffRepo);

        var workflowRule = Path.Combine(ideDir, "rules", "workflow.md");
        if (File.Exists(workflowRule))
            File.Copy(workflowRule, Path.Combine(PathHelper.HomeDir, ".gemini", "GEMINI.md"), overwrite: true);

        CleanupLegacyAntigravityPack();
        AnsiConsole.MarkupLine("  [green]✓[/] Antigravity → [grey]~/.gemini/config/ (AGENTS + rules + workflows + skills)[/]");
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

    static void InstallAntigravitySkills(string destDir, string ffRepo)
    {
        var skillsSrc = Path.Combine(ffRepo, "skills");
        if (!Directory.Exists(skillsSrc)) return;

        Directory.CreateDirectory(destDir);
        foreach (var skillDir in Directory.GetDirectories(skillsSrc, "forge-*"))
        {
            var name = Path.GetFileName(skillDir);
            var target = Path.Combine(destDir, name);
            if (Directory.Exists(target) || File.Exists(target))
            {
                try { Directory.Delete(target, recursive: true); } catch { /* best-effort */ }
            }

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
        var agentsRoot = Path.Combine(projectPath, ".agents");
        EnsureDirectoryWithBackup(agentsRoot);

        var rulesDest = Path.Combine(agentsRoot, "rules");
        EnsureDirectoryWithBackup(rulesDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "antigravity", "rules"), rulesDest, "*.md");

        var workflowsDest = Path.Combine(agentsRoot, "workflows");
        EnsureDirectoryWithBackup(workflowsDest);
        CopyGlob(Path.Combine(ffRepo, "ide", "antigravity", "workflows"), workflowsDest, "*.md");

        var agentsMdSrc = Path.Combine(ffRepo, "ide", "antigravity", "AGENTS.md");
        if (File.Exists(agentsMdSrc))
        {
            File.Copy(agentsMdSrc, Path.Combine(agentsRoot, "AGENTS.md"), overwrite: true);
            var rootAgents = Path.Combine(projectPath, "AGENTS.md");
            if (!File.Exists(rootAgents))
                File.Copy(agentsMdSrc, rootAgents, overwrite: true);
        }

        InstallAntigravitySkills(Path.Combine(agentsRoot, "skills"), ffRepo);
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

        Directory.CreateDirectory(PathHelper.FlowForgeBackupDir);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
        var baseName = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar)) ?? "flowforge";
        var backupDest = Path.Combine(PathHelper.FlowForgeBackupDir, $"{baseName}-{timestamp}");
        CopyDirectoryRecursive(path, backupDest);
        AnsiConsole.MarkupLine($"  [grey]⋯[/] Backup: {backupDest}");
    }

    static void CopyDirectoryRecursive(string source, string destination)
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
}
