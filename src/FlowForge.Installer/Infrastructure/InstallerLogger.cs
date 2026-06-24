namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Logger estructurado de instalación. Escribe a ~/.engram/install.log.
/// Formato: [YYYY-MM-DD HH:MM:SS] [LEVEL] mensaje
/// </summary>
public sealed class InstallerLogger
{
    readonly string _logFile;

    public InstallerLogger(string logFile)
    {
        _logFile = logFile;
        Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);
    }

    public static InstallerLogger Default => new(PathHelper.LogFile);

    public void Info(string message)  => Write("INFO",  message);
    public void Warn(string message)  => Write("WARN",  message);
    public void Error(string message) => Write("ERROR", message);

    void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
        try
        {
            File.AppendAllText(_logFile, line + Environment.NewLine);
        }
        catch
        {
            // Log write never throws — silent fail
        }
    }
}
