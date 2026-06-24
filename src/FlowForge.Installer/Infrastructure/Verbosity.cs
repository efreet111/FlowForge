namespace FlowForge.Installer.Infrastructure;

/// <summary>
/// Controla el nivel de detalle en mensajes de error.
/// Por defecto: false (errores limpios sin stack traces).
/// Con --verbose: true (stack trace + exception type + assembly version).
/// </summary>
public static class Verbosity
{
    /// <summary>
    /// Indica si el flag --verbose fue pasado.
    /// </summary>
    public static bool IsVerbose { get; set; }

    /// <summary>
    /// Obtiene el assembly version para mostrar en modo verbose.
    /// </summary>
    public static string AssemblyVersion =>
        typeof(Verbosity).Assembly.GetName().Version?.ToString() ?? "unknown";

    /// <summary>
    /// Formatea un mensaje de error.
/// Si IsVerbose=false: solo el mensaje.
/// Si IsVerbose=true: mensaje + stack trace + exception type + assembly version.
    /// </summary>
    public static string FormatError(string message, Exception? ex = null)
    {
        if (!IsVerbose)
            return message;

        var detail = ex != null
            ? $"{message}\n  Exception: {ex.GetType().Name}\n  Stack: {ex.StackTrace}\n  Assembly: {AssemblyVersion}"
            : $"{message}\n  Assembly: {AssemblyVersion}";

        return detail;
    }
}