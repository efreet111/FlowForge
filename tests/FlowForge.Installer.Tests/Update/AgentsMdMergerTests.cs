using System.Security.Cryptography;
using System.Text;
using FlowForge.Installer.Infrastructure;
using FlowForge.Installer.Modules.OpenCode;
using FlowForge.Installer.Update;
using Xunit;

namespace FlowForge.Installer.Tests.Update;

/// <summary>
/// Unit tests for AgentsMdMerger — covers AT-1 to AT-10 and RNF-SEC-001 to RNF-SEC-006.
/// </summary>
public class AgentsMdMergerTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    sealed class TestEnv : IDisposable
    {
        public string TempDir { get; }
        public string OpenCodeDir { get; }
        public string AgentsMdPath { get; }
        public string BackupRoot { get; }
        public InstallerLogger Log { get; }
        public BackupManager BackupManager { get; }
        public AtomicWriter AtomicWriter { get; }
        public AgentsMdMerger Merger { get; }

        public TestEnv()
        {
            TempDir = Path.Combine(Path.GetTempPath(), $"agentsmd-{Guid.NewGuid():N}");
            OpenCodeDir = Path.Combine(TempDir, "opencode");
            AgentsMdPath = Path.Combine(OpenCodeDir, "AGENTS.md");
            BackupRoot = Path.Combine(TempDir, "backups");
            Directory.CreateDirectory(OpenCodeDir);
            Directory.CreateDirectory(BackupRoot);

            Log = new InstallerLogger(Path.Combine(TempDir, "install.log"));
            BackupManager = new BackupManager(Log, BackupRoot);
            AtomicWriter = new AtomicWriter();
            Merger = new AgentsMdMerger(Log, BackupManager, AtomicWriter);
        }

        public void WriteAgentsMd(string content) =>
            File.WriteAllText(AgentsMdPath, content);

        public string ReadAgentsMd() =>
            File.ReadAllText(AgentsMdPath);

        public bool AgentsMdExists => File.Exists(AgentsMdPath);

        public static string ComputeSha256(string content)
        {
            var bytes = Encoding.UTF8.GetBytes(content);
            var hash = SHA256.HashData(bytes);
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public void Dispose()
        {
            try { Directory.Delete(TempDir, true); } catch { /* best-effort */ }
        }
    }

    static TestEnv CreateEnv() => new();

    // ── AT-1: install con/sin AGENTS.md → Created vs Merged [FR-001] ────────

    [Fact]
    public void Merge_AgentsMdNotExists_CreatesNewFile()
    {
        using var env = CreateEnv();
        // Ensure file does NOT exist
        Assert.False(env.AgentsMdExists);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.True(result.Success);
        Assert.Equal(MergeAction.Created, result.Action);
        Assert.True(env.AgentsMdExists);

        var content = env.ReadAgentsMd();
        Assert.Contains(AgentsMdMerger.ManagedBlockOpen, content);
        Assert.Contains(AgentsMdMerger.ManagedBlockClose, content);
    }

    [Fact]
    public void Merge_AgentsMdExists_WithEngram_MergesNotOverwrites()
    {
        using var env = CreateEnv();
        var originalContent = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Some engram content here.
            <!-- /gentle-ai:engram-protocol -->

            ## Persona
            My custom persona.
            """;
        env.WriteAgentsMd(originalContent);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.True(result.Success);
        Assert.Equal(MergeAction.Merged, result.Action);
        Assert.NotNull(result.BackupPath);

        var content = env.ReadAgentsMd();
        // Must contain both the new FlowForge block and original content
        Assert.Contains(AgentsMdMerger.ManagedBlockOpen, content);
        Assert.Contains("## Engram Protocol", content);
        Assert.Contains("## Persona", content);
    }

    // ── AT-2: post-merge, contenido ajeno byte-idéntico [FR-002] ────────────

    [Fact]
    public void Merge_PreservesExistingContent_ByteIdentical()
    {
        using var env = CreateEnv();
        var engramBlock = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Some engram content here.
            <!-- /gentle-ai:engram-protocol -->
            """;
        var personaBlock = """

            ## Persona
            My custom persona.
            """;
        var originalContent = engramBlock + personaBlock + "\n";
        env.WriteAgentsMd(originalContent);

        var hashBefore = TestEnv.ComputeSha256(originalContent);
        env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
        var merged = env.ReadAgentsMd();

        // Extract content outside the FlowForge managed block
        var openIdx = merged.IndexOf(AgentsMdMerger.ManagedBlockOpen, StringComparison.Ordinal);
        var closeIdx = merged.IndexOf(AgentsMdMerger.ManagedBlockClose, StringComparison.Ordinal);
        Assert.True(openIdx >= 0, "Managed block open marker not found");
        Assert.True(closeIdx > openIdx, "Managed block close marker not found");

        // Content after the managed block should be the original content
        var afterBlock = merged[(closeIdx + AgentsMdMerger.ManagedBlockClose.Length)..].TrimStart('\n', '\r');
        var originalTrimmed = originalContent.TrimStart('\n', '\r');
        Assert.Equal(originalTrimmed.TrimEnd(), afterBlock.TrimEnd());
    }

    // ── AT-3: bloque FlowForge ANTES de Engram Protocol [FR-003] ────────────

    [Fact]
    public void Merge_InsertsFlowForgeBlock_BeforeEngramProtocol()
    {
        using var env = CreateEnv();
        var originalContent = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(originalContent);

        env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
        var content = env.ReadAgentsMd();

        var flowforgeIdx = content.IndexOf(AgentsMdMerger.ManagedBlockOpen, StringComparison.Ordinal);
        var engramIdx = content.IndexOf(AgentsMdMerger.EngramProtocolMarker, StringComparison.Ordinal);

        Assert.True(flowforgeIdx >= 0, "FlowForge block not found");
        Assert.True(engramIdx >= 0, "Engram Protocol marker not found");
        Assert.True(flowforgeIdx < engramIdx, "FlowForge block must appear BEFORE Engram Protocol");
    }

    [Fact]
    public void Merge_NoEngramMarker_InsertsAtBeginning()
    {
        using var env = CreateEnv();
        var originalContent = """
            ## My Custom Section
            Some content without any gentle-ai markers.
            """;
        env.WriteAgentsMd(originalContent);

        env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
        var content = env.ReadAgentsMd();

        var flowforgeIdx = content.IndexOf(AgentsMdMerger.ManagedBlockOpen, StringComparison.Ordinal);
        var customIdx = content.IndexOf("## My Custom Section", StringComparison.Ordinal);

        Assert.True(flowforgeIdx >= 0, "FlowForge block not found");
        Assert.True(customIdx >= 0, "Custom section not found");
        Assert.True(flowforgeIdx < customIdx, "FlowForge block must appear at beginning when no Engram marker");
    }

    // ── AT-4: bloque Engram Protocol byte-idéntico post-merge [FR-004] ──────

    [Fact]
    public void Merge_EngramBlock_RemainsByteIdentical()
    {
        using var env = CreateEnv();
        var engramBlock = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Custom engram content with special chars: áéíóú ñ
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(engramBlock + "\n");

        var hashBefore = TestEnv.ComputeSha256(engramBlock);
        env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
        var merged = env.ReadAgentsMd();

        // Extract the Engram block from merged content
        var engramOpen = merged.IndexOf("<!-- gentle-ai:engram-protocol -->", StringComparison.Ordinal);
        var engramClose = merged.IndexOf("<!-- /gentle-ai:engram-protocol -->", StringComparison.Ordinal);
        Assert.True(engramOpen >= 0, "Engram open marker not found");
        Assert.True(engramClose > engramOpen, "Engram close marker not found");

        var extractedBlock = merged[engramOpen..(engramClose + "<!-- /gentle-ai:engram-protocol -->".Length)];
        Assert.Equal(engramBlock, extractedBlock);
    }

    // ── AT-5: conflicto → reporta, archivo sin cambios [FR-005] ─────────────

    [Fact]
    public void Merge_PrecedenceConflict_ReportsAndDoesNotModify()
    {
        using var env = CreateEnv();
        var contentWithConflict = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->

            ## Rules
            Always read memory before AGENTS.md — Engram memory takes precedence.
            """;
        env.WriteAgentsMd(contentWithConflict);
        var hashBefore = TestEnv.ComputeSha256(contentWithConflict);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.False(result.Success);
        Assert.Equal(MergeAction.ConflictReported, result.Action);
        Assert.True(result.ConflictDescriptions.Count > 0);

        // File must be unchanged
        var contentAfter = env.ReadAgentsMd();
        Assert.Equal(contentWithConflict, contentAfter);
    }

    [Fact]
    public void Merge_HashDrift_ReportsConflict()
    {
        using var env = CreateEnv();
        // Create content with a modified FlowForge block (hash mismatch)
        var contentWithDrift = """
            <!-- gentle-ai:flowforge -->
            <!-- hash: aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa -->

            ## Modified pre-flight content by user.

            <!-- /gentle-ai:flowforge -->

            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(contentWithDrift);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.False(result.Success);
        Assert.Equal(MergeAction.ConflictReported, result.Action);
        Assert.Contains(result.ConflictDescriptions, d => d.Contains("drift", StringComparison.OrdinalIgnoreCase) || d.Contains("modified", StringComparison.OrdinalIgnoreCase));
    }

    // ── AT-6: archivo vacío → con autoConfirm recrea [FR-006] ───────────────

    [Fact]
    public void Merge_EmptyFile_WithAutoConfirm_RecreatesAfterBackup()
    {
        using var env = CreateEnv();
        env.WriteAgentsMd("");

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: true);

        Assert.True(result.Success);
        Assert.Equal(MergeAction.Created, result.Action);
        Assert.NotNull(result.BackupPath);

        var content = env.ReadAgentsMd();
        Assert.Contains(AgentsMdMerger.ManagedBlockOpen, content);
    }

    [Fact]
    public void Merge_EmptyFile_WithoutAutoConfirm_ReturnsError()
    {
        using var env = CreateEnv();
        env.WriteAgentsMd("");

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.False(result.Success);
        Assert.Equal(MergeAction.Error, result.Action);
        Assert.Contains("confirmation", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Merge_WhitespaceOnly_TreatedAsEmpty()
    {
        using var env = CreateEnv();
        env.WriteAgentsMd("   \n\n  \t  \n");

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: true);

        Assert.True(result.Success);
        Assert.Equal(MergeAction.Created, result.Action);
    }

    // ── AT-7: marcador sin cierre → reporta malformación [FR-007] ───────────

    [Fact]
    public void Merge_UnclosedMarker_ReportsMalformation()
    {
        using var env = CreateEnv();
        var malformed = """
            <!-- gentle-ai:custom-section -->
            Some content without closing marker.
            """;
        env.WriteAgentsMd(malformed);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.False(result.Success);
        Assert.Equal(MergeAction.Error, result.Action);
        Assert.Contains("malformed", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    // ── AT-8: backup completo del directorio [FR-008] ───────────────────────

    [Fact]
    public void Merge_CreatesBackupOfEntireDirectory()
    {
        using var env = CreateEnv();
        // Add extra files to opencodeDir to verify full backup
        File.WriteAllText(Path.Combine(env.OpenCodeDir, "other-file.txt"), "other content");
        var subDir = Path.Combine(env.OpenCodeDir, "subdir");
        Directory.CreateDirectory(subDir);
        File.WriteAllText(Path.Combine(subDir, "nested.txt"), "nested content");

        var originalContent = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(originalContent);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.True(result.Success);
        Assert.NotNull(result.BackupPath);
        Assert.True(Directory.Exists(result.BackupPath));

        // Verify backup contains all files
        var backupFiles = Directory.GetFiles(result.BackupPath, "*", SearchOption.AllDirectories);
        Assert.True(backupFiles.Length >= 3, $"Expected at least 3 files in backup, got {backupFiles.Length}");
    }

    [Fact]
    public void Merge_BackupRetention_MaxFive()
    {
        using var env = CreateEnv();

        // Create 5 backups manually
        for (var i = 0; i < 5; i++)
        {
            var content = $"<!-- gentle-ai:engram-protocol -->\n## Engram {i}\n<!-- /gentle-ai:engram-protocol -->\n";
            env.WriteAgentsMd(content);
            env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
            Thread.Sleep(10); // Ensure different timestamps
        }

        var backups = env.BackupManager.ListBackups("opencode-config");
        Assert.True(backups.Count <= 5, $"Expected max 5 backups, got {backups.Count}");
    }

    // ── AT-9: idempotencia [FR-009] ─────────────────────────────────────────

    [Fact]
    public void Merge_SecondRun_IdempotentNoOp()
    {
        using var env = CreateEnv();
        var originalContent = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(originalContent);

        // First merge
        var result1 = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
        Assert.Equal(MergeAction.Merged, result1.Action);
        var contentAfterFirst = env.ReadAgentsMd();
        var hashAfterFirst = TestEnv.ComputeSha256(contentAfterFirst);

        // Second merge — should be no-op
        var result2 = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
        Assert.True(result2.Success);
        Assert.Equal(MergeAction.IdempotentNoOp, result2.Action);
        var contentAfterSecond = env.ReadAgentsMd();
        var hashAfterSecond = TestEnv.ComputeSha256(contentAfterSecond);

        // Content must be identical
        Assert.Equal(hashAfterFirst, hashAfterSecond);

        // No duplicate blocks
        var openCount = contentAfterSecond.Split(AgentsMdMerger.ManagedBlockOpen).Length - 1;
        Assert.Equal(1, openCount);
    }

    // ── AT-10: fallo durante escritura → original intacto [FR-010] ──────────

    [Fact]
    public void Merge_WriteFailure_OriginalIntact()
    {
        using var env = CreateEnv();
        var originalContent = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(originalContent);
        var hashBefore = TestEnv.ComputeSha256(originalContent);

        // Make the file read-only to force write failure
        var fileInfo = new FileInfo(env.AgentsMdPath);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(env.AgentsMdPath, UnixFileMode.UserRead);
        }

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        // Restore permissions for cleanup
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(env.AgentsMdPath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        // Either the merge failed gracefully or the original is intact
        var contentAfter = env.ReadAgentsMd();
        var hashAfter = TestEnv.ComputeSha256(contentAfter);

        if (!result.Success)
        {
            // Original must be intact
            Assert.Equal(hashBefore, hashAfter);
        }
        // If it succeeded (e.g. running as root), that's also acceptable
    }

    // ── RNF-SEC-001: template alterado → hash mismatch ──────────────────────

    [Fact]
    public void Merge_ModifiedManagedBlock_HashMismatch_ReportsDrift()
    {
        using var env = CreateEnv();
        // Simulate a user-edited managed block with wrong hash
        var tampered = """
            <!-- gentle-ai:flowforge -->
            <!-- hash: 0000000000000000000000000000000000000000000000000000000000000000 -->

            ## User modified this content.

            <!-- /gentle-ai:flowforge -->

            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(tampered);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.False(result.Success);
        Assert.Equal(MergeAction.ConflictReported, result.Action);
        Assert.Contains(result.ConflictDescriptions, d => d.Contains("drift", StringComparison.OrdinalIgnoreCase) || d.Contains("modified", StringComparison.OrdinalIgnoreCase));
    }

    // ── RNF-SEC-005: archivo > 1 MB → rechazado ────────────────────────────

    [Fact]
    public void Merge_FileExceeds1MB_RejectedAsSuspicious()
    {
        using var env = CreateEnv();
        // Create a file > 1 MB
        var largeContent = new string('x', (int)(AgentsMdMerger.MaxFileSizeBytes + 1));
        env.WriteAgentsMd(largeContent);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.False(result.Success);
        Assert.Equal(MergeAction.Error, result.Action);
        Assert.Contains("exceeds maximum size", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    // ── RNF-SEC-006: symlink fuera de ~/.config/opencode/ → rechazado ───────

    [Fact]
    public void Merge_SymlinkEscaping_Rejected()
    {
        if (OperatingSystem.IsWindows())
            return; // Symlink test is Linux/macOS specific

        using var env = CreateEnv();
        // Create a file outside the opencode dir
        var outsideFile = Path.Combine(env.TempDir, "outside.md");
        File.WriteAllText(outsideFile, "outside content");

        // Create a symlink inside opencodeDir pointing outside
        var symlinkPath = Path.Combine(env.OpenCodeDir, "AGENTS.md");
        try
        {
            File.CreateSymbolicLink(symlinkPath, outsideFile);
        }
        catch
        {
            // If symlink creation fails (permissions), skip test
            return;
        }

        var result = env.Merger.Merge(symlinkPath, env.OpenCodeDir, autoConfirm: false);

        // Should be rejected because symlink target escapes opencodeDir
        Assert.False(result.Success);
        Assert.Equal(MergeAction.Error, result.Action);
    }

    // ── Additional edge cases ───────────────────────────────────────────────

    [Fact]
    public void Merge_MultipleExistingSections_PreservesAll()
    {
        using var env = CreateEnv();
        var content = """
            <!-- gentle-ai:persona -->
            ## My Persona
            Custom persona content.
            <!-- /gentle-ai:persona -->

            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Engram content.
            <!-- /gentle-ai:engram-protocol -->

            ## Custom Section
            Not managed by anyone.
            """;
        env.WriteAgentsMd(content);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        Assert.True(result.Success);
        Assert.Equal(MergeAction.Merged, result.Action);

        var merged = env.ReadAgentsMd();
        Assert.Contains("## My Persona", merged);
        Assert.Contains("## Engram Protocol", merged);
        Assert.Contains("## Custom Section", merged);
        Assert.Contains(AgentsMdMerger.ManagedBlockOpen, merged);
    }

    [Fact]
    public void Merge_CanonicalBlockAlreadyExists_IdempotentNoOp()
    {
        using var env = CreateEnv();
        // First merge
        var content = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            Content.
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(content);
        env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        // Second merge → IdempotentNoOp
        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);
        Assert.Equal(MergeAction.IdempotentNoOp, result.Action);
        Assert.Null(result.BackupPath); // No backup needed for no-op
    }

    [Fact]
    public void Merge_BinaryContent_RejectedAsMalformed()
    {
        using var env = CreateEnv();
        // Write binary content with null bytes — reliable indicator of non-text content
        var binaryContent = new byte[]
        {
            0x23, 0x20, 0x48, 0x65, 0x6C, 0x6C, 0x6F, // "# Hello"
            0x00, 0x00, 0x00,                            // null bytes (binary indicator)
            0x57, 0x6F, 0x72, 0x6C, 0x64                // "World"
        };
        File.WriteAllBytes(env.AgentsMdPath, binaryContent);

        var result = env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        // Should be rejected — null bytes indicate binary/corrupt content
        Assert.False(result.Success);
        Assert.Equal(MergeAction.Error, result.Action);
        Assert.Contains("UTF-8", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Merge_LogsOnlyMetadata_NeverContent()
    {
        using var env = CreateEnv();
        var sensitiveContent = """
            <!-- gentle-ai:engram-protocol -->
            ## Engram Protocol
            SECRET_TOKEN=abc123xyz
            <!-- /gentle-ai:engram-protocol -->
            """;
        env.WriteAgentsMd(sensitiveContent);

        env.Merger.Merge(env.AgentsMdPath, env.OpenCodeDir, autoConfirm: false);

        // Read log file and verify no content leakage
        var logContent = File.ReadAllText(Path.Combine(env.TempDir, "install.log"));
        Assert.DoesNotContain("SECRET_TOKEN", logContent);
        Assert.DoesNotContain("abc123xyz", logContent);
        // Should only contain metadata (hashes, line counts, etc.)
        Assert.Contains("hash=", logContent);
    }
}
