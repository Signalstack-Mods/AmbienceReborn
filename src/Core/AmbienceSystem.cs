using GTA;
using GTA.Chrono;
using System;
using System.Collections.Generic;

public static class AmbienceSystem
{
    private class ZonePlaybackState
    {
        public string LastSound;
        public int NextPlayTime;
    }

    private static bool initialized;
    private static readonly Random random = new Random();
    private static int globalMinDelayMs = 18000;
    private static int globalMaxDelayMs = 45000;
    private static float masterVolume = 1f;
    private static bool enableDayAudio = true;
    private static bool enableNightAudio = true;
    private static bool enableRandomEvents = true;
    private static int fadeMs = 1500;
    private static int maxActiveZones = 2;
    private static bool debug;

    private static readonly Dictionary<string, ZonePlaybackState> zoneStates =
        new Dictionary<string, ZonePlaybackState>();

    private static readonly Dictionary<string, int> nextRandomEventTimes =
        new Dictionary<string, int>();

    public static void Init()
    {
        if (initialized)
            return;

        LoadSettings();
        ZoneManager.Load();

        initialized = true;

    }

    public static void Update()
    {
        if (!ModState.Enabled)
        {
            FadeOutAllAmbience();
            AudioEngine.Update();
            return;
        }

        var player = Game.Player.Character;
        if (player == null)
            return;

        var activeZones = ZoneManager.GetZones(player.Position, maxActiveZones);
        if (activeZones.Count == 0)
        {
            FadeOutAllAmbience();
            AudioEngine.Update();
            return;
        }

        AudioEngine.Update();

        var activeIds = new HashSet<string>();
        bool isNight = GameClock.Hour >= 20 || GameClock.Hour <= 6;

        for (int i = 0; i < activeZones.Count; i++)
        {
            var zone = activeZones[i];
            string zoneId = GetZoneId(zone);
            activeIds.Add(zoneId);

            ZonePlaybackState state = GetState(zoneId, zone);
            UpdateZoneAmbience(zone, zoneId, state, player.Position, isNight);
            UpdateRandomEvents(zone, zoneId, player.Position);
        }

        FadeOutInactiveZones(activeIds);
    }

    public static void Reload()
    {
        LoadSettings();
        ZoneManager.Load();
        zoneStates.Clear();
        nextRandomEventTimes.Clear();
        FadeOutAllAmbience();
    }

    private static void LoadSettings()
    {
        Config.Load();
        AudioEngine.ResetSoundAvailabilityCache();
        ModState.Enabled = Config.GetBool("Enabled", true);
        debug = Config.GetBool("Debug", false);
        masterVolume = Clamp(Config.GetFloat("MasterVolume", 1f), 0f, 1f);
        enableDayAudio = Config.GetBool("EnableDayAudio", true);
        enableNightAudio = Config.GetBool("EnableNightAudio", true);
        enableRandomEvents = Config.GetBool("EnableRandomEvents", true);
        fadeMs = Config.GetInt("FadeMs", fadeMs);
        globalMinDelayMs = Config.GetInt("MinDelayMs", globalMinDelayMs);
        globalMaxDelayMs = Config.GetInt("MaxDelayMs", globalMaxDelayMs);
        maxActiveZones = Math.Max(0, Config.GetInt("MaxActiveZones", 0));
    }

    private static ZonePlaybackState GetState(string zoneId, Zone zone)
    {
        ZonePlaybackState state;
        if (zoneStates.TryGetValue(zoneId, out state))
            return state;

        state = new ZonePlaybackState
        {
            LastSound = null,
            NextPlayTime = Game.GameTime + 250
        };

        zoneStates[zoneId] = state;

        if (debug)
            UiNotify.Post($"~b~Ambience zone: {zone.Name}", 2000);

        return state;
    }

    private static void UpdateZoneAmbience(
        Zone zone,
        string zoneId,
        ZonePlaybackState state,
        GTA.Math.Vector3 playerPosition,
        bool isNight)
    {
        UpdateAmbienceVolume(zone, zoneId, state, playerPosition);

        if (Game.GameTime < state.NextPlayTime)
            return;

        if ((isNight && !enableNightAudio) || (!isNight && !enableDayAudio))
        {
            ScheduleNext(zone, state);
            return;
        }

        string sound = isNight
            ? PickAvailable(zone.NightSounds, state.LastSound)
            : PickAvailable(zone.DaySounds, state.LastSound);

        if (!string.IsNullOrEmpty(sound))
        {
            string path = Paths.Sound(sound);
            float volume = zone.GetDistanceVolume(playerPosition) * masterVolume;

            AudioEngine.Play(path, volume, zone.AllowOverlap, AmbienceChannel(zoneId), fadeMs, true, true);
            state.LastSound = sound;
        }

        ScheduleNext(zone, state);
    }

