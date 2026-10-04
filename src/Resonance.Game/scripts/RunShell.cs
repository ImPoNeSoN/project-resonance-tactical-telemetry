using Godot;

namespace Resonance.Game;

/// <summary>
/// Main scene. Party select is the front door. The floor map and the boss fight sit behind it.
/// </summary>
public partial class RunShell : Control
{
    private PartySelect _party = null!;
    private FloorBoard _floor = null!;
    private GreyBoxBattle _battle = null!;

    public override void _Ready()
    {
        Window window = GetTree().Root;
        window.ContentScaleMode = Window.ContentScaleModeEnum.CanvasItems;
        window.ContentScaleAspect = Window.ContentScaleAspectEnum.Expand;
        window.ContentScaleSize = new Vector2I(1280, 720);
        window.ContentScaleFactor = 1f;

        AnchorFull(this);
        _party = new PartySelect();
        AddChild(_party);
        AnchorFull(_party);

        _floor = new FloorBoard { Visible = false };
        AddChild(_floor);
        AnchorFull(_floor);

        _battle = new GreyBoxBattle { Visible = false };
        AddChild(_battle);
        AnchorFull(_battle);

        _party.FloorChosen += ShowFloorFromParty;
        _party.BossChosen += ShowBossFromParty;
        _floor.FightRequested += ShowFight;
        _floor.PartyRequested += ShowParty;
        _battle.ReturnedToFloor += ShowFloor;
        _battle.ReturnedToParty += ShowParty;
    }

    private void ShowFloorFromParty()
    {
        _party.Visible = false;
        _battle.Visible = false;
        _floor.Visible = true;
    }

    private void ShowBossFromParty()
    {
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
        _floor.Visible = true;
    }

    private void ShowParty()
    {
        _battle.Visible = false;
        _floor.Visible = false;
        _party.Visible = true;
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
