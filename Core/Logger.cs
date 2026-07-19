using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PitLife.Core;

public static class Logger
{
    private static readonly string LogDir = Path.Combine("logs");
    private static readonly string LogFile = Path.Combine(LogDir, $"pitlife_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    private static readonly object Lock = new();
    private static readonly List<string> Buffer = [];
    private const int BufferSize = 100;
    private static readonly List<string> _recentEvents = [];
    private const int MaxRecentEvents = 50;

    public static IReadOnlyList<string> RecentEvents
    {
        get { lock (Lock) return _recentEvents.ToArray(); }
    }

    static Logger()
    {
        Directory.CreateDirectory(LogDir);
        RotateOldLogs();
        Write("INFO", "Logger initialized");
    }

    private static void RotateOldLogs()
    {
        try
        {
            var files = Directory.GetFiles(LogDir, "pitlife_*.log");
            Array.Sort(files, StringComparer.Ordinal);
            Array.Reverse(files);
            for (var i = 5; i < files.Length; i++)
                File.Delete(files[i]);
        }
        catch (Exception ex) { Console.Error.WriteLine($"Log rotation failed: {ex.Message}"); }
    }

    public static void Info(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Write("INFO", message);
    }
    public static void Debug(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Write("DEBUG", message);
    }
    public static void Warn(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Write("WARN", message);
    }
    public static void Error(string message)
    {
        ArgumentNullException.ThrowIfNull(message);
        Write("ERROR", message);
    }
    public static void Event(string category, string message)
    {
        ArgumentNullException.ThrowIfNull(category);
        ArgumentNullException.ThrowIfNull(message);
        Write($"EVT.{category}", message);
        lock (Lock)
        {
            _recentEvents.Add($"[{category}] {message}");
            if (_recentEvents.Count > MaxRecentEvents)
                _recentEvents.RemoveAt(0);
        }
    }

    public static void Flush()
    {
        lock (Lock)
        {
            if (Buffer.Count == 0) return;
            try { File.AppendAllLines(LogFile, Buffer); }
            catch (Exception ex) { Console.Error.WriteLine($"Log flush failed: {ex.Message}"); }
            Buffer.Clear();
        }
    }

    private static void Write(string level, string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        lock (Lock)
        {
            Buffer.Add(line);
            if (Buffer.Count >= BufferSize) Flush();
        }
    }
}
