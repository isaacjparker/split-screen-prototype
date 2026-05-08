using Godot;

public partial class FPSLabel : Label
{
    public override void _Process(double delta)
    {
        Text = $"{Engine.GetFramesPerSecond():F0} FPS";
    }
}