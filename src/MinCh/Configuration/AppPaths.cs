namespace MinCh.Configuration;

public static class AppPaths
{
    // Use a fixed string to avoid accidental name changes if the class is renamed
    public const string AppName = "CAFConsole";
    public static string AppNameLower => AppName.ToLower();

    public static string DataHome =>
        GetAndCreate(Path.Combine(Xdg.Directories.BaseDirectory.DataHome, AppNameLower));
    public static string ConfigHome =>
        GetAndCreate(Path.Combine(Xdg.Directories.BaseDirectory.ConfigHome, AppNameLower));
    public static string StateHome =>
        GetAndCreate(Path.Combine(Xdg.Directories.BaseDirectory.StateHome, AppNameLower));
    public static string CacheHome =>
        GetAndCreate(Path.Combine(Xdg.Directories.BaseDirectory.CacheHome, AppNameLower));

    public static string LogDirectory => GetAndCreate(Path.Combine(StateHome, "logs"));

    private static string GetAndCreate(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
        return path;
    }
}
