using Godot;
using System;

public partial class DummyInput : InputModule
{
	public override Vector3 GetMovementDirection() => Vector3.Zero;
    public override bool IsAttackRequested() => false;
    public override bool IsTargetLockHeld() => false;
    public override bool IsTargetLockRequested() => false;
}
