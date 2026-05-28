namespace BBDown.Core;

public static class Logger
{
    public static string? LogFilePath { get; set; }

    private static void WriteLine(string line)
    {
        Console.WriteLine(line);
        AppendToFile(line);
    }

    private static void AppendToFile(string line)
    {
        var path = LogFilePath;
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            File.AppendAllText(path, line + Environment.NewLine);
        }
        catch { /* silently ignore file write failures */ }
    }

    public static void Log(object text, bool enter = true)
    {
        var line = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + text;
        Console.Write(line);
        AppendToFile(line);
        if (enter)
        {
            Console.WriteLine();
            AppendToFile(string.Empty);
        }
    }

    public static void LogError(object text)
    {
        var line = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + text;
        Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write(text);
        Console.ResetColor();
        Console.WriteLine();
        AppendToFile(line);
    }

    public static void LogColor(object text, bool time = true)
    {
        string line;
        if (time)
        {
            line = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + text;
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
        }
        else
        {
            line = "                             " + text;
            Console.Write("                            ");
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(text);
        Console.ResetColor();
        Console.WriteLine();
        AppendToFile(line);
    }

    public static void LogWarn(object text, bool time = true)
    {
        string line;
        if (time)
        {
            line = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + text;
            Console.Write(DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - ");
        }
        else
        {
            line = "                             " + text;
            Console.Write("                            ");
        }
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.Write(text);
        Console.ResetColor();
        Console.WriteLine();
        AppendToFile(line);
    }

    public static void LogDebug(string toFormat, params object[] args)
    {
        if (!Config.Current.DebugLog) return;
        string message = args.Length > 0 ? string.Format(toFormat, args).Trim() : toFormat;
        var line = DateTime.Now.ToString("[yyyy-MM-dd HH:mm:ss.fff]") + " - " + message;
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(line);
        Console.ResetColor();
        Console.WriteLine();
        AppendToFile(line);
    }
}
