using Godot;

namespace Resonance.Game;

/// <summary>
/// Main scene. The floor map is the front door. Boss only opens the grey-box battle.
/// </summary>
public partial class RunShell : Control
{
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
        _floor = new FloorBoard();
        AddChild(_floor);
        AnchorFull(_floor);

        _battle = new GreyBoxBattle { Visible = false };
        AddChild(_battle);
        AnchorFull(_battle);

        _floor.FightRequested += ShowFight;
        _floor.BossOnlyRequested += ShowBossOnly;
        _battle.ReturnedToFloor += ShowFloor;
    }

    private void ShowFight()
    {
        _floor.Visible = false;
        _battle.Visible = true;
        _battle.OpenFloorFight();
    }

    private void ShowBossOnly()
    {
        _floor.Visible = false;
        _battle.Visible = true;
        _battle.ShowBossPicker();
    }

    private void ShowFloor()
    {
        _battle.Visible = false;
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
