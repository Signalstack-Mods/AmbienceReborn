# AmbienceReborn - LCPDFR Upload Description

## Short Description

AmbienceReborn adds configurable ambient audio zones to GTA V, bringing more life to Los Santos, Blaine County, forests, suburbs, rail areas, and Fort Zancudo with location-aware ambience, random events, looping sound beds, and an in-game LemonUI zone editor.

## Description

AmbienceReborn is a ScriptHookVDotNet plugin that plays custom ambience based on where the player is, the time of day, and the zone settings you configure.

The plugin supports daytime and nighttime ambience loops, random one-shot events, distance-based volume falloff, crossfading, continuous looping ambience beds, route-based zones, unlimited overlapping zones, and an in-game editor for creating and tuning zones without constantly editing JSON by hand.

Examples of ambience you can create:

- City traffic beds in Los Santos.
- Crickets at night in wooded areas.
- Random bird calls in suburban and forest areas.
- Distant train horns near rail corridors.
- Distant sirens around city outskirts.
- Fort Zancudo military activity such as jet flybys, helicopter flybys, and training booms.

## Features

- Custom ambience zones using JSON.
- Day and night sound lists per zone.
- Random event sound lists per zone.
- Continuous looping ambience beds with crossfading.
- Distance-based volume falloff.
- Unlimited overlapping zones with `MaxActiveZones=0`.
- In-game LemonUI editor.
- Zone radius blips for mapping.
- Route zones for long or irregular areas.
- Configurable editor key.
- Startup notification with version, author, and editor key.
- Optional LemonUI banner texture support.
- Mod developer guide for adding sounds, zones, and presets.

## Requirements

- Grand Theft Auto V.
- ScriptHookV.
- ScriptHookVDotNet 3.
- LemonUI.SHVDN3.dll.

## Installation

Copy the included files into your GTA V `scripts` folder.

Expected structure:

```text
scripts/
  AmbienceReborn.dll
  LemonUI.SHVDN3.dll
  AmbienceReborn/
    config/config.ini
    data/ambience_zones.json
    sounds/
```

Start the game. AmbienceReborn will show a notification when loaded.

## Controls

The editor key is configurable in:

```text
scripts/AmbienceReborn/config/config.ini
```

Default:

```ini
ToggleEditor=F7
```

Use the editor to create zones, move zones, adjust radius and volume, record route zones, refresh zone map blips, and apply ambience presets.

## Adding More Sounds

Add `.wav` files under:

```text
scripts/AmbienceReborn/sounds/
```

Zone sound paths are relative to that folder. Example:

```json
"RandomSounds": [
  "birds/cardinal.wav",
  "military/distant_jet.wav"
]
```

WAV is recommended. MP3 is not recommended for this plugin's playback backend.

See `MOD_DEVELOPERS.md` for more detail on adding custom sounds, zones, presets, and banner textures.

## Configuration Notes

Useful settings in `config.ini`:

- `MasterVolume`: overall volume.
- `FadeMs`: fade and crossfade duration.
- `MaxActiveZones`: maximum active overlapping zones. Use `0` for unlimited.
- `EnableRandomEvents`: toggles random zone events.
- `ShowZoneBlipsOnStartup`: toggles map blips on startup.
- `EnableGpsRoutePreview`: toggles GPS-style route preview.
- `MapRefreshMs`: throttles minimap overlay rebuilds.
- `EnableEditorBanner`: enables the LemonUI editor banner.

## Known Notes

- Use WAV files for best compatibility.
- Very large numbers of overlapping zones can increase audio load. Use a positive `MaxActiveZones` value if needed.
- GPS route preview uses GTA map natives. If you have crashes while mapping, disable `EnableGpsRoutePreview`.
- Custom PNG/JPG images cannot be used directly as GTA notification or LemonUI banner textures. They must be packed into a GTA texture dictionary (`.ytd`) first.

## Credits

Created by Signalstack Mods.

Built with:

- ScriptHookVDotNet.
- LemonUI.

Thanks to the GTA V modding community for documentation, tools, and testing knowledge.

## License / Permissions

AmbienceReborn is provided for personal, non-commercial GTA V modding use.

Do not reupload, redistribute, mirror, sell, or include this plugin in another mod pack without permission.

If you add or redistribute sound assets, make sure you have the right to use and distribute those sounds.

See `LICENSE.md` for the full license notice.

