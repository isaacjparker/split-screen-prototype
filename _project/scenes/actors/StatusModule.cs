using Godot;
using System;

public partial class StatusModule : Node
{
    private PlayerBrain _brain;

    public void Initialise(PlayerBrain brain)
    {
        _brain = brain;
    }
}
