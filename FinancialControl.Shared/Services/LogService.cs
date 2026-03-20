namespace FinancialControl.Shared.Services;

public static class LogService
{
    /// <summary>
    /// Registers a log message to a specified file. If the file or its directory does not exist, they will be created automatically.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="logPath"></param>
    public static void Register(string message, string logPath = "logs/execucao.txt")
    {
        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}";
        File.AppendAllText(logPath, line);
    }
}
