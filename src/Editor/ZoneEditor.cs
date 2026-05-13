using GTA;
using GTA.Math;
using LemonUI.Menus;
using System;
using System.Linq;

public class ZoneEditor
{
    public NativeMenu Menu;

    private Zone selectedZone;
    private readonly bool debugEnabled;
    private bool deleteArmed;
    private bool showZoneBlips;
    private bool showAllZoneBlips;
    private bool followPlayerMode;
    private bool routeRecording;
    private int lastFollowSaveTime;
    private int lastRouteSaveTime;
    private Vector3 lastRoutePoint;
    private readonly ZoneBlipManager blipManager = new ZoneBlipManager();

    public ZoneEditor()
    {
        debugEnabled = Config.GetBool("Debug", false);
        showZoneBlips = Config.GetBool("ShowZoneBlipsOnStartup", false);
        Menu = new NativeMenu("AmbienceReborn", debugEnabled ? "Zone Editor + Debug" : "Zone Editor");
        MenuBanner.Apply(Menu);

        var toggleMod = new NativeItem("Toggle Ambience");
        toggleMod.Activated += (s, e) =>
        {
            ModState.Enabled = !ModState.Enabled;
            if (!ModState.Enabled)
                AudioEngine.StopAll(Config.GetInt("FadeMs", 1500));

            UiNotify.Post(
                ModState.Enabled ? "~g~Ambience Enabled" : "~y~Ambience Disabled",
                2500);
        };

        var status = new NativeItem("Show Editor Status");
        status.Activated += (s, e) =>
        {
            string selected = selectedZone == null ? "None" : selectedZone.Name;
            UiNotify.Post(
                $"Zones: {ZoneManager.Zones.Count} | Selected: {selected} | Blips: {showZoneBlips} | All: {showAllZoneBlips} | Debug: {debugEnabled}",
                3500);
        };

        var toggleBlips = new NativeItem("Toggle Zone Blips");
        toggleBlips.Activated += (s, e) =>
        {
            showZoneBlips = !showZoneBlips;

            if (!showZoneBlips)
                blipManager.Clear();
            else
                blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);

            UiNotify.Post(
                showZoneBlips ? "~g~Zone blips shown" : "~y~Zone blips hidden",
                2500);
        };

        var toggleAllBlips = new NativeItem("Toggle All Zone Blips");
        toggleAllBlips.Activated += (s, e) =>
        {
            showAllZoneBlips = !showAllZoneBlips;

            if (showZoneBlips)
                blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);

