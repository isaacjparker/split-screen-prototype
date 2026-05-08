using Godot;

public partial class StartScreen : Control
{
    [Export] private string _gameplayScenePath = "res://Scenes/Gameplay.tscn";

    public override void _Process(double delta)
    {
        for (int i = 0; i < 4; i++)
        {
            if (Input.IsActionJustPressed($"interact_{i}"))
            {
                GetTree().ChangeSceneToFile(_gameplayScenePath);
                return;
            }
        }

        // Quit on any select press (using whatever you've bound select to)
        for (int i = 0; i < 4; i++)
        {
            if (Input.IsActionJustPressed($"select_{i}"))
            {
                GetTree().Quit();
                return;
            }
        }
    }
}