using Godot;
using Resonance.Sim.Data;

namespace Resonance.Game;

/// <summary>
/// 9-slice panels and status icons from the proof HUD kit.
/// Texture margins are the @2x values in assets/ui/MANIFEST.md.
/// </summary>
public static class HudSkin
{
    private const string Hud = "assets/ui/hud/";
    private const string Icons = "assets/ui/icons/";
    private static readonly Dictionary<string, Texture2D?> Cache = new();

    public static Theme BattleTheme()
    {
        var theme = new Theme();
        theme.SetStylebox("normal", "Button", Slice("hud_button_normal@2x.png", 24, 10));
        theme.SetStylebox("hover", "Button", Slice("hud_button_hover@2x.png", 24, 10));
        theme.SetStylebox("pressed", "Button", Slice("hud_button_pressed@2x.png", 24, 10));
        theme.SetStylebox("disabled", "Button", Slice("hud_button_normal@2x.png", 24, 10));
        theme.SetStylebox("focus", "Button", Slice("hud_button_hover@2x.png", 24, 10));
        theme.SetColor("font_color", "Button", new Color("d5dde8"));
        theme.SetColor("font_hover_color", "Button", new Color("ffffff"));
        theme.SetColor("font_pressed_color", "Button", new Color("ffe08a"));
        theme.SetColor("font_disabled_color", "Button", new Color("8ea0b5"));
        theme.SetFontSize("font_size", "Button", 13);
        return theme;
    }

    public static StyleBox PartyCard() => Slice("hud_party_card@2x.png", 32, 14);

    public static StyleBox BossPart() => Slice("hud_boss_part_panel@2x.png", 32, 24, 32, 24, 12);

    public static StyleBox Ability() => Slice("hud_ability_panel@2x.png", 24, 10);

    public static StyleBox Timeline() => Slice("hud_timeline_bar@2x.png", 48, 24, 48, 24, 8);

    public static Texture2D? Status(string name) => Png($"{Icons}icon_status_{name}@2x.png");

    public static Texture2D? Chain(ChainProperty property)
    {
        string file = property switch
        {
            ChainProperty.Blunt => "icon_element_blunt@2x.png",
            ChainProperty.Piercing => "icon_element_piercing@2x.png",
            ChainProperty.Slashing => "icon_element_slashing@2x.png",
            ChainProperty.Fire => "icon_element_fire@2x.png",
            ChainProperty.Ice => "icon_element_ice@2x.png",
            ChainProperty.Wind => "icon_element_wind@2x.png",
            ChainProperty.Earth => "icon_element_earth@2x.png",
            ChainProperty.Lightning => "icon_element_lightning@2x.png",
            ChainProperty.Water => "icon_element_water@2x.png",
            ChainProperty.Light => "icon_element_light@2x.png",
            ChainProperty.Darkness => "icon_element_darkness@2x.png",
            _ => "",
        };
        return file.Length == 0 ? null : Png(Icons + file);
    }

    private static StyleBox Slice(string file, int margin, int content) =>
        Slice(file, margin, margin, margin, margin, content);

    private static StyleBox Slice(string file, int left, int top, int right, int bottom, int content)
    {
        Texture2D? texture = Png(Hud + file);
        if (texture == null)
        {
            return new StyleBoxFlat
            {
                BgColor = new Color("1b2230"),
                ContentMarginLeft = content,
                ContentMarginTop = content,
                ContentMarginRight = content,
                ContentMarginBottom = content,
            };
        }

        return new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = left,
            TextureMarginTop = top,
            TextureMarginRight = right,
            TextureMarginBottom = bottom,
            ContentMarginLeft = content,
            ContentMarginTop = content,
            ContentMarginRight = content,
            ContentMarginBottom = content,
            AxisStretchHorizontal = StyleBoxTexture.AxisStretchMode.Stretch,
            AxisStretchVertical = StyleBoxTexture.AxisStretchMode.Stretch,
        };
    }

    /// <summary>
    /// Kit filenames keep the @2x suffix. Godot 4 loads that path as a normal resource,
    /// including from an exported pck. A raw PNG read is the fallback when the import is missing.
    /// </summary>
    private static Texture2D? Png(string relative)
    {
        if (Cache.TryGetValue(relative, out Texture2D? cached))
        {
            return cached;
        }

        string res = "res://" + relative;
        Texture2D? texture = ResourceLoader.Load<Texture2D>(res);
        if (texture == null && Godot.FileAccess.FileExists(res))
        {
            using Godot.FileAccess file = Godot.FileAccess.Open(res, Godot.FileAccess.ModeFlags.Read);
            var image = new Image();
            if (file != null && image.LoadPngFromBuffer(file.GetBuffer((long)file.GetLength())) == Error.Ok)
            {
                texture = ImageTexture.CreateFromImage(image);
            }
        }

        if (texture == null)
        {
            GD.PrintErr($"ARENA_IMPORT missing {relative}");
        }

        Cache[relative] = texture;
        return texture;
    }
}
