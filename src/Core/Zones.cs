using GTA;
using GTA.Math;
using System.Collections.Generic;

public class Zone
{
    public string Id = System.Guid.NewGuid().ToString();
    public string Name;
    public Vector3 Position;
    public List<Vector3> Points = new List<Vector3>();
    public float Radius = 50f;
    public float RouteLeftWidth;
    public float RouteRightWidth;

    public float Volume = 1f;
    public int MinDelayMs = 18000;
    public int MaxDelayMs = 45000;
    public bool AllowOverlap = false;

    public string[] DaySounds;
    public string[] NightSounds;
    public string[] RandomSounds;
    public int RandomMinDelayMs = 60000;
    public int RandomMaxDelayMs = 180000;
    public float RandomChance = 0.45f;
    public float RandomVolume = 1f;

    public bool Contains(Vector3 pos)
    {
        var sample = GetRouteSample(pos);
        return sample.Distance <= sample.Width;
    }

    public float GetDistanceVolume(Vector3 pos)
    {
        var sample = GetRouteSample(pos);

        if (sample.Width <= 0f)
            return Volume;

        float edgeFade = 1f - (sample.Distance / sample.Width);
        float scaled = 0.25f + (0.75f * edgeFade);

        return System.Math.Max(0f, System.Math.Min(1f, Volume * scaled));
    }

    public float GetDistance(Vector3 pos)
    {
        if (Points == null || Points.Count == 0)
            return Vector3.Distance(pos, Position);

        if (Points.Count == 1)
            return Vector3.Distance(pos, Points[0]);

        float best = float.MaxValue;

        for (int i = 0; i < Points.Count - 1; i++)
        {
            float distance = DistanceToSegment(pos, Points[i], Points[i + 1]);
            if (distance < best)
                best = distance;
        }

        return best;
    }

    public float GetEffectiveRouteWidth()
    {
        return System.Math.Max(GetLeftWidth(), GetRightWidth());
    }

    public float GetLeftWidth()
    {
        return RouteLeftWidth > 0f ? RouteLeftWidth : Radius;
    }

    public float GetRightWidth()
    {
        return RouteRightWidth > 0f ? RouteRightWidth : Radius;
    }

    public Vector3 GetDisplayPosition()
    {
        if (Points != null && Points.Count > 0)
            return Points[0];

        return Position;
    }

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = Dot(segment, segment);

        if (lengthSquared <= 0.001f)
            return Vector3.Distance(point, start);

        float t = Dot(point - start, segment) / lengthSquared;
        t = System.Math.Max(0f, System.Math.Min(1f, t));

        Vector3 projection = start + (segment * t);
        return Vector3.Distance(point, projection);
    }

    private static float Dot(Vector3 a, Vector3 b)
    {
        return (a.X * b.X) + (a.Y * b.Y) + (a.Z * b.Z);
    }

    private RouteSample GetRouteSample(Vector3 pos)
    {
        if (Points == null || Points.Count < 2)
        {
            return new RouteSample
            {
                Distance = GetDistance(pos),
                Width = Radius
            };
        }

        RouteSample best = new RouteSample
        {
            Distance = float.MaxValue,
            Width = Radius
        };

        for (int i = 0; i < Points.Count - 1; i++)
        {
            RouteSample sample = GetSegmentSample(pos, Points[i], Points[i + 1]);
            if (sample.Distance < best.Distance)
                best = sample;
        }

        return best;
    }

    private RouteSample GetSegmentSample(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = Dot(segment, segment);

        if (lengthSquared <= 0.001f)
        {
            return new RouteSample
            {
                Distance = Vector3.Distance(point, start),
                Width = GetEffectiveRouteWidth()
            };
        }

        float t = Dot(point - start, segment) / lengthSquared;
        t = System.Math.Max(0f, System.Math.Min(1f, t));

        Vector3 projection = start + (segment * t);
        float distance = Vector3.Distance(point, projection);
        float side = ((segment.X * (point.Y - start.Y)) - (segment.Y * (point.X - start.X)));

        return new RouteSample
        {
            Distance = distance,
            Width = side >= 0f ? GetLeftWidth() : GetRightWidth()
        };
    }

    private struct RouteSample
    {
        public float Distance;
        public float Width;
    }
}
