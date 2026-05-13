# AmbienceReborn Mod Developer Guide

Use this guide when adding new sound packs, zones, presets, or visual branding.

## Adding Sounds

Put audio files under:

```text
scripts/AmbienceReborn/sounds/
```

Paths inside `ambience_zones.json` are relative to that folder. For example:

```json
"RandomSounds": [
  "birds/cardinal.wav",
  "birds/blue_jay.wav",
  "military/distant_jet.wav"
]
```

Use `.wav` files for best compatibility with the current playback backend. MP3 files are not recommended; convert them to WAV before adding them to zone data. Short one-shot event sounds work well in `RandomSounds`; longer looping beds belong in `DaySounds` or `NightSounds`.

Good folder conventions:

- `city/`: traffic, distant horns, sirens, neighborhood loops.
- `forest/`: loops such as birds, wind, crickets, insects.
- `birds/`: individual bird calls used as random events.
- `military/`: Fort Zancudo jets, flybys, training booms, PA/radio sounds.
- `rail/`: train horns and trackside activity.
- `sirens/`: distant sirens or emergency ambience.

## Adding Or Editing Zones

Zones live in:

```text
scripts/AmbienceReborn/data/ambience_zones.json
```

Each zone can have a daytime loop list, nighttime loop list, and random event list:

```json
{
  "Id": "example-suburb",
  "Name": "Example Suburb",
  "Position": { "X": -625.0, "Y": 250.0, "Z": 85.0 },
  "Radius": 1200.0,
  "Volume": 0.45,
  "MinDelayMs": 30000,
  "MaxDelayMs": 75000,
  "AllowOverlap": false,
  "DaySounds": [ "forest/birds.wav" ],
  "NightSounds": [ "forest/crickets.wav" ],
  "RandomSounds": [
    "birds/mourning_dove.wav",
    "birds/cardinal.wav"
  ],
  "RandomMinDelayMs": 45000,
  "RandomMaxDelayMs": 140000,
  "RandomChance": 0.7,
  "RandomVolume": 0.55
}
```

`DaySounds` and `NightSounds` are looped ambience channels. The plugin keeps them playing continuously and crossfades when the selected sound changes. `RandomSounds` are occasional one-shot sounds controlled by `RandomMinDelayMs`, `RandomMaxDelayMs`, and `RandomChance`.

## Overlap Rules

`MaxActiveZones` controls how many zones can play at once:

```ini
MaxActiveZones=0
```

`0` means unlimited overlap. A positive number limits playback to that many zones. When capped, smaller zones are chosen before larger zones, and ties use nearest distance. This lets small detailed areas win over broad regional zones.

## Recommended Tuning

- Keep main ambience loops quiet and steady: `Volume` around `0.35` to `0.65`.
- Use random events sparingly: `RandomMinDelayMs` of `45000` or higher usually feels natural.
- Avoid putting loud one-shots in `DaySounds` or `NightSounds`; they will loop.
- Use `RandomVolume` lower than main volume for distant events unless the sound should be close.
- Keep `AllowOverlap=false` for most ambience loops. Overlap between zones is handled by separate zone channels.
- Use the in-game editor for positions/radius, then inspect the JSON for final cleanup.

## Adding Presets

Editor presets are defined in:

```text
src/Editor/ZoneEditor.cs
```

Look for existing entries such as `Preset: Fort Zancudo Military`. A preset calls `ApplyPreset(...)` with:

```text
name, radius, volume, day sounds, night sounds, random sounds, random chance
```

After changing presets, rebuild the project so the new menu item is included in `AmbienceReborn.dll`.

## Branding And UI Images

The editor banner is configured with:

```ini
EnableEditorBanner=true
EditorBannerDictionary=commonmenu
EditorBannerTexture=interaction_bgd
```

LemonUI and GTA notifications use texture dictionary/name pairs, not direct PNG paths. To use a custom image, convert/import it into a streamed `.ytd` texture dictionary and set the dictionary and texture names in `config.ini`.

The generated source banner PNGs are kept in:

```text
assets/banner/
```

They are source assets only until packed into a GTA-readable texture dictionary.
