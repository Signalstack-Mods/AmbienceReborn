using GTA;
using GTA.Native;
using System.Collections.Generic;

public class ZoneBlipManager
{
    private readonly List<Blip> blips = new List<Blip>();
    private string signature;
    private string selectedZoneId;
    private int lastRefreshTime;
    private bool gpsRouteActive;

    private readonly int[] colors =
    {
        2, 3, 5, 7, 8, 17, 27, 38, 46, 47, 50, 60, 66, 72
    };

    public void Update(Zone selectedZone, bool showAllZones)
    {
        string currentSelectedId = selectedZone?.Id;
        string currentSignature = BuildSignature(showAllZones, selectedZone);
        int refreshMs = Config.GetInt("MapRefreshMs", 30000);
        bool refreshDue = Game.GameTime - lastRefreshTime >= refreshMs;

        if (signature == currentSignature &&
            selectedZoneId == currentSelectedId &&
            (!refreshDue || !HasMapContent))
        {
            return;
        }

        Refresh(selectedZone, showAllZones);
    }

    public bool HasMapContent => blips.Count > 0 || gpsRouteActive;

    public void Refresh(Zone selectedZone, bool showAllZones)
    {
        try
        {
            Logger.Info("Zone map refresh begin.");
            Clear();

            signature = BuildSignature(showAllZones, selectedZone);
            selectedZoneId = selectedZone?.Id;
            lastRefreshTime = Game.GameTime;

            for (int i = 0; i < ZoneManager.Zones.Count; i++)
            {
                var zone = ZoneManager.Zones[i];
                if (zone == null || zone.Radius <= 0f)
                    continue;

                if (!showAllZones && zone != selectedZone)
                    continue;

                AddZoneBlips(zone, selectedZone, i);
            }

            if (Config.GetBool("EnableGpsRoutePreview", false))
                AddGpsRoute(selectedZone);

            Logger.Info($"Zone map refresh end. Blips={blips.Count} Selected='{selectedZone?.Name}' ShowAll={showAllZones}.");
        }
        catch (System.Exception ex)
        {
            Logger.Error("ZoneBlipManager.Refresh failed.", ex);
            ClearSafe();
        }
    }

    public void Clear()
    {
        if (!HasMapContent)
            return;

        try
        {
            ClearSafe();
            if (gpsRouteActive)
            {
                Function.Call(Hash.CLEAR_GPS_MULTI_ROUTE);
                gpsRouteActive = false;
            }
        }
        catch (System.Exception ex)
        {
            Logger.Error("ZoneBlipManager.Clear failed.", ex);
        }
    }

    private void ClearSafe()
    {
        foreach (var blip in blips)
        {
            try
            {
                if (blip != null && blip.Exists())
                    blip.Delete();
            }
            catch (System.Exception ex)
            {
                Logger.Warn("Failed to delete zone blip: " + ex.Message);
            }
        }

        blips.Clear();
        signature = null;
        selectedZoneId = null;
    }

    private void AddZoneBlips(Zone zone, Zone selectedZone, int zoneIndex)
    {
        var points = zone.Points;

        if (points == null || points.Count == 0)
        {
            AddBlip(zone.Position, zone.Radius, zone, selectedZone, zoneIndex);
            return;
        }

        int maxBlips = zone == selectedZone ? 18 : 8;
        int step = System.Math.Max(1, points.Count / maxBlips);

        for (int i = 0; i < points.Count; i += step)
        {
            AddBlip(points[i], zone.GetEffectiveRouteWidth(), zone, selectedZone, zoneIndex);
        }

        if (points.Count > 1 && (points.Count - 1) % step != 0)
            AddBlip(points[points.Count - 1], zone.GetEffectiveRouteWidth(), zone, selectedZone, zoneIndex);
    }

    private void AddBlip(GTA.Math.Vector3 position, float radius, Zone zone, Zone selectedZone, int zoneIndex)
    {
        try
        {
            Blip blip = World.CreateBlip(position, radius);
            blip.Color = (BlipColor)colors[zoneIndex % colors.Length];
            blip.Alpha = zone == selectedZone ? 170 : 95;
            blip.Name = zone.Name ?? "Ambience Zone";

            blips.Add(blip);
        }
        catch (System.Exception ex)
        {
            Logger.Error($"Failed to create zone blip for {zone?.Name} at {position}.", ex);
        }
    }

    private string BuildSignature(bool showAllZones, Zone selectedZone)
    {
        int pointCount = 0;
        int radiusHash = 17;

        foreach (var zone in ZoneManager.Zones)
        {
            if (!showAllZones && zone != selectedZone)
                continue;

            pointCount += zone?.Points?.Count ?? 0;
            if (zone != null)
            {
                radiusHash = (radiusHash * 31) + zone.Radius.GetHashCode();
                radiusHash = (radiusHash * 31) + zone.RouteLeftWidth.GetHashCode();
                radiusHash = (radiusHash * 31) + zone.RouteRightWidth.GetHashCode();
            }
        }

        return showAllZones + ":" + ZoneManager.Zones.Count + ":" + pointCount + ":" + radiusHash;
    }

    private void AddGpsRoute(Zone selectedZone)
    {
        if (selectedZone?.Points == null || selectedZone.Points.Count < 2)
            return;

        int color = colors[ZoneManager.Zones.IndexOf(selectedZone) % colors.Length];

        try
        {
            Logger.Info($"GPS route preview begin for '{selectedZone.Name}' with {selectedZone.Points.Count} points.");
            Function.Call(Hash.CLEAR_GPS_MULTI_ROUTE);
            Function.Call(Hash.START_GPS_MULTI_ROUTE, color, true, true);

            foreach (var point in selectedZone.Points)
            {
                Function.Call(Hash.ADD_POINT_TO_GPS_MULTI_ROUTE, point.X, point.Y, point.Z);
            }

            Function.Call(Hash.SET_GPS_MULTI_ROUTE_RENDER, true);
            gpsRouteActive = true;
            Logger.Info("GPS route preview end.");
        }
        catch (System.Exception ex)
        {
            Logger.Error("Failed to render selected route GPS line.", ex);
        }
    }
}
