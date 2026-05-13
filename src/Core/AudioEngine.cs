using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static class AudioEngine
{
    private class ActiveSound
    {
        public string Alias;
        public string Path;
        public string Channel;
        public int StartedAt;
        public bool Loop;
        public float Volume;
        public float StartVolume;
        public float TargetVolume;
        public int FadeStartedAt;
        public int FadeMs;
        public bool CloseWhenSilent;
    }

    private static readonly List<ActiveSound> activeSounds = new List<ActiveSound>();
    private static readonly HashSet<string> missingSounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> loggedMissingSounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> existingSounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static int nextAliasId;

    [DllImport("winmm.dll", CharSet = CharSet.Auto)]
    private static extern int mciSendString(string command, StringBuilder returnValue, int returnLength, IntPtr winHandle);

    public static bool IsPlaying(string path)
    {
        Update();
        return activeSounds.Exists(sound =>
            string.Equals(sound.Path, path, StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsSoundAvailable(string path, bool logMissing = false)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        if (existingSounds.Contains(path))
            return true;

        if (missingSounds.Contains(path))
        {
            if (logMissing && loggedMissingSounds.Add(path))
                Logger.Warn("Missing sound: " + path);

            return false;
        }

        if (File.Exists(path))
        {
            existingSounds.Add(path);
            return true;
        }

        missingSounds.Add(path);
        if (logMissing && loggedMissingSounds.Add(path))
            Logger.Warn("Missing sound: " + path);

        return false;
    }

    public static void ResetSoundAvailabilityCache()
    {
        existingSounds.Clear();
        missingSounds.Clear();
        loggedMissingSounds.Clear();
    }

    public static void Play(
        string path,
        float volume = 1f,
        bool allowOverlap = false,
        string channel = null,
        int fadeMs = 1000,
        bool crossfade = true,
        bool loop = false)
    {
        try
        {
            if (!IsSoundAvailable(path, true))
            {
                return;
            }

            if (!string.IsNullOrEmpty(channel) && crossfade)
            {
                var current = FindChannelSound(channel, path);
                if (current != null)
                {
                    FadeTo(current, Clamp01(volume), fadeMs, false);
                    return;
                }

                FadeOutChannel(channel, fadeMs);
            }

            if (!allowOverlap && string.IsNullOrEmpty(channel) && IsPlaying(path))
                return;

            string playbackPath = path;
            string alias = "AmbienceReborn_" + nextAliasId++;
            string safePath = playbackPath.Replace("\"", string.Empty);

            int openResult = Mci($"open \"{safePath}\" type {GetMciDeviceType(path)} alias {alias}");
            if (openResult != 0)
                return;

            float clampedVolume = Clamp01(volume);
            SetVolume(alias, crossfade ? 0f : clampedVolume);

            int playResult = Mci(loop ? $"play {alias} repeat" : $"play {alias}");
            if (playResult != 0)
            {
                Mci($"close {alias}");
                return;
            }

            activeSounds.Add(new ActiveSound
            {
                Alias = alias,
                Path = path,
                Channel = channel,
                StartedAt = Environment.TickCount,
                Loop = loop,
                Volume = crossfade ? 0f : clampedVolume,
                StartVolume = crossfade ? 0f : clampedVolume,
                TargetVolume = clampedVolume,
                FadeStartedAt = Environment.TickCount,
                FadeMs = crossfade ? Math.Max(1, fadeMs) : 0,
                CloseWhenSilent = false
            });

            Logger.Info($"Playing sound '{path}' channel='{channel}' volume={clampedVolume:0.00} loop={loop}.");
        }
        catch (Exception ex)
        {
            Logger.Error("AudioEngine.Play failed for " + path, ex);
        }
    }

    public static void Update()
    {
        for (int i = activeSounds.Count - 1; i >= 0; i--)
        {
            var sound = activeSounds[i];
            UpdateFade(sound);

            if (sound.CloseWhenSilent && sound.Volume <= 0.001f)
            {
                Close(sound);
                activeSounds.RemoveAt(i);
                continue;
            }

            string mode = MciQuery($"status {sound.Alias} mode");

            if (mode.IndexOf("stopped", StringComparison.OrdinalIgnoreCase) >= 0 ||
                mode.IndexOf("not ready", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Close(sound);
                activeSounds.RemoveAt(i);
            }
        }
    }

    public static void StopAll()
    {
        foreach (var sound in activeSounds)
        {
            Close(sound);
        }

        activeSounds.Clear();
    }

    public static void StopAll(int fadeMs)
    {
        for (int i = 0; i < activeSounds.Count; i++)
        {
            FadeTo(activeSounds[i], 0f, fadeMs, true);
        }
    }

    public static void FadeOutChannel(string channel, int fadeMs)
    {
        for (int i = 0; i < activeSounds.Count; i++)
        {
            var sound = activeSounds[i];

            if (!string.Equals(sound.Channel, channel, StringComparison.OrdinalIgnoreCase))
                continue;

            FadeTo(sound, 0f, fadeMs, true);
        }
    }

    public static void SetChannelVolume(string channel, float volume, int fadeMs = 250)
    {
        for (int i = 0; i < activeSounds.Count; i++)
        {
            var sound = activeSounds[i];
            if (sound.CloseWhenSilent)
                continue;

            if (!string.Equals(sound.Channel, channel, StringComparison.OrdinalIgnoreCase))
                continue;

            FadeTo(sound, volume, fadeMs, false);
        }
    }

    private static void StopChannel(string channel)
    {
        for (int i = activeSounds.Count - 1; i >= 0; i--)
        {
            var sound = activeSounds[i];

            if (!string.Equals(sound.Channel, channel, StringComparison.OrdinalIgnoreCase))
                continue;

            Close(sound);
            activeSounds.RemoveAt(i);
        }
    }

    private static void Close(ActiveSound sound)
    {
        Mci($"stop {sound.Alias}");
        Mci($"close {sound.Alias}");
    }

    private static ActiveSound FindChannelSound(string channel, string path)
    {
        for (int i = activeSounds.Count - 1; i >= 0; i--)
        {
            var sound = activeSounds[i];
            if (sound.CloseWhenSilent)
                continue;

            if (string.Equals(sound.Channel, channel, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(sound.Path, path, StringComparison.OrdinalIgnoreCase))
                return sound;
        }

        return null;
    }

    private static void FadeTo(ActiveSound sound, float targetVolume, int fadeMs, bool closeWhenSilent)
    {
        targetVolume = Clamp01(targetVolume);
        if (sound.CloseWhenSilent &&
            closeWhenSilent &&
            targetVolume <= 0f &&
            sound.TargetVolume <= 0f)
        {
            return;
        }

        if (sound.CloseWhenSilent == closeWhenSilent &&
            Math.Abs(sound.TargetVolume - targetVolume) <= 0.001f)
        {
            return;
        }

        sound.StartVolume = sound.Volume;
        sound.TargetVolume = targetVolume;
        sound.FadeStartedAt = Environment.TickCount;
        sound.FadeMs = Math.Max(1, fadeMs);
        sound.CloseWhenSilent = closeWhenSilent;
    }

    private static void UpdateFade(ActiveSound sound)
    {
        if (sound.FadeMs <= 0)
            return;

        float t = (Environment.TickCount - sound.FadeStartedAt) / (float)sound.FadeMs;
        if (t >= 1f)
        {
            sound.Volume = sound.TargetVolume;
            sound.FadeMs = 0;
        }
        else
        {
            t = Math.Max(0f, Math.Min(1f, t));
            sound.Volume = sound.StartVolume + ((sound.TargetVolume - sound.StartVolume) * t);
        }

        SetVolume(sound.Alias, sound.Volume);
    }

    private static void SetVolume(string alias, float volume)
    {
        int mciVolume = Math.Max(0, Math.Min(1000, (int)Math.Round(Clamp01(volume) * 1000f)));
        Mci($"setaudio {alias} volume to {mciVolume}", false);
    }

    private static string GetMciDeviceType(string path)
    {
        string extension = Path.GetExtension(path);
        if (string.Equals(extension, ".mp3", StringComparison.OrdinalIgnoreCase))
            return "mpegvideo";

        return "waveaudio";
    }

    private static string GetVolumeAdjustedPath(string path, float volume)
    {
        float clamped = Clamp01(volume);
        if (clamped >= 0.99f)
            return path;

        try
        {
            Directory.CreateDirectory(Paths.Cache);

            int bucket = Math.Max(0, Math.Min(100, (int)Math.Round(clamped * 100f)));
            uint id = unchecked((uint)path.ToLowerInvariant().GetHashCode());
            string fileName = $"{Path.GetFileNameWithoutExtension(path)}_{id:X8}_{File.GetLastWriteTimeUtc(path).Ticks}_{bucket}.wav";
            string cachedPath = Path.Combine(Paths.Cache, fileName);

            if (File.Exists(cachedPath))
                return cachedPath;

            return TryWriteScaledWave(path, cachedPath, clamped)
                ? cachedPath
                : path;
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to create volume-adjusted WAV cache for " + path, ex);
            return path;
        }
    }

    private static float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }

    private static bool TryWriteScaledWave(string sourcePath, string outputPath, float volume)
    {
        byte[] data = File.ReadAllBytes(sourcePath);
        if (data.Length < 44 || Encoding.ASCII.GetString(data, 0, 4) != "RIFF")
            return false;

        int formatOffset = FindChunk(data, "fmt ");
        int dataOffset = FindChunk(data, "data");

        if (formatOffset < 0 || dataOffset < 0)
            return false;

        int formatSize = ReadInt32(data, formatOffset + 4);
        int sampleFormat = ReadInt16(data, formatOffset + 8);
        int bitsPerSample = ReadInt16(data, formatOffset + 22);
        int soundDataStart = dataOffset + 8;
        int soundDataLength = ReadInt32(data, dataOffset + 4);
        int soundDataEnd = Math.Min(data.Length, soundDataStart + soundDataLength);

        if (formatSize < 16 || soundDataStart >= data.Length)
            return false;

        byte[] copy = new byte[data.Length];
        Buffer.BlockCopy(data, 0, copy, 0, data.Length);

        if (sampleFormat == 1)
            ScalePcm(copy, soundDataStart, soundDataEnd, bitsPerSample, volume);
        else if (sampleFormat == 3 && bitsPerSample == 32)
            ScaleFloat(copy, soundDataStart, soundDataEnd, volume);
        else
            return false;

        File.WriteAllBytes(outputPath, copy);
        return true;
    }

    private static void ScalePcm(byte[] data, int start, int end, int bitsPerSample, float volume)
    {
        if (bitsPerSample == 8)
        {
            for (int i = start; i < end; i++)
            {
                int centered = data[i] - 128;
                data[i] = (byte)Math.Max(0, Math.Min(255, 128 + (int)(centered * volume)));
            }
        }
        else if (bitsPerSample == 16)
        {
            for (int i = start; i + 1 < end; i += 2)
            {
                short sample = BitConverter.ToInt16(data, i);
                WriteInt16(data, i, (short)Math.Max(short.MinValue, Math.Min(short.MaxValue, sample * volume)));
            }
        }
        else if (bitsPerSample == 24)
        {
            for (int i = start; i + 2 < end; i += 3)
            {
                int sample = data[i] | (data[i + 1] << 8) | (data[i + 2] << 16);
                if ((sample & 0x800000) != 0)
                    sample |= unchecked((int)0xFF000000);

                int scaled = (int)(sample * volume);
                data[i] = (byte)(scaled & 0xFF);
                data[i + 1] = (byte)((scaled >> 8) & 0xFF);
                data[i + 2] = (byte)((scaled >> 16) & 0xFF);
            }
        }
        else if (bitsPerSample == 32)
        {
            for (int i = start; i + 3 < end; i += 4)
            {
                int sample = BitConverter.ToInt32(data, i);
                WriteInt32(data, i, (int)Math.Max(int.MinValue, Math.Min(int.MaxValue, sample * volume)));
            }
        }
    }

    private static void ScaleFloat(byte[] data, int start, int end, float volume)
    {
        for (int i = start; i + 3 < end; i += 4)
        {
            float sample = BitConverter.ToSingle(data, i);
            byte[] scaled = BitConverter.GetBytes(sample * volume);
            Buffer.BlockCopy(scaled, 0, data, i, 4);
        }
    }

    private static int FindChunk(byte[] data, string id)
    {
        for (int i = 12; i + 8 < data.Length;)
        {
            string chunkId = Encoding.ASCII.GetString(data, i, 4);
            int chunkSize = ReadInt32(data, i + 4);

            if (chunkId == id)
                return i;

            i += 8 + chunkSize + (chunkSize % 2);
        }

        return -1;
    }

    private static short ReadInt16(byte[] data, int offset)
    {
        return BitConverter.ToInt16(data, offset);
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return BitConverter.ToInt32(data, offset);
    }

    private static void WriteInt16(byte[] data, int offset, short value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        data[offset] = bytes[0];
        data[offset + 1] = bytes[1];
    }

    private static void WriteInt32(byte[] data, int offset, int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        Buffer.BlockCopy(bytes, 0, data, offset, 4);
    }

    private static int Mci(string command, bool logFailure = true)
    {
        int result = mciSendString(command, null, 0, IntPtr.Zero);
        if (result != 0 && logFailure)
            Logger.Warn($"MCI returned {result}: {command}");

        return result;
    }

    private static string MciQuery(string command)
    {
        var result = new StringBuilder(128);
        mciSendString(command, result, result.Capacity, IntPtr.Zero);
        return result.ToString();
    }
}
