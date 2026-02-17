using Godot;
using System;

public abstract class ActorState
{
    protected PlayerBrain _brain;

    public ActorState(PlayerBrain brain)
    {
        _brain = brain;
    }

    public abstract void EnterState();
    public abstract void ProcessState(float delta);
    public abstract void ExitState();
}
