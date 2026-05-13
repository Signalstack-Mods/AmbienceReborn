using LemonUI.Menus;
using GTA;

public class UIManager
{
    public NativeMenu Menu;

    public UIManager()
    {
        Menu = new NativeMenu("AmbienceReborn", "Zones");
        MenuBanner.Apply(Menu);

        var create = new NativeItem("Create Zone");

        create.Activated += (s, e) =>
        {
            ZoneManager.Zones.Add(new Zone
            {
                Name = "Zone_" + ZoneManager.Zones.Count,
                Position = Game.Player.Character.Position,
                Radius = 50f,
                Volume = 0.7f
            });
        };

        Menu.Add(create);
    }

    public void Process()
    {
        Menu?.Process();
    }
}
