using Godot;

namespace Resonance.Game;

/// <summary>
/// Main scene. A playtest opens on the title screen, then party select, the Cinder Throat
/// floor, and a victory or defeat screen. The layout-lock check still opens the boss fight.
/// </summary>
public partial class RunShell : Control
{
    private TitleScreen _title = null!;
    private SettingsScreen _settings = null!;
    private PartySelect _party = null!;
    private FloorBoard _floor = null!;
    private GreyBoxBattle _battle = null!;
    private SimBridge _bridge = null!;
    private bool _shots;
    private int _shotFrame;

    public override void _Ready()
    {
        Window window = GetTree().Root;
        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = new Vector2I(1280, 720);
        window.ContentScaleFactor = 1f;
        _shots = BuildStamp.DevSession && OS.GetEnvironment("PLAYTEST_SHOT") == "1";
        if (GreyBoxBattle.LayoutLockRequested(out int lockWidth, out int lockHeight))
        {
            window.Mode = Window.ModeEnum.Windowed;
            window.Borderless = false;
            window.Size = new Vector2I(lockWidth, lockHeight);
        }
        else if (_shots)
        {
            window.Mode = Window.ModeEnum.Windowed;
            window.Borderless = false;
            window.Size = new Vector2I(1280, 720);
        }
        else
        {
            PlaytestSettings.Apply(window);
        }

        _bridge = GetNode<SimBridge>("/root/SimBridge");
        AnchorFull(this);

        _title = new TitleScreen();
        AddChild(_title);
        AnchorFull(_title);

        _settings = new SettingsScreen { Visible = false };
        AddChild(_settings);
        AnchorFull(_settings);

        _party = new PartySelect { Visible = false };
        AddChild(_party);
        AnchorFull(_party);

        _floor = new FloorBoard { Visible = false };
        AddChild(_floor);
        AnchorFull(_floor);

        _battle = new GreyBoxBattle { Visible = false };
        AddChild(_battle);
        AnchorFull(_battle);

        _title.PlayPressed += ShowParty;
        _title.SettingsPressed += ShowSettings;
        _settings.Closed += ShowTitle;
        _party.FloorChosen += ShowFloorFromParty;
        _party.BossChosen += ShowBossFromParty;
        _party.TitleRequested += ShowTitle;
        _floor.FightRequested += ShowFight;
        _floor.PartyRequested += ShowParty;
        _floor.TitleRequested += ShowTitle;
        _battle.ReturnedToFloor += ShowFloor;
        _battle.ReturnedToParty += ShowParty;
        if (GreyBoxBattle.LayoutLockRequested(out _, out _))
        {
            ShowBossFromParty();
            _battle.BeginLayoutLock();
        }
    }

    public override void _Process(double delta)
    {
        if (!_shots)
        {
            return;
        }

        _shotFrame++;
        if (_shotFrame == 6)
        {
            SaveShot("title");
        }
        else if (_shotFrame == 8)
        {
            ShowSettings();
        }
        else if (_shotFrame == 14)
        {
            SaveShot("settings");
        }
        else if (_shotFrame == 16)
        {
            _settings.Visible = false;
            _title.Visible = false;
            _bridge.StartFloor(0);
            _bridge.FastForwardFloor();
            _floor.Visible = true;
        }
        else if (_shotFrame == 30)
        {
            SaveShot("result");
            GetTree().Quit();
        }
    }

    private void SaveShot(string name)
    {
        Image? image = GetViewport().GetTexture().GetImage();
        if (image == null)
        {
            return;
        }

        string path = $"/tmp/playtest-{name}.png";
        image.SavePng(path);
        GD.Print($"PLAYTEST_SHOT {path}");
    }

    private void ShowTitle()
    {
        if (_bridge.AutoRunning)
        {
            _bridge.ToggleAuto();
        }

        _bridge.LeaveFloor();
        _settings.Visible = false;
        _party.Visible = false;
        _floor.Visible = false;
        _battle.Visible = false;
        _title.Visible = true;
    }

    private void ShowSettings()
    {
        _title.Visible = false;
        _party.Visible = false;
        _floor.Visible = false;
        _battle.Visible = false;
        _settings.Visible = true;
    }

    private void ShowParty()
    {
        if (_bridge.AutoRunning)
        {
            _bridge.ToggleAuto();
        }

        _title.Visible = false;
        _settings.Visible = false;
        _battle.Visible = false;
        _floor.Visible = false;
        _party.Visible = true;
    }

    private void ShowFloorFromParty()
    {
        _title.Visible = false;
        _party.Visible = false;
        _battle.Visible = false;
        _floor.Visible = true;
    }

    private void ShowBossFromParty()
    {
        _title.Visible = false;
        _party.Visible = false;
        _floor.Visible = false;
        _battle.Visible = true;
        _battle.OpenBossFight();
    }

    private void ShowFight()
    {
        _floor.Visible = false;
        _battle.Visible = true;
        _battle.OpenFloorFight();
    }

    private void ShowFloor()
    {
        _battle.Visible = false;
        _party.Visible = false;
        _title.Visible = false;
        _floor.Visible = true;
    }

    private static void AnchorFull(Control control)
    {
        control.AnchorLeft = 0f;
        control.AnchorTop = 0f;
        control.AnchorRight = 1f;
        control.AnchorBottom = 1f;
        control.OffsetLeft = 0f;
        control.OffsetTop = 0f;
        control.OffsetRight = 0f;
        control.OffsetBottom = 0f;
        control.GrowHorizontal = GrowDirection.Both;
        control.GrowVertical = GrowDirection.Both;
        control.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        control.SizeFlagsVertical = SizeFlags.ExpandFill;
    }
}
