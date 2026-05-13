using GTA;
using GTA.UI;
using LemonUI.Menus;
using System.Windows.Forms;

public static class KeybindHandler
{
    public static Keys ToggleMenuKey = Keys.F10;
    public static Keys ModifierKey = Keys.LShiftKey;
    private static readonly System.Collections.Generic.Dictionary<Keys, bool> previous =
        new System.Collections.Generic.Dictionary<Keys, bool>();

    public static void LoadConfig()
    {
        ToggleMenuKey = ParseKey(Config.Get("ToggleEditor", "F7"), Keys.F7);
        ModifierKey = ParseKey(Config.Get("Modifier", "ControlKey"), Keys.ControlKey);
    }

    public static string GetEditorKeyLabel()
    {
        return ToggleMenuKey.ToString();
    }

    public static bool WasPressed(Keys key)
    {
        bool pressed = Game.IsKeyPressed(key);
        bool wasPressed = previous.ContainsKey(key) && previous[key];
        previous[key] = pressed;

        return pressed && !wasPressed;
    }

    public static bool ModifierHeld()
    {
        return Game.IsKeyPressed(ModifierKey);
    }

    public static bool EditorTogglePressed()
    {
        return WasPressed(ToggleMenuKey);
    }

    public static void Update(NativeMenu menu)
    {
        if (WasPressed(ToggleMenuKey))
        {
            menu.Visible = !menu.Visible;

            System.Console.WriteLine("[AmbienceReborn] Menu toggled");

            UiNotify.Post("~b~Ambience Menu Toggled", 2500);
        }
    }

    private static Keys ParseKey(string value, Keys fallback)
    {
        if (!string.IsNullOrWhiteSpace(value) &&
            System.Enum.TryParse(value, true, out Keys key))
        {
            return key;
        }

        return fallback;
    }
}
