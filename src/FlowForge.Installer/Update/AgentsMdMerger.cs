using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules.OpenCode;

namespace FlowForge.Installer.Update;

/// <summary>
/// Result of a non-destructive merge of ~/.config/opencode/AGENTS.md.
/// </summary>
public sealed record AgentsMdMergeResult(
    bool Success,
    MergeAction Action,
    string? BackupPath,
    string? Error,
    IReadOnlyList<string> ConflictDescriptions
);

/// <summary>Action taken by the merger.</summary>
public enum MergeAction { Created, Merged, IdempotentNoOp, ConflictReported, Error }

/// <summary>
/// Non-destructive merge engine for ~/.config/opencode/AGENTS.md.
/// Pattern: read → parse sections → detect conflicts → inject managed block → atomic write.
/// Reuses BackupManager (directory backup) and AtomicWriter (tmp + rename).
/// </summary>
public sealed partial class AgentsMdMerger
{
    // --- Constants ---

    /// <summary>Maximum allowed file size (1 MB). RNF-SEC-005.</summary>
    public const long MaxFileSizeBytes = 1_048_576;

    /// <summary>Opening marker for the FlowForge managed block.</summary>
    public const string ManagedBlockOpen = "<!-- gentle-ai:flowforge -->";

    /// <summary>Closing marker for the FlowForge managed block.</summary>
    public const string ManagedBlockClose = "<!-- /gentle-ai:flowforge -->";

    /// <summary>Marker that identifies the Engram Protocol section.</summary>
    public const string EngramProtocolMarker = "<!-- gentle-ai:engram-protocol -->";

    /// <summary>
    /// Canonical pre-flight content injected into AGENTS.md.
    /// Derived from ide/shared/workflow-orchestrator-parity.md §"Pre-flight: AGENTS.md first (mandatory)".
    /// </summary>
    static readonly string PreflightContent = """
        ## Pre-flight: AGENTS.md first (mandatory)

        Before reading Engram memory, `.ai-work/`, or `.engram.json`, you MUST read `AGENTS.md` at the repo root.
        This is the authoritative skill index, checkpoint contract, and skill path registry.

        **Startup order (deterministic — never reorder):**
        1. `AGENTS.md` — skill index + checkpoint contract + skill paths
        2. Local files — `.ai-work/{feature-slug}/`, `.flowforge.json`
        3. Engram memory — secondary reference only; never a substitute for local state

        If Engram memory is unavailable or stale, proceed from AGENTS.md + local files alone.
        Never block or misroute because memory is absent.
        """;

    /// <summary>SHA-256 hash of the canonical pre-flight content (RNF-SEC-001).</summary>
    static readonly string CanonicalHash = ComputeSha256(PreflightContent);

    /// <summary>Precedence conflict patterns (configurable — OQ-1).</summary>
    static readonly string[] DefaultPrecedencePatterns =
    [
        "lee memoria antes que AGENTS.md",
        "read memory before AGENTS.md",
        "Engram memory takes precedence",
        "Engram memory tiene precedencia",
        "ignore AGENTS.md",
        "ignora AGENTS.md",
    ];

    readonly InstallerLogger _log;
    readonly BackupManager _backupManager;
    readonly AtomicWriter _atomicWriter;

    public AgentsMdMerger(InstallerLogger log, BackupManager backupManager, AtomicWriter atomicWriter)
    {
        _log = log;
        _backupManager = backupManager;
        _atomicWriter = atomicWriter;
    }

