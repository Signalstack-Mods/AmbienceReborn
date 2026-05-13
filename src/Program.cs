using GTA;
using System.Reflection;

public class Program : Script
{
    ZoneEditor editor;
    private int tickCount;

    public Program()
    {
        Logger.Init();
        Logger.Info("Script constructor started.");

        try
        {
            AmbienceSystem.Init();
            KeybindHandler.LoadConfig();

            editor = new ZoneEditor();

            Tick += OnTick;

            System.Console.WriteLine("[AmbienceReborn] Loaded");
            Logger.Info("Script loaded.");
            ShowLoadedNotification();
        }
        catch (System.Exception ex)
        {
            Logger.Error("Fatal error during script startup.", ex);
            throw;
        }
    }

    private void OnTick(object sender, System.EventArgs e)
    {
        tickCount++;

        try
        {
            AmbienceSystem.Update();
        }
        catch (System.Exception ex)
        {
            Logger.Error("AmbienceSystem.Update failed on tick " + tickCount + ".", ex);
        }

        try
        {
            editor?.Process();
        }
        catch (System.Exception ex)
        {
            Logger.Error("ZoneEditor.Process failed on tick " + tickCount + ".", ex);
        }

        try
        {
            if (editor?.Menu != null)
                KeybindHandler.Update(editor.Menu);
        }
        catch (System.Exception ex)
        {
            Logger.Error("KeybindHandler.Update failed on tick " + tickCount + ".", ex);
        }
    }

    private void ShowLoadedNotification()
    {
        var assembly = Assembly.GetExecutingAssembly();
        string version = assembly.GetName().Version?.ToString() ?? "1.0.0";
        string author = "Signalstack Mods";
        var authorAttribute = assembly.GetCustomAttribute<AssemblyCompanyAttribute>();
        if (authorAttribute != null && !string.IsNullOrWhiteSpace(authorAttribute.Company))
            author = authorAttribute.Company;

        GTA.UI.Notification.PostMessageText(
            $"~g~Ambient zones online~s~~n~Editor: ~b~{KeybindHandler.GetEditorKeyLabel()}~s~~n~Version: {version}~n~Made by {author}",
            new GTA.Graphics.TextureAsset("CHAR_LS_TOURIST_BOARD", "CHAR_LS_TOURIST_BOARD"),
            false,
            GTA.UI.FeedTextIcon.Message,
            "AmbienceReborn",
            "Plugin loaded");
    }
}
