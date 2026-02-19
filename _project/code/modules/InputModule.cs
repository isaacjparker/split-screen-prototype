using Godot;
using System;

public partial class InputModule : Node
{
    public virtual Vector3 GetMovementDirection() => Vector3.Zero;
    public virtual bool IsAttackRequested() => false;
    public virtual bool IsTargetLockHeld() => false;
    public virtual bool IsTargetLockRequested() => false;

}
