using GTA;
using GTA.Math;
using System.Collections.Generic;

public static class ZoneManager
{
    public static List<Zone> Zones = new List<Zone>();
    public static bool Loaded;

    public static void Load()
    {
        Paths.EnsureDirectories();
        Zones = ZoneStorage.Load();
        Loaded = true;
    }

    public static void Save()
    {
        ZoneStorage.Save(Zones);
    }

    public static Zone GetZone(Vector3 pos)
    {
        var zones = GetZones(pos, 1);
        return zones.Count == 0 ? null : zones[0];
    }

    public static List<Zone> GetZones(Vector3 pos, int maxZones)
    {
        var matches = new List<Zone>();
        var radii = new List<float>();
        var distances = new List<float>();

        if (!Loaded || Zones == null || Zones.Count == 0)
            return matches;

        for (int i = 0; i < Zones.Count; i++)
        {
            var zone = Zones[i];
            if (zone == null || !zone.Contains(pos))
                continue;

            float distance = zone.GetDistance(pos);
            int insertAt = matches.Count;

            for (int j = 0; j < matches.Count; j++)
            {
                if (zone.Radius < radii[j] ||
                    (zone.Radius == radii[j] && distance < distances[j]))
                {
                    insertAt = j;
                    break;
                }
            }

            matches.Insert(insertAt, zone);
            radii.Insert(insertAt, zone.Radius);
            distances.Insert(insertAt, distance);

            if (maxZones > 0 && matches.Count > maxZones)
            {
                int removeAt = matches.Count - 1;
                matches.RemoveAt(removeAt);
                radii.RemoveAt(removeAt);
                distances.RemoveAt(removeAt);
            }
        }

        return matches;
    }

    public static List<Zone> GetZones(Vector3 pos)
    {
        if (!Loaded || Zones == null)
            return new List<Zone>();

        return GetZones(pos, Zones.Count);
    }
}