    /// <summary>
    /// Non-destructive merge of AGENTS.md.
    /// Flow: validate path → validate size/encoding → if exists: parse → conflict check → inject → backup → write.
    /// If not exists: create from scratch. If canonical already exists: no-op.
    /// </summary>
    public AgentsMdMergeResult Merge(string agentsMdPath, string opencodeDir, bool autoConfirm)
    {
        try
        {
            // Step 1: Validate path (RNF-SEC-006)
            ValidatePath(agentsMdPath, opencodeDir);

            // Step 2: File does not exist → create from scratch (FR-001)
            if (!File.Exists(agentsMdPath))
            {
                var newContent = BuildManagedBlock();
                Directory.CreateDirectory(opencodeDir);
                _atomicWriter.Write(agentsMdPath, newContent);
                var hash = ComputeSha256(newContent);
                _log.Info($"AgentsMdMerger: created AGENTS.md hash={hash} lines={CountLines(newContent)}");
                return new AgentsMdMergeResult(true, MergeAction.Created, null, null, []);
            }

            // Step 3: File exists → read + validate size (RNF-SEC-005) + encoding (FR-007)
            var fileInfo = new FileInfo(agentsMdPath);
            if (fileInfo.Length > MaxFileSizeBytes)
            {
                return new AgentsMdMergeResult(false, MergeAction.Error, null,
                    $"AGENTS.md exceeds maximum size ({fileInfo.Length} bytes > {MaxFileSizeBytes}). Treating as suspicious.",
                    []);
            }

            string content;
            try
            {
                content = File.ReadAllText(agentsMdPath, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                return new AgentsMdMergeResult(false, MergeAction.Error, null,
                    $"Failed to read AGENTS.md as UTF-8: {ex.Message}", []);
            }

            // Validate UTF-8: check for replacement characters or null bytes indicating binary/corrupt content
            if (content.Contains('\uFFFD') || content.Contains('\0'))
            {
                return new AgentsMdMergeResult(false, MergeAction.Error, null,
                    "AGENTS.md contains invalid UTF-8 sequences (binary/corrupt).", []);
            }

            var preHash = ComputeSha256(content);

            // Step 4: Empty/whitespace (FR-006)
            if (string.IsNullOrWhiteSpace(content))
            {
                if (!autoConfirm)
                {
                    return new AgentsMdMergeResult(false, MergeAction.Error, null,
                        "AGENTS.md is empty or whitespace-only. Requires confirmation to recreate.", []);
                }

                // Backup + recreate
                var backupPath = _backupManager.CreateBackup(opencodeDir, "opencode-config");
                var emptyContent = BuildManagedBlock();
                _atomicWriter.Write(agentsMdPath, emptyContent);
                var hash = ComputeSha256(emptyContent);
                _log.Info($"AgentsMdMerger: recreated empty AGENTS.md hash={hash} backup={backupPath}");
                return new AgentsMdMergeResult(true, MergeAction.Created, backupPath, null, []);
            }

            // Step 5: Parse sections → check for malformations (FR-007)
            var (sections, malformations) = ParseSections(content);
            if (malformations.Count > 0)
            {
                return new AgentsMdMergeResult(false, MergeAction.Error, null,
                    $"AGENTS.md has malformed section markers: {string.Join("; ", malformations)}", []);
            }

            // Step 6: Detect conflicts (FR-005, AC-5)
            var existingManagedBlock = ExtractExistingManagedBlock(content);
            var conflicts = DetectConflicts(content, existingManagedBlock);
            if (conflicts.Count > 0)
            {
                var descriptions = conflicts.Select(c => c.Description).ToList();
                _log.Warn($"AgentsMdMerger: conflicts detected count={conflicts.Count}");
                return new AgentsMdMergeResult(false, MergeAction.ConflictReported, null, null, descriptions);
            }

            // Step 7: Idempotency — if canonical managed block already exists (FR-009)
            if (existingManagedBlock != null)
            {
                var existingHash = ExtractHashFromBlock(existingManagedBlock);
                if (existingHash == CanonicalHash)
                {
                    _log.Info($"AgentsMdMerger: idempotent no-op hash={CanonicalHash}");
                    return new AgentsMdMergeResult(true, MergeAction.IdempotentNoOp, null, null, []);
                }
                // Drift detected but not reported as conflict (already checked above in DetectConflicts)
                // This path is reached only if drift was not flagged — shouldn't happen normally
            }

            // Step 8: Build merged content — insert managed block BEFORE Engram Protocol line (FR-003)
            var managedBlock = BuildManagedBlock();
            var mergedContent = InsertManagedBlock(content, managedBlock);

            // Step 9: Backup complete directory (FR-008)
            var backup = _backupManager.CreateBackup(opencodeDir, "opencode-config");

            // Step 10: Atomic write (FR-010)
            _atomicWriter.Write(agentsMdPath, mergedContent);

            // Step 11: Log metadata only (RNF-SEC-003, RNF-SEC-004)
            var postHash = ComputeSha256(mergedContent);
            var injectionLine = FindEngramProtocolLine(content);
            _log.Info($"AgentsMdMerger: merged pre_hash={preHash} post_hash={postHash} injection_line={injectionLine} backup={backup}");

            // Step 12: Return Merged
            return new AgentsMdMergeResult(true, MergeAction.Merged, backup, null, []);
        }
        catch (Exception ex) when (ex is not FileNotFoundException)
        {
            _log.Error($"AgentsMdMerger: merge failed: {ex.Message}");
            return new AgentsMdMergeResult(false, MergeAction.Error, null, ex.Message, []);
        }
    }

    // --- Private methods (inline, no separate classes) ---

    /// <summary>Canonicalizes path and rejects symlinks that escape ~/.config/opencode/ (RNF-SEC-006).</summary>
    static void ValidatePath(string agentsMdPath, string opencodeDir)
    {
        var canonicalTarget = Path.GetFullPath(agentsMdPath);
        var canonicalDir = Path.GetFullPath(opencodeDir);

        // If the file exists, resolve symlinks
        if (File.Exists(agentsMdPath))
        {
            var fileInfo = new FileInfo(agentsMdPath);
            if (fileInfo.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                // Resolve symlink target
                var resolvedTarget = ResolveSymlink(agentsMdPath);
                if (resolvedTarget != null)
                {
                    canonicalTarget = resolvedTarget;
                }
            }
        }

        // Verify the target is under the opencode directory
        if (!canonicalTarget.StartsWith(canonicalDir, StringComparison.Ordinal))
        {
            throw new UnauthorizedAccessException(
                $"AGENTS.md path '{canonicalTarget}' escapes OpenCode directory '{canonicalDir}'. Rejected (RNF-SEC-006).");
        }
    }

    static string? ResolveSymlink(string path)
    {
        try
        {
            // On Linux/macOS, read the symlink target
            if (!OperatingSystem.IsWindows())
            {
                var info = new FileInfo(path);
                if (info.LinkTarget != null)
                {
                    var baseDir = Path.GetDirectoryName(path) ?? ".";
                    return Path.GetFullPath(info.LinkTarget, baseDir);
                }
            }
        }
        catch
        {
            // Best effort — if we can't resolve, continue with original path
        }
        return null;
    }

    /// <summary>Parses &lt;!-- gentle-ai:* --&gt;...&lt;!-- /gentle-ai:* --&gt; sections. Returns (sections, malformations).</summary>
    static (IReadOnlyList<MarkdownSection> Sections, IReadOnlyList<string> Malformations) ParseSections(string content)
    {
        var sections = new List<MarkdownSection>();
        var malformations = new List<string>();
        var lines = content.Split('\n');

        var openPattern = SectionOpenRegex();
        var closePattern = SectionCloseRegex();

        var openStack = new Stack<(string Name, int LineIndex)>();

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');

            var openMatch = openPattern.Match(line);
            if (openMatch.Success)
            {
                var name = openMatch.Groups[1].Value;
                openStack.Push((name, i));
                continue;
            }

            var closeMatch = closePattern.Match(line);
            if (closeMatch.Success)
            {
                var name = closeMatch.Groups[1].Value;
                if (openStack.Count > 0 && openStack.Peek().Name == name)
                {
                    var (openName, openLine) = openStack.Pop();
                    var blockBuilder = new StringBuilder();
                    for (var j = openLine; j <= i; j++)
                    {
                        blockBuilder.AppendLine(lines[j].TrimEnd('\r'));
                    }
                    sections.Add(new MarkdownSection(openName, blockBuilder.ToString().TrimEnd(), openLine, i));
                }
                else
                {
                    malformations.Add($"Closing marker for '{name}' at line {i + 1} has no matching opener");
                }
            }
        }

        // Any remaining open markers are malformations
        while (openStack.Count > 0)
        {
            var (name, lineIdx) = openStack.Pop();
            malformations.Add($"Opening marker for '{name}' at line {lineIdx + 1} has no matching closer");
        }

        return (sections, malformations);
    }

