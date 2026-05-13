using System.IO;

public static class Paths
{
    public const string Root = "scripts/AmbienceReborn/";
    public const string Config = Root + "config/";
    public const string Data = Root + "data/";
    public const string Sounds = Root + "sounds/";
    public const string Cache = Root + "cache/";

    public const string ConfigFile = Config + "config.ini";
    public const string ZoneFile = Data + "ambience_zones.json";

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(Config);
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(Sounds);
        Directory.CreateDirectory(Cache);
    }

    public static string Sound(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return string.Empty;

        if (Path.IsPathRooted(relativePath))
            return relativePath;

        return Path.Combine(Sounds, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }
}
