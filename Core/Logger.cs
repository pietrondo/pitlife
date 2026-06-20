using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace PitLife.Core;

public static class Logger
{
    private static readonly string LogDir = Path.Combine("logs");
    private static readonly string LogFile = Path.Combine(LogDir, $"pitlife_{DateTime.Now:yyyyMMdd_HHmmss}.log");
    private static readonly object Lock = new();
    private static readonly List<string> Buffer = [];
    private const int BufferSize = 50;

    static Logger()
    {
        Directory.CreateDirectory(LogDir);
        Write("INFO", "Logger initialized");
    }

    public static void Info(string message) => Write("INFO", message);
    public static void Debug(string message) => Write("DEBUG", message);
    public static void Warn(string message) => Write("WARN", message);
    public static void Error(string message) => Write("ERROR", message);
    public static void Event(string category, string message) => Write($"EVT.{category}", message);

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
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
        lock (Lock)
        {
            Buffer.Add(line);
            if (Buffer.Count >= BufferSize) Flush();
        }
    }
}