    private static string PickAvailable(string[] arr, string lastSound)
    {
        if (arr == null || arr.Length == 0)
            return null;

        var available = new List<string>();
        for (int i = 0; i < arr.Length; i++)
        {
            string sound = arr[i];
            if (string.IsNullOrWhiteSpace(sound) ||
                !AudioEngine.IsSoundAvailable(Paths.Sound(sound)))
                continue;

            available.Add(sound);
        }

        if (available.Count == 0)
            return null;

        if (available.Count == 1)
            return available[0];

        string selected;
        int attempts = 0;
        do
        {
            selected = available[random.Next(available.Count)];
            attempts++;
        }
        while (selected == lastSound && attempts < 5);

        return selected;
    }

    private static void ScheduleNext(Zone zone, ZonePlaybackState state)
    {
        int min = zone.MinDelayMs > 0 ? zone.MinDelayMs : globalMinDelayMs;
        int max = zone.MaxDelayMs > 0 ? zone.MaxDelayMs : globalMaxDelayMs;

        if (max < min)
            max = min;

        state.NextPlayTime = Game.GameTime + random.Next(min, max + 1);
    }

    private static void UpdateAmbienceVolume(
        Zone zone,
        string zoneId,
        ZonePlaybackState state,
        GTA.Math.Vector3 playerPosition)
    {
        if (string.IsNullOrEmpty(state.LastSound))
            return;

        float volume = zone.GetDistanceVolume(playerPosition) * masterVolume;
        AudioEngine.SetChannelVolume(AmbienceChannel(zoneId), volume, 250);
    }

    private static void UpdateRandomEvents(Zone zone, string zoneId, GTA.Math.Vector3 playerPosition)
    {
        if (!enableRandomEvents ||
            zone.RandomSounds == null ||
            zone.RandomSounds.Length == 0)
            return;

        if (!nextRandomEventTimes.TryGetValue(zoneId, out int nextTime))
        {
            ScheduleRandomEvent(zone, zoneId);
            return;
        }

        if (Game.GameTime < nextTime)
            return;

        float chance = Clamp(zone.RandomChance, 0f, 1f);
        if (random.NextDouble() <= chance)
        {
            string sound = PickAvailable(zone.RandomSounds, null);
            if (!string.IsNullOrEmpty(sound))
            {
                float volume = zone.GetDistanceVolume(playerPosition) *
                    Clamp(zone.RandomVolume, 0f, 1f) *
                    masterVolume;

                AudioEngine.Play(Paths.Sound(sound), volume, false, "random:" + zoneId, fadeMs, true);

                if (debug)
                    UiNotify.Post($"~b~Random ambience: {sound}", 2000);
            }
        }

        ScheduleRandomEvent(zone, zoneId);
    }

    private static void ScheduleRandomEvent(Zone zone, string zoneId)
    {
        int min = zone.RandomMinDelayMs > 0 ? zone.RandomMinDelayMs : 60000;
        int max = zone.RandomMaxDelayMs > 0 ? zone.RandomMaxDelayMs : 180000;

        if (max < min)
            max = min;

        nextRandomEventTimes[zoneId] = Game.GameTime + random.Next(min, max + 1);
    }

    private static void FadeOutInactiveZones(HashSet<string> activeIds)
    {
        var keys = new List<string>(zoneStates.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            string zoneId = keys[i];
            if (activeIds.Contains(zoneId))
                continue;

            AudioEngine.FadeOutChannel(AmbienceChannel(zoneId), fadeMs);
            zoneStates.Remove(zoneId);
        }
    }

    private static void FadeOutAllAmbience()
    {
        var keys = new List<string>(zoneStates.Keys);
        for (int i = 0; i < keys.Count; i++)
        {
            AudioEngine.FadeOutChannel(AmbienceChannel(keys[i]), fadeMs);
        }

        zoneStates.Clear();
    }

    private static string AmbienceChannel(string zoneId)
    {
        return "ambience:" + zoneId;
    }

    private static string GetZoneId(Zone zone)
    {
        if (!string.IsNullOrWhiteSpace(zone.Id))
            return zone.Id;

        return zone.Name ?? "zone";
    }

    private static float Clamp(float value, float min, float max)
    {
        return Math.Max(min, Math.Min(max, value));
    }
}
