using System;
using System.IO;
using System.Collections.Generic;

public static class Config
{
    private static Dictionary<string, string> values = new Dictionary<string, string>();

    private static string path =
        "scripts/AmbienceReborn/config/config.ini";

    public static void Load()
    {
        values.Clear();

        if (!File.Exists(path))
        {
            CreateDefault();
        }

        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();

            if (string.IsNullOrWhiteSpace(trimmed)) continue;
            if (trimmed.StartsWith(";")) continue;
            if (trimmed.StartsWith("#")) continue;
            if (trimmed.StartsWith("[")) continue;

            int separator = trimmed.IndexOf('=');
            if (separator <= 0) continue;

            values[trimmed.Substring(0, separator).Trim()] =
                trimmed.Substring(separator + 1).Trim();
        }
    }

    public static string Get(string key, string defaultValue = "")
    {
        if (values.ContainsKey(key))
            return values[key];

        return defaultValue;
    }

    public static bool GetBool(string key, bool defaultValue = false)
    {
        if (values.ContainsKey(key) &&
            bool.TryParse(values[key], out bool result))
            return result;

        return defaultValue;
    }

    public static float GetFloat(string key, float defaultValue = 0f)
    {
        if (values.ContainsKey(key) &&
            float.TryParse(values[key], out float result))
            return result;

        return defaultValue;
    }

    public static int GetInt(string key, int defaultValue = 0)
    {
        if (values.ContainsKey(key) &&
            int.TryParse(values[key], out int result))
            return result;

        return defaultValue;
    }

    private static void CreateDefault()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));

        File.WriteAllText(path,
@"[General]
Enabled=true
EnableEditorBanner=true
EditorBannerDictionary=commonmenu
EditorBannerTexture=interaction_bgd

[Keybinds]
ToggleEditor=F7

[Audio]
MasterVolume=1.0
EnableNightAudio=true
EnableDayAudio=true
MinDelayMs=18000
MaxDelayMs=45000
MaxActiveZones=0

[Zones]
MapRefreshMs=30000
");
    }
}
