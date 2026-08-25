using System;
using System.IO;

namespace PitLife.Tool;

internal static class ToolRoot
{
    /// <summary>
    /// Root del repo (directory che contiene PitLife.sln o .git). Serve sia per la working
    /// directory (ConfigLoader usa path relativi) sia per leggere i sorgenti (BuiltinSpecies.cs).
    /// </summary>
    public static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "PitLife.sln")) ||
                Directory.Exists(Path.Combine(dir.FullName, ".git")))
                return dir.FullName;
            dir = dir.Parent;
        }
        return Directory.GetCurrentDirectory();
    }

    public static void EnsureWorkingDirectory() => Directory.SetCurrentDirectory(RepoRoot());

    public static string ConfigDir => Path.Combine(RepoRoot(), "Content", "config");
}
