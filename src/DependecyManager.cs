using System;
using GTA;
using GTA.UI;

public static class DependencyManager
{
    public static void Check()
    {
        bool lemon = Has("LemonUI");

        if (!lemon)
        {
            ModState.Enabled = false;

            Notification.PostTicker(
                "~r~AmbienceReborn disabled",
                true,
                true
            );

            if (!lemon)
            {
                Notification.PostTicker(
                    "~r~Missing LemonUI.SHVDN3.dll",
                    false,
                    true
                );
            }

            return;
        }

        Notification.PostTicker(
            "~g~AmbienceReborn loaded",
            false,
            true
        );
    }

    private static bool Has(string name)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.GetName().Name.Contains(name))
                return true;
        }

        return false;
    }
}
