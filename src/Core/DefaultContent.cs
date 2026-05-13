using GTA.Math;
using System.Collections.Generic;

public static class DefaultContent
{
    private static readonly string[] BirdEventSounds =
    {
        "forest/birds.wav",
        "birds/mourning_dove.wav",
        "birds/chickadee.wav",
        "birds/american_robin.wav",
        "birds/cardinal.wav",
        "birds/blue_jay.wav",
        "birds/woodpecker.wav",
        "birds/sparrow.wav"
    };

    private static readonly string[] MilitaryEventSounds =
    {
        "military/distant_jet.wav",
        "military/helicopter_flyby.wav",
        "military/distant_training_boom.wav"
    };

    public static List<Zone> GetZones()
    {
        return new List<Zone>
        {
            new Zone
            {
                Name = "CITY",
                Position = new Vector3(-200, -900, 30),
                Radius = 2500,
                Volume = 0.8f,
                DaySounds = new[]
                {
                    "city/traffic_1.wav"
                },
                NightSounds = new[]
                {
                    "city/traffic_2.wav"
                },
                RandomSounds = new string[0],
                RandomChance = 0f
            },

            new Zone
            {
                Name = "FOREST",
                Position = new Vector3(1070, -700, 58),
                Radius = 400,
                Volume = 0.6f,
                DaySounds = new[]
                {
                    "forest/birds.wav"
                },
                NightSounds = new[]
                {
                    "forest/crickets.wav"
                },
                RandomSounds = BirdEventSounds,
                RandomMinDelayMs = 45000,
                RandomMaxDelayMs = 140000,
                RandomChance = 0.85f,
                RandomVolume = 0.65f
            },

            new Zone
            {
                Id = "suburban-vinewood-birdlife",
                Name = "Vinewood Suburban Birdlife",
                Position = new Vector3(-625, 250, 85),
                Radius = 1650,
                Volume = 0.45f,
                MinDelayMs = 30000,
                MaxDelayMs = 75000,
                DaySounds = new[]
                {
                    "forest/birds.wav"
                },
                NightSounds = new[]
                {
                    "forest/crickets.wav"
                },
                RandomSounds = BirdEventSounds,
                RandomMinDelayMs = 50000,
                RandomMaxDelayMs = 170000,
                RandomChance = 0.7f,
                RandomVolume = 0.55f
            },

            new Zone
            {
                Id = "ls-fringe-sirens",
                Name = "Los Santos Fringe",
                Position = new Vector3(650, -1350, 35),
                Radius = 1800,
                Volume = 0.55f,
                DaySounds = new[]
                {
                    "city/traffic_1.wav"
                },
                NightSounds = new[]
                {
                    "city/traffic_2.wav"
                },
                RandomSounds = new[]
                {
                    "sirens/distant_siren.wav"
                },
                RandomMinDelayMs = 60000,
                RandomMaxDelayMs = 210000,
                RandomChance = 0.55f,
                RandomVolume = 0.75f
            },

            new Zone
            {
                Id = "blaine-rail-approach",
                Name = "Blaine County Rail Approach",
                Position = new Vector3(2250, 3000, 45),
                Radius = 1600,
                Volume = 0.5f,
                DaySounds = new string[0],
                NightSounds = new string[0],
                RandomSounds = new[]
                {
                    "rail/train_horn.wav"
                },
                RandomMinDelayMs = 90000,
                RandomMaxDelayMs = 300000,
                RandomChance = 0.45f,
                RandomVolume = 0.75f
            },

            new Zone
            {
                Id = "fort-zancudo-military",
                Name = "Fort Zancudo Military Base",
                Position = new Vector3(-2340, 3260, 32),
                Radius = 1850,
                Volume = 0.45f,
                MinDelayMs = 45000,
                MaxDelayMs = 120000,
                DaySounds = new string[0],
                NightSounds = new string[0],
                RandomSounds = MilitaryEventSounds,
                RandomMinDelayMs = 55000,
                RandomMaxDelayMs = 210000,
                RandomChance = 0.65f,
                RandomVolume = 0.55f
            },

            new Zone
            {
                Id = "sandy-shores",
                Name = "Sandy Shores",
                Position = new Vector3(1850, 3700, 35),
                Radius = 850,
                Volume = 0.4f,
                DaySounds = new string[0],
                NightSounds = new string[0],
                RandomSounds = new string[0],
                RandomChance = 0f
            }
        };
    }
}
