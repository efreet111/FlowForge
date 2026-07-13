using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace FlowForge.Installer.Modules.OpenCode;

public sealed class AtomicWriter
{
    public void Write(string path, string content, bool allowSymlink = false)
    {
        var target = new FileInfo(path);
        if (target.Exists && IsSymlink(target))
        {
            if (!allowSymlink)
                throw new SymlinkTargetException(path);
        }

        var tmpPath = $"{path}.tmp";
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path) ?? string.Empty);
        File.WriteAllText(tmpPath, content);

        File.Move(tmpPath, path, overwrite: true);

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);

        var sudoUser = Environment.GetEnvironmentVariable("SUDO_USER");
        if (!OperatingSystem.IsWindows() && !string.IsNullOrWhiteSpace(sudoUser))
            TryChown(path, sudoUser);
    }

    static bool IsSymlink(FileInfo info)
    {
        return info.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    static void TryChown(string path, string user)
    {
        try
        {
            var psi = new ProcessStartInfo("chown")
            {
                ArgumentList = { $"{user}:{user}", path },
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using var proc = Process.Start(psi);
            proc?.WaitForExit();
        }
        catch
        {
            // best effort
        }
    }
}

public sealed class SymlinkTargetException : Exception
{
    public SymlinkTargetException(string path)
        : base($"Symlink detected at {path}. Use --allow-symlink to override.")
    {
    }
}
