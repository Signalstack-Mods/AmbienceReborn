using GTA;
using GTA.UI;
using System;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

public static class VersionChecker
{
    private const string URL =
        "https://api.github.com/repos/Signalstack-Mods/AmbienceReborn/releases/latest";

    private static bool checkedVersion;

    public static async void Check()
    {
        if (checkedVersion)
            return;

        checkedVersion = true;

        try
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add(
                    "User-Agent",
                    "AmbienceReborn"
                );

                string response =
                    await client.GetStringAsync(URL);

                string latest = GetTagName(response);

                Version current =
                    Assembly.GetExecutingAssembly()
                    .GetName()
                    .Version;

                if (latest != null)
                {
                    Version latestVersion =
                        new Version(
                            latest.Replace("v", "")
                        );

                    if (latestVersion > current)
                    {
                        Notification.PostTicker(
                            "~y~AmbienceReborn update available",
                            false,
                            true
                        );
                    }
                }
            }
        }
        catch
        {
        }
    }

    private static string GetTagName(string response)
    {
        var match = Regex.Match(
            response,
            "\"tag_name\"\\s*:\\s*\"(?<tag>[^\"]+)\"",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Groups["tag"].Value : null;
    }
}