            UiNotify.Post(
                showAllZoneBlips ? "~y~All zone blips enabled" : "~g~Focused zone blips enabled",
                3000);
        };

        var refreshMap = new NativeItem("Refresh Zone Map");
        refreshMap.Activated += (s, e) =>
        {
            if (!showZoneBlips)
                showZoneBlips = true;

            blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);
            UiNotify.Post("~b~Zone map refreshed", 2000);
        };

        var createZone = new NativeItem("Create Zone Here");
        createZone.Activated += (s, e) =>
        {
            var pos = Game.Player.Character.Position;

            var zone = new Zone
            {
                Name = "Zone_" + ZoneManager.Zones.Count,
                Position = pos,
                Radius = Config.GetFloat("DefaultRadius", 75f),
                Volume = 0.8f,
                MinDelayMs = Config.GetInt("MinDelayMs", 18000),
                MaxDelayMs = Config.GetInt("MaxDelayMs", 45000),
                DaySounds = new string[] { "city/traffic_1.wav" },
                NightSounds = new string[] { "forest/crickets.wav" },
                RandomSounds = new string[0],
                RandomChance = 0f
            };

            ZoneManager.Zones.Add(zone);
            ZoneManager.Save();
            selectedZone = zone;

            UiNotify.Post("~g~Zone Created", 2500);
        };

        var duplicateZone = new NativeItem("Duplicate Selected Here");
        duplicateZone.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            var copy = CloneZone(selectedZone);
            copy.Id = Guid.NewGuid().ToString();
            copy.Name = selectedZone.Name + " Copy";
            copy.Position = Game.Player.Character.Position;

            ZoneManager.Zones.Add(copy);
            selectedZone = copy;
            SaveIfEnabled();

            UiNotify.Post("~g~Zone duplicated at player", 2500);
        };

        var selectZone = new NativeItem("Select Nearest Zone");
        selectZone.Activated += (s, e) =>
        {
            selectedZone = FindNearestZone(Game.Player.Character.Position);
            deleteArmed = false;
            ShowSelectedZone();
        };

        var showCurrent = new NativeItem("Show Current Zone");
        showCurrent.Activated += (s, e) =>
        {
            var zone = ZoneManager.GetZone(Game.Player.Character.Position);
            if (zone == null)
            {
                UiNotify.Post("~y~No ambience zone here", 2500);
                return;
            }

            selectedZone = zone;
            deleteArmed = false;
            ShowZone(zone);
        };

        var moveZone = new NativeItem("Move Selected Here");
        moveZone.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            MoveSelectedToPlayer();
        };

        var toggleFollowPlayer = new NativeItem("Toggle Follow Player");
        toggleFollowPlayer.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            followPlayerMode = !followPlayerMode;
            lastFollowSaveTime = 0;

            if (followPlayerMode)
            {
                MoveSelectedToPlayer(false);
                UiNotify.Post("~b~Selected zone now follows player", 3000);
            }
            else
            {
                SaveIfEnabled();
                UiNotify.Post("~g~Zone placement locked", 2500);
            }
        };

        var startRouteRecording = new NativeItem("Start Route Recording");
        startRouteRecording.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            routeRecording = true;
            followPlayerMode = false;
            EnsureRouteStarted();
            UiNotify.Post("~b~Route recording started", 2500);
        };

        var stopRouteRecording = new NativeItem("Stop Route Recording");
        stopRouteRecording.Activated += (s, e) =>
        {
            routeRecording = false;
            SaveIfEnabled();
            UiNotify.Post("~g~Route recording stopped", 2500);
        };

        var addRoutePoint = new NativeItem("Add Route Point Here");
        addRoutePoint.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            AddRoutePoint(Game.Player.Character.Position, true);
        };

        var clearRoutePoints = new NativeItem("Clear Route Points");
        clearRoutePoints.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            routeRecording = false;
            selectedZone.Points.Clear();
            selectedZone.Position = Game.Player.Character.Position;
            SaveIfEnabled();
            blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);
            UiNotify.Post("~y~Route points cleared", 2500);
        };

        var increaseLeftWidth = new NativeItem("Route Left Side Wider");
        increaseLeftWidth.Activated += (s, e) => AdjustRouteWidth(true, GetRouteWidthStep());

        var decreaseLeftWidth = new NativeItem("Route Left Side Narrower");
        decreaseLeftWidth.Activated += (s, e) => AdjustRouteWidth(true, -GetRouteWidthStep());

        var increaseRightWidth = new NativeItem("Route Right Side Wider");
        increaseRightWidth.Activated += (s, e) => AdjustRouteWidth(false, GetRouteWidthStep());

        var decreaseRightWidth = new NativeItem("Route Right Side Narrower");
        decreaseRightWidth.Activated += (s, e) => AdjustRouteWidth(false, -GetRouteWidthStep());

        var swapRouteSides = new NativeItem("Swap Route Sides");
        swapRouteSides.Activated += (s, e) =>
        {
            if (!RequireRouteSelection()) return;

            float left = selectedZone.GetLeftWidth();
            selectedZone.RouteLeftWidth = selectedZone.GetRightWidth();
            selectedZone.RouteRightWidth = left;
            SaveIfEnabled();
            blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);
            ShowRouteWidths();
        };

        var nudgeNorth = new NativeItem("Nudge North (+Y)");
        nudgeNorth.Activated += (s, e) => NudgeSelected(0f, GetNudgeStep(), 0f);

        var nudgeSouth = new NativeItem("Nudge South (-Y)");
        nudgeSouth.Activated += (s, e) => NudgeSelected(0f, -GetNudgeStep(), 0f);

        var nudgeEast = new NativeItem("Nudge East (+X)");
        nudgeEast.Activated += (s, e) => NudgeSelected(GetNudgeStep(), 0f, 0f);

        var nudgeWest = new NativeItem("Nudge West (-X)");
        nudgeWest.Activated += (s, e) => NudgeSelected(-GetNudgeStep(), 0f, 0f);

        var nudgeUp = new NativeItem("Nudge Up (+Z)");
        nudgeUp.Activated += (s, e) => NudgeSelected(0f, 0f, GetVerticalNudgeStep());

        var nudgeDown = new NativeItem("Nudge Down (-Z)");
        nudgeDown.Activated += (s, e) => NudgeSelected(0f, 0f, -GetVerticalNudgeStep());

        var printPosition = new NativeItem("Show Zone Position");
        printPosition.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            ShowPosition(selectedZone);
        };

        var increaseRadius = new NativeItem("Increase Radius");
        increaseRadius.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.Radius += 25f;
            SaveIfEnabled();
            UiNotify.Post($"Radius: {selectedZone.Radius:0}m", 2000);
        };

        var decreaseRadius = new NativeItem("Decrease Radius");
        decreaseRadius.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.Radius = Math.Max(10f, selectedZone.Radius - 25f);
            SaveIfEnabled();
            UiNotify.Post($"Radius: {selectedZone.Radius:0}m", 2000);
        };

        var increaseVolume = new NativeItem("Increase Volume");
        increaseVolume.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.Volume = Clamp01(selectedZone.Volume + 0.05f);
            SaveIfEnabled();
            UiNotify.Post($"Volume: {selectedZone.Volume:0.00}", 2000);
        };

        var decreaseVolume = new NativeItem("Decrease Volume");
        decreaseVolume.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.Volume = Clamp01(selectedZone.Volume - 0.05f);
            SaveIfEnabled();
            UiNotify.Post($"Volume: {selectedZone.Volume:0.00}", 2000);
        };

        var fasterAmbience = new NativeItem("Ambience More Often");
        fasterAmbience.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.MinDelayMs = Math.Max(1000, selectedZone.MinDelayMs - 5000);
            selectedZone.MaxDelayMs = Math.Max(selectedZone.MinDelayMs, selectedZone.MaxDelayMs - 5000);
            SaveIfEnabled();
            ShowDelay("Ambience", selectedZone.MinDelayMs, selectedZone.MaxDelayMs);
        };

        var slowerAmbience = new NativeItem("Ambience Less Often");
        slowerAmbience.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.MinDelayMs += 5000;
            selectedZone.MaxDelayMs += 5000;
            SaveIfEnabled();
            ShowDelay("Ambience", selectedZone.MinDelayMs, selectedZone.MaxDelayMs);
        };

        var randomMoreOften = new NativeItem("Random Events More Often");
        randomMoreOften.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.RandomMinDelayMs = Math.Max(5000, selectedZone.RandomMinDelayMs - 10000);
            selectedZone.RandomMaxDelayMs = Math.Max(selectedZone.RandomMinDelayMs, selectedZone.RandomMaxDelayMs - 10000);
            SaveIfEnabled();
            ShowDelay("Random", selectedZone.RandomMinDelayMs, selectedZone.RandomMaxDelayMs);
        };

        var randomLessOften = new NativeItem("Random Events Less Often");
        randomLessOften.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.RandomMinDelayMs += 10000;
            selectedZone.RandomMaxDelayMs += 10000;
            SaveIfEnabled();
            ShowDelay("Random", selectedZone.RandomMinDelayMs, selectedZone.RandomMaxDelayMs);
        };

        var increaseRandomChance = new NativeItem("Increase Random Chance");
        increaseRandomChance.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.RandomChance = Clamp01(selectedZone.RandomChance + 0.05f);
            SaveIfEnabled();
            UiNotify.Post($"Random chance: {selectedZone.RandomChance:P0}", 2000);
        };

        var decreaseRandomChance = new NativeItem("Decrease Random Chance");
        decreaseRandomChance.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            selectedZone.RandomChance = Clamp01(selectedZone.RandomChance - 0.05f);
            SaveIfEnabled();
            UiNotify.Post($"Random chance: {selectedZone.RandomChance:P0}", 2000);
        };

        var cityPreset = new NativeItem("Preset: City Ambience");
        cityPreset.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            ApplyPreset(
                "City Ambience",
                2500f,
                0.8f,
                new[] { "city/traffic_1.wav" },
                new[] { "city/traffic_2.wav" },
                new string[0],
                0f);
        };

        var forestPreset = new NativeItem("Preset: Birds + Crickets");
        forestPreset.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            ApplyPreset(
                "Nature Ambience",
                400f,
                0.6f,
                new[] { "forest/birds.wav" },
                new[] { "forest/crickets.wav" },
                new string[0],
                0f);
        };

        var sirenPreset = new NativeItem("Preset: LS Fringe Sirens");
        sirenPreset.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            ApplyPreset(
                "Los Santos Fringe",
                1800f,
                0.55f,
                new[] { "city/traffic_1.wav" },
                new[] { "city/traffic_2.wav" },
                new[] { "sirens/distant_siren.wav" },
                0.55f);
        };

        var trainPreset = new NativeItem("Preset: Blaine Train Approach");
        trainPreset.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            ApplyPreset(
                "Blaine County Rail Approach",
                1600f,
                0.5f,
                new string[0],
                new string[0],
                new[] { "rail/train_horn.wav" },
                0.45f);
        };

        var militaryPreset = new NativeItem("Preset: Fort Zancudo Military");
        militaryPreset.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            ApplyPreset(
                "Fort Zancudo Military Base",
                1850f,
                0.45f,
                new string[0],
                new string[0],
                new[]
                {
                    "military/distant_jet.wav",
                    "military/helicopter_flyby.wav",
                    "military/distant_training_boom.wav"
                },
                0.65f);
        };

        var silentPreset = new NativeItem("Preset: Silent Override");
        silentPreset.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            ApplyPreset(
                "Silent Override",
                850f,
                0.4f,
                new string[0],
                new string[0],
                new string[0],
                0f);
        };

        var deleteZone = new NativeItem("Delete Selected Zone");
        deleteZone.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            if (!deleteArmed)
            {
                deleteArmed = true;
                UiNotify.Post("~y~Press Delete Selected Zone again to confirm", 3000);
                return;
            }

            string name = selectedZone.Name;
            ZoneManager.Zones.Remove(selectedZone);
            selectedZone = null;
            deleteArmed = false;
            SaveIfEnabled();

            UiNotify.Post($"~r~Deleted {name}", 2500);
        };

        var saveZones = new NativeItem("Save Zones");
        saveZones.Activated += (s, e) =>
        {
            ZoneManager.Save();
            UiNotify.Post("~g~Zones saved", 2000);
        };

        var reload = new NativeItem("Reload Config + Zones");
        reload.Activated += (s, e) =>
        {
            AmbienceSystem.Reload();
            selectedZone = null;
            UiNotify.Post("~b~Ambience reloaded", 2500);
        };

        Menu.Add(toggleMod);
        Menu.Add(status);
        Menu.Add(toggleBlips);
        Menu.Add(toggleAllBlips);
        Menu.Add(refreshMap);
        Menu.Add(createZone);
        Menu.Add(duplicateZone);
        Menu.Add(selectZone);
        Menu.Add(showCurrent);
        Menu.Add(moveZone);
        Menu.Add(toggleFollowPlayer);
        Menu.Add(startRouteRecording);
        Menu.Add(stopRouteRecording);
        Menu.Add(addRoutePoint);
        Menu.Add(clearRoutePoints);
        Menu.Add(increaseLeftWidth);
        Menu.Add(decreaseLeftWidth);
        Menu.Add(increaseRightWidth);
        Menu.Add(decreaseRightWidth);
        Menu.Add(swapRouteSides);
        Menu.Add(nudgeNorth);
        Menu.Add(nudgeSouth);
        Menu.Add(nudgeEast);
        Menu.Add(nudgeWest);
        Menu.Add(nudgeUp);
        Menu.Add(nudgeDown);
        Menu.Add(printPosition);
        Menu.Add(increaseRadius);
        Menu.Add(decreaseRadius);
        Menu.Add(increaseVolume);
        Menu.Add(decreaseVolume);
        Menu.Add(fasterAmbience);
        Menu.Add(slowerAmbience);
        Menu.Add(randomMoreOften);
        Menu.Add(randomLessOften);
        Menu.Add(increaseRandomChance);
        Menu.Add(decreaseRandomChance);
        Menu.Add(cityPreset);
        Menu.Add(forestPreset);
        Menu.Add(sirenPreset);
        Menu.Add(trainPreset);
        Menu.Add(militaryPreset);
        Menu.Add(silentPreset);
        Menu.Add(deleteZone);
        Menu.Add(saveZones);
        Menu.Add(reload);

        if (debugEnabled)
        {
            AddDebugItems();
        }
    }

    public void Process()
    {
        Menu?.Process();

        UpdateFollowPlayerMode();
        UpdateRouteRecording();

        if (showZoneBlips)
            blipManager.Update(GetVisibleZone(), showAllZoneBlips);
        else if (blipManager.HasMapContent)
            blipManager.Clear();
    }

    private void AddDebugItems()
    {
        var showDebugInfo = new NativeItem("[Debug] Show Zone Details");
        showDebugInfo.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            UiNotify.Post(
                $"{selectedZone.Id} | Day {Count(selectedZone.DaySounds)} | Night {Count(selectedZone.NightSounds)} | Random {Count(selectedZone.RandomSounds)}",
                4500);
        };

        var testMainSound = new NativeItem("[Debug] Play Zone Ambience");
        testMainSound.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            string sound = selectedZone.DaySounds?.FirstOrDefault()
                ?? selectedZone.NightSounds?.FirstOrDefault();

            PlayDebugSound(sound, selectedZone.Volume);
        };

        var testRandomSound = new NativeItem("[Debug] Play Random Event");
        testRandomSound.Activated += (s, e) =>
        {
            if (!RequireSelection()) return;

            string sound = selectedZone.RandomSounds?.FirstOrDefault();
            PlayDebugSound(sound, selectedZone.RandomVolume);
        };

        var stopSounds = new NativeItem("[Debug] Stop All Sounds");
        stopSounds.Activated += (s, e) =>
        {
            AudioEngine.StopAll();
            UiNotify.Post("~y~Sounds stopped", 2000);
        };

        Menu.Add(showDebugInfo);
        Menu.Add(testMainSound);
        Menu.Add(testRandomSound);
        Menu.Add(stopSounds);
    }

    private Zone FindNearestZone(Vector3 position)
    {
        Zone closest = null;
        float bestDistance = float.MaxValue;

        foreach (var zone in ZoneManager.Zones)
        {
            float distance = Vector3.Distance(position, zone.Position);

            if (distance < bestDistance)
            {
                closest = zone;
                bestDistance = distance;
            }
        }

        return closest;
    }

    private bool RequireSelection()
    {
        if (selectedZone != null)
            return true;

        UiNotify.Post("~y~Select a zone first", 2500);
        return false;
    }

    private void ShowSelectedZone()
    {
        if (selectedZone == null)
        {
            UiNotify.Post("~y~No zones found", 2500);
            return;
        }

        ShowZone(selectedZone);
    }

    private void ShowZone(Zone zone)
    {
        float distance = Vector3.Distance(Game.Player.Character.Position, zone.Position);
        UiNotify.Post(
            $"{zone.Name} | {distance:0}m away | R {zone.Radius:0}m | Points {CountPoints(zone)} | Vol {zone.Volume:0.00}",
            4500);
    }

    private void MoveSelectedToPlayer(bool showMessage = true)
    {
        Vector3 target = Game.Player.Character.Position;
        Vector3 delta = target - selectedZone.GetDisplayPosition();

        MoveSelectedBy(delta);
        SaveIfEnabled();
        blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);

        if (showMessage)
            UiNotify.Post($"~b~Moved {selectedZone.Name} to player", 2500);
    }

    private void NudgeSelected(float x, float y, float z)
    {
        if (!RequireSelection()) return;

        followPlayerMode = false;
        MoveSelectedBy(new Vector3(x, y, z));
        SaveIfEnabled();
        blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);
        ShowPosition(selectedZone);
    }

    private void MoveSelectedBy(Vector3 delta)
    {
        selectedZone.Position += delta;

        if (selectedZone.Points == null)
            return;

        for (int i = 0; i < selectedZone.Points.Count; i++)
        {
            selectedZone.Points[i] += delta;
        }
    }

    private void UpdateFollowPlayerMode()
    {
        if (!followPlayerMode || selectedZone == null)
            return;

        selectedZone.Position = Game.Player.Character.Position;
        blipManager.Update(GetVisibleZone(), showAllZoneBlips);

        if (Game.GameTime - lastFollowSaveTime > 1500)
        {
            SaveIfEnabled();
            lastFollowSaveTime = Game.GameTime;
        }

        if (Menu != null && Menu.Visible)
        {
            GTA.UI.Screen.ShowHelpTextThisFrame(
                $"Moving {selectedZone.Name}: walk/drive to place it, then toggle Follow Player off.");
        }
    }

    private void UpdateRouteRecording()
    {
        if (!routeRecording || selectedZone == null)
            return;

        Vector3 position = Game.Player.Character.Position;
        float spacing = Config.GetFloat("RoutePointSpacing", 45f);

        if (selectedZone.Points == null || selectedZone.Points.Count == 0)
        {
            AddRoutePoint(position, false);
        }
        else if (Vector3.Distance(position, lastRoutePoint) >= spacing)
        {
            AddRoutePoint(position, false);
        }

        if (Game.GameTime - lastRouteSaveTime > 2500)
        {
            SaveIfEnabled();
            lastRouteSaveTime = Game.GameTime;
        }

        if (Menu != null && Menu.Visible)
        {
            GTA.UI.Screen.ShowHelpTextThisFrame(
                $"Recording {selectedZone.Name}: {selectedZone.Points.Count} route points. Stop recording when finished.");
        }
    }

    private void EnsureRouteStarted()
    {
        if (selectedZone.Points == null)
            selectedZone.Points = new System.Collections.Generic.List<Vector3>();

        if (selectedZone.Points.Count == 0)
            AddRoutePoint(Game.Player.Character.Position, false);
        else
            lastRoutePoint = selectedZone.Points[selectedZone.Points.Count - 1];
    }

    private void AddRoutePoint(Vector3 position, bool showMessage)
    {
        if (selectedZone.Points == null)
            selectedZone.Points = new System.Collections.Generic.List<Vector3>();

        selectedZone.Points.Add(position);
        selectedZone.Position = selectedZone.Points[0];
        lastRoutePoint = position;

        SaveIfEnabled();
        if (showMessage)
            blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);
        else
            blipManager.Update(GetVisibleZone(), showAllZoneBlips);

        if (showMessage)
        {
            UiNotify.Post(
                $"~g~Route point added ({selectedZone.Points.Count})",
                2000);
        }
    }

    private void ShowPosition(Zone zone)
    {
        UiNotify.Post(
            $"{zone.Name} position | X {zone.Position.X:0.0} | Y {zone.Position.Y:0.0} | Z {zone.Position.Z:0.0}",
            3500);
    }

    private float GetNudgeStep()
    {
        return Config.GetFloat("MoveStep", 10f);
    }

    private float GetVerticalNudgeStep()
    {
        return Config.GetFloat("VerticalMoveStep", 2f);
    }

    private float GetRouteWidthStep()
    {
        return Config.GetFloat("RouteWidthStep", 25f);
    }

    private bool RequireRouteSelection()
    {
        if (!RequireSelection())
            return false;

        if (selectedZone.Points != null && selectedZone.Points.Count >= 2)
            return true;

        UiNotify.Post("~y~Selected zone needs a recorded route first", 2500);
        return false;
    }

    private void AdjustRouteWidth(bool leftSide, float delta)
    {
        if (!RequireRouteSelection()) return;

        if (selectedZone.RouteLeftWidth <= 0f)
            selectedZone.RouteLeftWidth = selectedZone.Radius;

        if (selectedZone.RouteRightWidth <= 0f)
            selectedZone.RouteRightWidth = selectedZone.Radius;

        if (leftSide)
            selectedZone.RouteLeftWidth = Math.Max(5f, selectedZone.RouteLeftWidth + delta);
        else
            selectedZone.RouteRightWidth = Math.Max(5f, selectedZone.RouteRightWidth + delta);

        SaveIfEnabled();
        blipManager.Refresh(GetVisibleZone(), showAllZoneBlips);
        ShowRouteWidths();
    }

    private void ShowRouteWidths()
    {
        UiNotify.Post(
            $"Route width | Left {selectedZone.GetLeftWidth():0}m | Right {selectedZone.GetRightWidth():0}m",
            3000);
    }

    private void PlayDebugSound(string sound, float volume)
    {
        if (string.IsNullOrEmpty(sound))
        {
            UiNotify.Post("~y~Selected zone has no matching sound", 2500);
            return;
        }

        AudioEngine.Play(Paths.Sound(sound), volume, true);
    }

    private int Count(string[] sounds)
    {
        return sounds == null ? 0 : sounds.Length;
    }

    private void SaveIfEnabled()
    {
        if (Config.GetBool("AutoSave", true))
            ZoneManager.Save();
    }

    private Zone CloneZone(Zone source)
    {
        return new Zone
        {
            Id = source.Id,
            Name = source.Name,
            Position = source.Position,
            Radius = source.Radius,
            RouteLeftWidth = source.RouteLeftWidth,
            RouteRightWidth = source.RouteRightWidth,
            Volume = source.Volume,
            MinDelayMs = source.MinDelayMs,
            MaxDelayMs = source.MaxDelayMs,
            AllowOverlap = source.AllowOverlap,
            DaySounds = Copy(source.DaySounds),
            NightSounds = Copy(source.NightSounds),
            RandomSounds = Copy(source.RandomSounds),
            Points = CopyPoints(source.Points),
            RandomMinDelayMs = source.RandomMinDelayMs,
            RandomMaxDelayMs = source.RandomMaxDelayMs,
            RandomChance = source.RandomChance,
            RandomVolume = source.RandomVolume
        };
    }

    private string[] Copy(string[] sounds)
    {
        if (sounds == null)
            return new string[0];

        return sounds.ToArray();
    }

    private System.Collections.Generic.List<Vector3> CopyPoints(System.Collections.Generic.List<Vector3> points)
    {
        if (points == null)
            return new System.Collections.Generic.List<Vector3>();

        return points.ToList();
    }

    private int CountPoints(Zone zone)
    {
        return zone.Points == null ? 0 : zone.Points.Count;
    }

    private Zone GetVisibleZone()
    {
        if (showAllZoneBlips)
            return selectedZone;

        if (selectedZone != null)
            return selectedZone;

        var player = Game.Player.Character;
        return player == null ? null : ZoneManager.GetZone(player.Position);
    }

    private float Clamp01(float value)
    {
        return Math.Max(0f, Math.Min(1f, value));
    }

    private void ShowDelay(string label, int minMs, int maxMs)
    {
        UiNotify.Post(
            $"{label}: {minMs / 1000}s - {maxMs / 1000}s",
            2500);
    }

    private void ApplyPreset(
        string name,
        float radius,
        float volume,
        string[] daySounds,
        string[] nightSounds,
        string[] randomSounds,
        float randomChance)
    {
        selectedZone.Name = name;
        selectedZone.Radius = radius;
        selectedZone.Volume = volume;
        selectedZone.DaySounds = daySounds;
        selectedZone.NightSounds = nightSounds;
        selectedZone.RandomSounds = randomSounds;
        selectedZone.RandomChance = randomChance;

        if (randomSounds.Length > 0)
        {
            selectedZone.RandomMinDelayMs = 60000;
            selectedZone.RandomMaxDelayMs = 210000;
            selectedZone.RandomVolume = 0.75f;
        }
        else
        {
            selectedZone.RandomChance = 0f;
        }

        SaveIfEnabled();
        UiNotify.Post($"~g~Applied {name} preset", 2500);
    }
}

public static class UiNotify
{
    public static void Post(string message)
    {
        GTA.UI.Notification.PostTicker(message, false, true);
    }

    public static void Post(string message, int durationMs)
    {
        Post(message);
    }
}
