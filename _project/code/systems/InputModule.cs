using Godot;
using System;

public partial class InputModule : Node
{
    protected ActorCore _core;
    public void Initialise(ActorCore core)
    {
        _core = core;
    }

    public virtual Vector3 GetMovementDirection() => Vector3.Zero;
    public virtual bool IsAttackRequested() => false;
    public virtual bool IsTargetLockHeld() => false;
    public virtual bool IsTargetLockRequested() => false;

}
