using GTA.Math;
using System.Collections.Generic;

public class AudioZone
{
    public string Name;

    public Vector3 Position;
    public float Radius;
    public float Volume;

    public List<string> DaySounds = new();
    public List<string> NightSounds = new();
}