using System;
using System.IO;

public static class Logger
{
    private static readonly object sync = new object();
    private static readonly string logPath = Paths.Root + "AmbienceReborn.log";

    public static void Init()
    {
        try
        {
            Paths.EnsureDirectories();
            File.WriteAllText(logPath,
                $"[{Timestamp()}] AmbienceReborn log started{Environment.NewLine}");
        }
        catch
        {
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warn(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message, Exception ex = null)
    {
        Write("ERROR", ex == null ? message : message + Environment.NewLine + ex);
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (sync)
            {
                File.AppendAllText(logPath,
                    $"[{Timestamp()}] [{level}] {message}{Environment.NewLine}");
            }
        }
        catch
        {
        }
    }

    private static string Timestamp()
    {
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    }
}