    /// <summary>Locates the line of the Engram Protocol marker for insertion before it.</summary>
    static int FindEngramProtocolLine(string content)
    {
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim().Contains(EngramProtocolMarker, StringComparison.Ordinal))
                return i;
        }
        return -1;
    }

    /// <summary>Builds the complete managed block with markers + embedded hash.</summary>
    static string BuildManagedBlock()
    {
        var sb = new StringBuilder();
        sb.AppendLine(ManagedBlockOpen);
        sb.AppendLine($"<!-- hash: {CanonicalHash} -->");
        sb.AppendLine();
        sb.AppendLine(PreflightContent);
        sb.AppendLine();
        sb.AppendLine(ManagedBlockClose);
        return sb.ToString();
    }

    /// <summary>Detects conflicts: (a) drift of managed block vs canonical hash, (b) precedence patterns.</summary>
    static IReadOnlyList<ConflictReport> DetectConflicts(string content, string? existingManagedBlock)
    {
        var conflicts = new List<ConflictReport>();

        // (a) Hash drift detection
        if (existingManagedBlock != null)
        {
            var existingHash = ExtractHashFromBlock(existingManagedBlock);
            if (existingHash != null && existingHash != CanonicalHash)
            {
                conflicts.Add(new ConflictReport(
                    ConflictKind.Drift,
                    $"FlowForge managed block has been modified (hash drift: expected {CanonicalHash[..12]}..., found {existingHash[..Math.Min(12, existingHash.Length)]}...)",
                    null));
            }
        }

        // (b) Precedence pattern scan
        var lines = content.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            foreach (var pattern in DefaultPrecedencePatterns)
            {
                if (line.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                {
                    conflicts.Add(new ConflictReport(
                        ConflictKind.PrecedenceRule,
                        $"Line {i + 1}: precedence conflict detected — \"{pattern}\"",
                        i + 1));
                }
            }
        }

        return conflicts;
    }

    /// <summary>Extracts the existing managed block content, or null if not present.</summary>
    static string? ExtractExistingManagedBlock(string content)
    {
        var openIdx = content.IndexOf(ManagedBlockOpen, StringComparison.Ordinal);
        if (openIdx < 0) return null;

        var closeIdx = content.IndexOf(ManagedBlockClose, StringComparison.Ordinal);
        if (closeIdx < 0 || closeIdx <= openIdx) return null;

        return content[openIdx..(closeIdx + ManagedBlockClose.Length)];
    }

    /// <summary>Extracts the hash value from a managed block's &lt;!-- hash: ... --&gt; line.</summary>
    static string? ExtractHashFromBlock(string block)
    {
        var hashPattern = HashLineRegex();
        var match = hashPattern.Match(block);
        return match.Success ? match.Groups[1].Value : null;
    }

    /// <summary>Inserts the managed block before the Engram Protocol line (or at start if no Engram marker).</summary>
    static string InsertManagedBlock(string content, string managedBlock)
    {
        var engramLine = FindEngramProtocolLine(content);

        if (engramLine < 0)
        {
            // No Engram Protocol → insert at the beginning
            return managedBlock.TrimEnd() + Environment.NewLine + Environment.NewLine + content;
        }

        // Insert before the Engram Protocol line
        var lines = content.Split('\n');
        var sb = new StringBuilder();

        for (var i = 0; i < engramLine; i++)
        {
            sb.AppendLine(lines[i].TrimEnd('\r'));
        }

        sb.Append(managedBlock.TrimEnd());
        sb.AppendLine();
        sb.AppendLine();

        for (var i = engramLine; i < lines.Length; i++)
        {
            sb.AppendLine(lines[i].TrimEnd('\r'));
        }

        return sb.ToString().TrimEnd() + Environment.NewLine;
    }

    static int CountLines(string content) => content.Split('\n').Length;

    static string ComputeSha256(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // --- Source-generated regex patterns ---

    [GeneratedRegex(@"<!--\s*gentle-ai:(\S+)\s*-->")]
    private static partial Regex SectionOpenRegex();

    [GeneratedRegex(@"<!--\s*/gentle-ai:(\S+)\s*-->")]
    private static partial Regex SectionCloseRegex();

    [GeneratedRegex(@"<!--\s*hash:\s*([a-fA-F0-9]+)\s*-->")]
    private static partial Regex HashLineRegex();
}

// --- Internal types (file-scoped) ---

internal sealed record MarkdownSection(string Name, string FullBlock, int StartLine, int EndLine);
internal sealed record ConflictReport(ConflictKind Kind, string Description, int? LineNumber);
internal enum ConflictKind { Drift, PrecedenceRule }
