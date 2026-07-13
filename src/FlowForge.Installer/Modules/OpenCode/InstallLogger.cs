using System.IO;
using System.Linq;
using System.IO;
using System.Linq;
using System.Text;

namespace FlowForge.Installer.Modules.OpenCode;

public sealed class InstallLogger
{
    readonly string _installerVersion;
    readonly string _backupRoot;

    public InstallLogger(string installerVersion)
    {
        _installerVersion = installerVersion;
        _backupRoot = FlowForge.Installer.Infrastructure.PathHelper.OpenCodeBackupsDir;
    }

    public void Append(string[] modifiedFiles, string? preHash, string? postHash, bool usedSudo)
    {
        var timestamp = DateTime.UtcNow;
        var dir = Path.Combine(_backupRoot, timestamp.ToString("yyyyMMddHHmmss"));
        Directory.CreateDirectory(dir);

        RotateOldLogs();

        var logPath = Path.Combine(dir, "install.log");
        var builder = new StringBuilder();
        builder.AppendLine($"timestamp={timestamp:O}");
        builder.AppendLine($"installer_version={_installerVersion}");
        builder.AppendLine($"user={Environment.UserName}");
        builder.AppendLine($"env_user={Environment.GetEnvironmentVariable("USER") ?? ""}");
        builder.AppendLine($"ran_as={(usedSudo ? "sudo" : "user")}");
        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!string.IsNullOrEmpty(sudoUser))
            builder.AppendLine($"sudo_user={sudoUser}");
        builder.AppendLine($"pre_opencode_hash={preHash ?? "n/a"}");
        builder.AppendLine($"post_opencode_hash={postHash ?? "n/a"}");
        builder.AppendLine("files:");
        foreach (var file in modifiedFiles.Distinct())
            builder.AppendLine($"- {file}");

        File.WriteAllText(logPath, builder.ToString());
    }

    void RotateOldLogs()
    {
        try
        {
            var cutoff = DateTime.UtcNow.AddDays(-30);
            var dirs = Directory.GetDirectories(_backupRoot);
            foreach (var dir in dirs)
            {
                if (!DateTime.TryParseExact(Path.GetFileName(dir), "yyyyMMddHHmmss", null, System.Globalization.DateTimeStyles.None, out var parsed))
                    continue;
                if (parsed < cutoff)
                    Directory.Delete(dir, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }
}
