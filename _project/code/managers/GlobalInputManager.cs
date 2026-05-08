using Godot;

public partial class GlobalInputManager: Node
{
    [Export] private string _startScreenScenePath = "res://Scenes/StartScreen.tscn";

    public override void _Process(double delta)
    {
        for (int i = 0; i < 4; i++)
        {
            if (Input.IsActionJustPressed($"select_{i}"))
            {
                GetTree().ChangeSceneToFile(_startScreenScenePath);
                return;
            }
        }
    }
}