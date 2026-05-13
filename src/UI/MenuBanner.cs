using LemonUI.Elements;
using LemonUI.Menus;

public static class MenuBanner
{
    public static void Apply(NativeMenu menu)
    {
        if (menu == null || !Config.GetBool("EnableEditorBanner", true))
            return;

        string dictionary = Config.Get("EditorBannerDictionary", "commonmenu");
        string texture = Config.Get("EditorBannerTexture", "interaction_bgd");

        if (string.IsNullOrWhiteSpace(dictionary) || string.IsNullOrWhiteSpace(texture))
            return;

        menu.Banner = new ScaledTexture(dictionary, texture);
    }
}
