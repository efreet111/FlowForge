using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace FlowForge.Installer.Tests;

/// <summary>
/// Structural contract tests for the /fforge-init skill (HU-047).
/// Mirrors the style of <see cref="ScriptTests"/>: assertions on the shipped
/// artifacts rather than on runtime behaviour (that is covered by
/// tests/fforge-init/run-tests.sh, which needs only bash).
/// Contracts under test: skills/forge-init/contracts.md (C-1 … C-10).
/// </summary>
public class FforgeInitScriptTests
{
    const string SkillDir = "skills/forge-init";
    const string SkillPath = $"{SkillDir}/SKILL.md";
    const string ContractsPath = $"{SkillDir}/contracts.md";
    const string EnginePath = $"{SkillDir}/scripts/fforge-init.sh";
    const string BaseTemplatePath = $"{SkillDir}/templates/base/AGENTS.md.base";
    const string BaseMetaPath = $"{SkillDir}/templates/base/base.meta.json";
    const string FragmentDir = $"{SkillDir}/templates/fragments";
    const string ProjectTemplatePath = "templates/project/AGENTS.md.template";

    static string Load(string relativePath) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, relativePath));

    static string[] FragmentPaths() =>
        Directory.GetFiles(Path.Combine(AppContext.BaseDirectory, FragmentDir), "*.md.fragment",
                           SearchOption.AllDirectories)
                 .OrderBy(p => p, StringComparer.Ordinal)
                 .ToArray();

    /// <summary>Frontmatter block of a Markdown file, without the --- fences.</summary>
    static string Frontmatter(string path)
    {
        var lines = File.ReadAllLines(path);
        Assert.Equal("---", lines[0].Trim());
        var end = Array.FindIndex(lines, 1, l => l.Trim() == "---");
        Assert.True(end > 0, $"{path}: frontmatter is not closed");
        return string.Join("\n", lines.Skip(1).Take(end - 1));
    }

    static string FrontmatterScalar(string frontmatter, string key)
    {
        var match = Regex.Match(frontmatter, $@"(?m)^\s*{Regex.Escape(key)}:\s*(.+?)\s*$");
        return match.Success ? match.Groups[1].Value.Trim().Trim('"') : string.Empty;
    }

    // ---------------------------------------------------------------- C-1

    [Fact]
    public void FR_001_SkillFrontmatterDeclaresNameAndTrigger()
    {
        var fm = Frontmatter(Path.Combine(AppContext.BaseDirectory, SkillPath));
        Assert.Equal("forge-init", FrontmatterScalar(fm, "name"));
        Assert.Equal("fforge-init, init AGENTS.md, inicializar AGENTS.md, sync AGENTS.md, adopt FlowForge in project",
                     FrontmatterScalar(fm, "trigger"));
        Assert.Contains("Initialize or synchronize AGENTS.md", fm);
    }

    [Fact]
    public void FR_001_SkillDisablesAutoInvocation()
    {
        // HU-045: install-skills.sh looks for this exact string in Step 4.
        var content = Load(SkillPath);
        Assert.Contains("metadata:", content);
        Assert.Contains("opencode/autoinvoke: false", content);
    }

    [Fact]
    public void FR_001_SkillCarriesVersionAndChangelog()
    {
        var fm = Frontmatter(Path.Combine(AppContext.BaseDirectory, SkillPath));
        Assert.Matches(@"^\d+\.\d+\.\d+$", FrontmatterScalar(fm, "version"));
        Assert.Contains("changelog:", fm);
        Assert.Contains("- date:", fm);
        Assert.Contains("note:", fm);
    }

    [Fact]
    public void CONV_003_DirectoryMatchesInstallerGlob()
    {
        // install-skills.sh discovers skills with ls -d skills/forge-*/SKILL.md
        Assert.Matches(@"^forge-[a-z0-9-]+$", new DirectoryInfo(
            Path.Combine(AppContext.BaseDirectory, SkillDir)).Name);
        Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, SkillPath)));
    }

    [Fact]
    public void FR_001_SkillShipsTemplatesAndEngine()
    {
        foreach (var required in new[]
                 {
                     SkillPath, ContractsPath, EnginePath, BaseTemplatePath, BaseMetaPath,
                     $"{SkillDir}/scripts/fforge-init.sh",
                 })
        {
            Assert.True(File.Exists(Path.Combine(AppContext.BaseDirectory, required)),
                        $"missing shipped file: {required}");
        }

        Assert.Equal(11, FragmentPaths().Length);
    }

    // ---------------------------------------------------------------- C-6

    [Fact]
    public void FR_006_EveryFragmentDeclaresItsManifest()
    {
        foreach (var path in FragmentPaths())
        {
            var fm = Frontmatter(path);
            var id = FrontmatterScalar(fm, "id");
            var kind = FrontmatterScalar(fm, "kind");
            Assert.False(string.IsNullOrWhiteSpace(id), $"{path}: missing id");
            Assert.Contains(kind, new[] { "stack", "functionality", "agent" });
            Assert.False(string.IsNullOrWhiteSpace(FrontmatterScalar(fm, "label")), $"{path}: missing label");
            Assert.Equal("1", FrontmatterScalar(fm, "version"));
            Assert.Matches(@"^[a-z][a-z0-9-]*$", id);
        }
    }

    [Fact]
    public void FR_006_FragmentIdsAreUnique()
    {
        var ids = FragmentPaths()
            .Select(p => FrontmatterScalar(Frontmatter(p), "id"))
            .ToList();
        Assert.Equal(ids.Count, ids.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void FR_006_EveryFragmentDeclaresDetect()
    {
        foreach (var path in FragmentPaths())
        {
            Assert.Contains("detect:", File.ReadAllText(path));
        }
    }

    [Fact]
    public void CONV_002_FragmentsAreInertMarkdown()
    {
        foreach (var path in FragmentPaths())
        {
            var body = File.ReadAllText(path);
            Assert.DoesNotContain("```", body);                 // no shell/code blocks
            Assert.DoesNotContain(".md.fragment", body);        // no cross-references
            Assert.DoesNotMatch(@"__[A-Z_]+__", body);          // no template placeholders
            Assert.DoesNotContain("curl ", body);
            Assert.DoesNotContain("npm install", body);
        }
    }

    [Fact]
    public void FR_009_AgentFragmentsAreNotMutuallyExclusive()
    {
        var agentFragments = FragmentPaths()
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}agent{Path.DirectorySeparatorChar}")
                        || p.Contains("/agent/"))
            .ToArray();
        Assert.Equal(3, agentFragments.Length);

        var agentIds = agentFragments
            .Select(p => FrontmatterScalar(Frontmatter(p), "id"))
            .ToArray();
        Assert.Contains("opencode", agentIds);
        Assert.Contains("claude-code", agentIds);
        Assert.Contains("cursor", agentIds);

        foreach (var path in agentFragments)
        {
            var fm = Frontmatter(path);
            var conflicts = Regex.Match(fm, @"(?m)^conflicts:\s*\[(.*?)\]").Groups[1].Value;
            foreach (var id in agentIds)
            {
                Assert.DoesNotContain(id, conflicts);
            }
        }
    }

    // ---------------------------------------------------------------- C-7

    [Fact]
    public void FR_008_BaseTemplateUsesOnlyTheSharedPlaceholderVocabulary()
    {
        var shared = new HashSet<string>(StringComparer.Ordinal)
        {
            "__PROJECT_NAME__", "__DATE__", "__FLOWFORGE_VERSION__",
            "__STACK_LANG__", "__STACK_FRAMEWORK__", "__STACK_TESTS__", "__STACK_VERSION__",
        };

        var baseTemplate = Load(BaseTemplatePath);
        var tokens = Regex.Matches(baseTemplate, @"__[A-Z_]+__")
                          .Select(m => m.Value)
                          .Distinct()
                          .ToArray();
        Assert.NotEmpty(tokens);
        foreach (var token in tokens)
        {
            Assert.True(shared.Contains(token), $"base template uses a non-shared placeholder: {token}");
        }

        // every placeholder the base uses must also be documented in base.meta.json
        var meta = Load(BaseMetaPath);
        var declared = Regex.Matches(meta, "\"(__[A-Z_]+__)\"")
                            .Select(m => m.Groups[1].Value)
                            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("__PROJECT_NAME__", declared);
        foreach (var token in tokens.Where(t => t.StartsWith("__STACK_", StringComparison.Ordinal)))
        {
            Assert.Contains(token, declared);
        }
    }

    [Fact]
    public void FR_008_BaseTemplateCarriesTheRequiredScaffolds()
    {
        var content = Load(BaseTemplatePath);
        Assert.Contains("## Overview", content);
        Assert.Contains("## Tech Stack", content);
        Assert.Contains("## AI Agent Rules", content);
        Assert.Contains("## Build & Test Commands", content);
        Assert.Contains("## Documentation", content);
        Assert.Contains("## Skills", content);
        Assert.Contains("<!-- fforge-init:fragments -->", content);
        Assert.Contains("fforge-init:begin", content);
        Assert.Contains("fforge-init:end", content);
    }

    [Fact]
    public void FR_008_BaseTemplateMarkdownIsBalanced()
    {
        var lines = Load(BaseTemplatePath).Split('\n');
        var fences = lines.Count(l => l.TrimStart().StartsWith("```", StringComparison.Ordinal));
        Assert.Equal(0, fences % 2);
        var opens = lines.Count(l => l.TrimStart().StartsWith("<!--", StringComparison.Ordinal));
        var closes = lines.Count(l => l.Contains("-->"));
        Assert.Equal(opens, closes);
    }

    [Fact]
    public void FR_006_BaseMetaDeclaresTheFourProfileNames()
    {
        var meta = Load(BaseMetaPath);
        foreach (var profile in new[] { "blank", "kotlin-android", "web", "python" })
        {
            Assert.Contains($"\"{profile}\"", meta);
        }
        Assert.Contains("\"requiredSections\"", meta);
        Assert.Contains("\"skills\"", meta);
    }

    // ---------------------------------------------------------------- C-8

    [Fact]
    public void FR_001_EngineExitCodeTableIsFrozen()
    {
        var engine = Load(EnginePath);
        var contracts = Load(ContractsPath);
        foreach (var row in new[]
                 {
                     "0  success", "1  usage error", "2  target not found",
                     "3  permission denied", "4  unresolved external skills",
                     "5  unresolved conflict", "6  invalid plan.json", "7  write failed mid-apply",
                 })
        {
            Assert.Contains(row, engine);
        }

        foreach (var row in new[]
                 {
                     "| `0` |", "| `1` |", "| `2` |", "| `3` |",
                     "| `4` |", "| `5` |", "| `6` |", "| `7` |",
                 })
        {
            Assert.Contains(row, contracts);
        }
    }

    [Fact]
    public void FR_001_EngineDocumentsItsSubcommands()
    {
        var engine = Load(EnginePath);
        foreach (var sub in new[]
                 {
                     "detect", "analyze", "classify", "recommend",
                     "preview", "apply", "restore", "version", "help",
                 })
        {
            Assert.Contains(sub, engine);
        }
        Assert.Contains("fforge-init.sh detect", engine);
        Assert.Contains("fforge-init.sh apply", engine);
    }

    [Fact]
    public void FR_001_EngineCitesTheContract()
    {
        Assert.Contains("contracts.md", Load(EnginePath));
        Assert.Contains("contracts.md", Load(SkillPath));
    }

    [Fact]
    public void CONV_001_EverySubcommandSupportsJson()
    {
        var engine = Load(EnginePath);
        Assert.Contains("--json", engine);
        var jsonFlags = Regex.Matches(engine, @"--json\)").Count;
        Assert.True(jsonFlags >= 6, $"expected --json on at least 6 subcommands, found {jsonFlags}");
    }

    // ---------------------------------------------------------------- NFR-006

    [Fact]
    public void NFR_006_EngineUsesNoExternalRuntime()
    {
        var engine = Load(EnginePath);
        Assert.StartsWith("#!/usr/bin/env bash", engine);
        Assert.Contains("set -u", engine);
        foreach (var forbidden in new[]
                 {
                     "curl ", "wget ", "npm ", "npx ", "node ", "jq ", "pip install", "git clone",
                 })
        {
            Assert.DoesNotContain(forbidden, engine);
        }
        Assert.Contains("sha256sum", engine);
        Assert.Contains("awk", engine);
    }

    [Fact]
    public void NFR_006_SkillIsSelfContained()
    {
        var skill = Load(SkillPath);
        var contracts = Load(ContractsPath);
        Assert.Contains("NFR-006", contracts);
        // the skill must not delegate to another skill at runtime
        Assert.DoesNotContain("flowdoc-assist", skill);
        Assert.DoesNotContain("find-skills", skill);
    }

    // ---------------------------------------------------------------- C-9

    [Fact]
    public void NFR_004_BackupContractIsDocumented()
    {
        var contracts = Load(ContractsPath);
        Assert.Contains(".agents.md.backup", contracts);
        Assert.Contains(".agents.md.backup.tmp", contracts);
        Assert.Contains("restore", contracts);
        Assert.Contains("--dry-run", contracts);

        var engine = Load(EnginePath);
        Assert.Contains(".agents.md.backup", engine);
        Assert.Contains("restore", engine);
        Assert.Contains("--dry-run", engine);
    }

    [Fact]
    public void NFR_004_EngineNeverTouchesTheGlobalAgentsMd()
    {
        // DEC-5: this skill owns ./AGENTS.md only; ~/.config/opencode/AGENTS.md belongs to the installer
        var engine = Load(EnginePath);
        Assert.DoesNotContain(".config/opencode", engine);
        Assert.DoesNotContain("flowforge-backups", engine);
    }

    // ---------------------------------------------------------------- contracts

    [Fact]
    public void FR_001_ContractsDocumentEveryFrozenSection()
    {
        var contracts = Load(ContractsPath);
        foreach (var section in new[]
                 {
                     "C-1. Skill frontmatter contract",
                     "C-2. Section model",
                     "C-3. Merge config",
                     "C-4. Classification contract",
                     "C-5. Managed-block marker contract",
                     "C-6. Fragment manifest contract",
                     "C-7. Base template contract",
                     "C-7a. Skills union",
                     "C-7b. Title rule",
                     "C-8. Engine CLI contract",
                     "C-9. Backup / rollback contract",
                     "C-10. Source-changed guard",
                 })
        {
            Assert.Contains(section, contracts);
        }
    }

    [Fact]
    public void FR_004_ContractsFreezeTheClassificationPrefixes()
    {
        var contracts = Load(ContractsPath);
        Assert.Contains("^forge-", contracts);
        Assert.Contains("^flowdoc-", contracts);
        Assert.Contains("^reginald-", contracts);
        Assert.Contains("sdd-explore", contracts);
        Assert.Contains("forge-discovery", contracts);
    }

    [Fact]
    public void FR_001_ContractsKeepTheNameDivergence()
    {
        // CONV-3: source dir skills/forge-init, command /fforge-init — documented, not a bug
        var contracts = Load(ContractsPath);
        Assert.Contains("forge-init", contracts);
        Assert.Contains("fforge-init", contracts);
        Assert.Contains("don't \"fix\" this back", contracts);
    }

    // ---------------------------------------------------------------- T-013

    [Fact]
    public void FR_001_SlashCommandIsRegistered()
    {
        var command = Load("ide/opencode/commands/fforge-init.md");
        Assert.StartsWith("---", command);
        Assert.Contains("description:", command);
        Assert.Contains("agent: flowforge", command);
        Assert.Contains("--dry-run", command);
        Assert.Contains("--restore", command);
        Assert.Contains("forge-init/SKILL.md", command);
    }

    // ---------------------------------------------------------------- registry

    [Fact]
    public void FR_001_RootAgentsMdIndexesTheSkill()
    {
        var index = Load("AGENTS.md");
        Assert.Contains("forge-init", index);
        Assert.Contains("skills/forge-init/SKILL.md", index);
        Assert.Contains("On-demand", index);
    }

    [Fact]
    public void FR_001_InstallerBannerListsTheSkill()
    {
        var installer = Load("install-skills.sh");
        Assert.Contains("forge-init", installer);
        Assert.Contains("Core Skills (12)", installer);
        Assert.Contains("CORE_COUNT", installer);
    }
}
