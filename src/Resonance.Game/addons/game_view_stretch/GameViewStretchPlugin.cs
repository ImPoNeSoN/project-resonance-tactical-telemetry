using Godot;

/// <summary>
/// The editor game view defaults to a fixed 1280×720 viewport. Maximizing the window
/// then leaves the battle at its design size. Stretch-to-fit makes that viewport follow
/// the game window, which is what canvas_items + expand already handle at runtime.
/// </summary>
[Tool]
public partial class GameViewStretchPlugin : EditorPlugin
{
    private const int StretchToFit = 2;

    public override void _EnterTree()
    {
        EditorInterface.Singleton.GetEditorSettings().SetProjectMetadata("game_view", "embed_size_mode", StretchToFit);
    }
}
