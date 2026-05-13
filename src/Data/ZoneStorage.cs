using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
using GTA.Math;

public static class ZoneStorage
{
    private class ZoneDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public PositionDto Position { get; set; }
        public List<PositionDto> Points { get; set; }
        public float Radius { get; set; }
        public float RouteLeftWidth { get; set; }
        public float RouteRightWidth { get; set; }
        public float Volume { get; set; }
        public int MinDelayMs { get; set; }
        public int MaxDelayMs { get; set; }
        public bool AllowOverlap { get; set; }
        public string[] DaySounds { get; set; }
        public string[] NightSounds { get; set; }
        public string[] RandomSounds { get; set; }
        public int RandomMinDelayMs { get; set; }
        public int RandomMaxDelayMs { get; set; }
        public float RandomChance { get; set; }
        public float RandomVolume { get; set; }
    }

    private class PositionDto
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    }

    public static List<Zone> Load()
    {
        if (!File.Exists(Paths.ZoneFile))
            return DefaultContent.GetZones();

        try
        {
            var serializer = new JavaScriptSerializer();
            var zones = serializer.Deserialize<List<ZoneDto>>(
                File.ReadAllText(Paths.ZoneFile));

            return zones != null && zones.Count > 0
                ? ToZones(zones)
                : DefaultContent.GetZones();
        }
        catch
        {
            return DefaultContent.GetZones();
        }
    }

    public static void Save(List<Zone> zones)
    {
        Directory.CreateDirectory(Paths.Data);

        var serializer = new JavaScriptSerializer();
        var json = serializer.Serialize(ToDtos(zones));

        File.WriteAllText(Paths.ZoneFile,
            PrettyJson(json));
    }

    private static List<Zone> ToZones(List<ZoneDto> dtos)
    {
        var zones = new List<Zone>();

        foreach (var dto in dtos)
        {
            if (dto == null)
                continue;

            zones.Add(new Zone
            {
                Id = string.IsNullOrWhiteSpace(dto.Id) ? System.Guid.NewGuid().ToString() : dto.Id,
                Name = dto.Name,
                Position = new Vector3(
                    dto.Position?.X ?? 0f,
                    dto.Position?.Y ?? 0f,
                    dto.Position?.Z ?? 0f),
                Points = ToPoints(dto.Points),
                Radius = dto.Radius <= 0f ? 50f : dto.Radius,
                RouteLeftWidth = dto.RouteLeftWidth,
                RouteRightWidth = dto.RouteRightWidth,
                Volume = dto.Volume <= 0f ? 1f : dto.Volume,
                MinDelayMs = dto.MinDelayMs,
                MaxDelayMs = dto.MaxDelayMs,
                AllowOverlap = dto.AllowOverlap,
                DaySounds = dto.DaySounds ?? new string[0],
                NightSounds = dto.NightSounds ?? new string[0],
                RandomSounds = dto.RandomSounds ?? new string[0],
                RandomMinDelayMs = dto.RandomMinDelayMs,
                RandomMaxDelayMs = dto.RandomMaxDelayMs,
                RandomChance = dto.RandomChance,
                RandomVolume = dto.RandomVolume <= 0f ? 1f : dto.RandomVolume
            });
        }

        return zones;
    }

    private static List<ZoneDto> ToDtos(List<Zone> zones)
    {
        var dtos = new List<ZoneDto>();

        foreach (var zone in zones)
        {
            dtos.Add(new ZoneDto
            {
                Id = zone.Id,
                Name = zone.Name,
                Position = new PositionDto
                {
                    X = zone.Position.X,
                    Y = zone.Position.Y,
                    Z = zone.Position.Z
                },
                Points = ToPointDtos(zone.Points),
                Radius = zone.Radius,
                RouteLeftWidth = zone.RouteLeftWidth,
                RouteRightWidth = zone.RouteRightWidth,
                Volume = zone.Volume,
                MinDelayMs = zone.MinDelayMs,
                MaxDelayMs = zone.MaxDelayMs,
                AllowOverlap = zone.AllowOverlap,
                DaySounds = zone.DaySounds,
                NightSounds = zone.NightSounds,
                RandomSounds = zone.RandomSounds,
                RandomMinDelayMs = zone.RandomMinDelayMs,
                RandomMaxDelayMs = zone.RandomMaxDelayMs,
                RandomChance = zone.RandomChance,
                RandomVolume = zone.RandomVolume
            });
        }

        return dtos;
    }

    private static List<Vector3> ToPoints(List<PositionDto> points)
    {
        var result = new List<Vector3>();

        if (points == null)
            return result;

        foreach (var point in points)
        {
            if (point == null)
                continue;

            result.Add(new Vector3(point.X, point.Y, point.Z));
        }

        return result;
    }

    private static List<PositionDto> ToPointDtos(List<Vector3> points)
    {
        var result = new List<PositionDto>();

        if (points == null)
            return result;

        foreach (var point in points)
        {
            result.Add(new PositionDto
            {
                X = point.X,
                Y = point.Y,
                Z = point.Z
            });
        }

        return result;
    }

    private static string PrettyJson(string json)
    {
        int indent = 0;
        bool quoted = false;
        var result = new System.Text.StringBuilder();

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];

            if (ch == '"' && (i == 0 || json[i - 1] != '\\'))
                quoted = !quoted;

            if (!quoted && (ch == '{' || ch == '['))
            {
                result.Append(ch);
                result.AppendLine();
                result.Append(new string(' ', ++indent * 2));
            }
            else if (!quoted && (ch == '}' || ch == ']'))
            {
                result.AppendLine();
                result.Append(new string(' ', --indent * 2));
                result.Append(ch);
            }
            else if (!quoted && ch == ',')
            {
                result.Append(ch);
                result.AppendLine();
                result.Append(new string(' ', indent * 2));
            }
            else if (!quoted && ch == ':')
            {
                result.Append(": ");
            }
            else
            {
                result.Append(ch);
            }
        }

        return result.ToString();
    }
}
