using System.Security.Cryptography;

namespace FlowForge.Installer.Update;

/// <summary>
/// Report of SHA-256 comparison for a single file.
/// </summary>
public sealed record ModifiedFileReport(
    string FilePath,
    string InstalledSha256,
    string SourceSha256,
    bool IsModified
);

/// <summary>
/// Detects user modifications to agent files by comparing SHA-256 hashes
/// between installed files and source repo files.
/// </summary>
public sealed class UserModifiedAgentDetector
{
    /// <summary>
    /// Compares SHA-256 of installed agent files against source repo files.
    /// Returns list of reports — caller decides skip/backup/overwrite.
    /// </summary>
    public IReadOnlyList<ModifiedFileReport> DetectModifications(
        string installedDir, string sourceDir, string filePattern)
    {
        var reports = new List<ModifiedFileReport>();

        if (!Directory.Exists(sourceDir))
            return reports;

        var sourceFiles = Directory.GetFiles(sourceDir, filePattern, SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDir, sourceFile);
            var installedFile = Path.Combine(installedDir, relativePath);
            var sourceHash = ComputeSha256(sourceFile);

            if (!File.Exists(installedFile))
            {
                // New file — not modified (just not installed yet)
                reports.Add(new ModifiedFileReport(
                    relativePath, "", sourceHash, false));
                continue;
            }

            var installedHash = ComputeSha256(installedFile);
            var isModified = !string.Equals(installedHash, sourceHash, StringComparison.OrdinalIgnoreCase);

            reports.Add(new ModifiedFileReport(
                relativePath, installedHash, sourceHash, isModified));
        }

        // Check for files in installed that don't exist in source (deleted upstream)
        if (Directory.Exists(installedDir))
        {
            var installedFiles = Directory.GetFiles(installedDir, filePattern, SearchOption.AllDirectories);
            foreach (var installedFile in installedFiles)
            {
                var relativePath = Path.GetRelativePath(installedDir, installedFile);
                var sourceFile = Path.Combine(sourceDir, relativePath);

                if (!File.Exists(sourceFile))
                {
                    var installedHash = ComputeSha256(installedFile);
                    reports.Add(new ModifiedFileReport(
                        relativePath, installedHash, "", false));
                }
            }
        }

        return reports;
    }

    static string ComputeSha256(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexStringLower(hash);
    }
}
