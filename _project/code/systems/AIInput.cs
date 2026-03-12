using Godot;
using System;

public partial class AIInput : InputModule
{
    public override Vector3 GetMovementDirection()
    {
        ActorCore target = _core.Status.CurrentTarget;

        if (target == null || !Node.IsInstanceValid(target)) return Vector3.Zero;

        float distance = _core.GlobalPosition.DistanceTo(target.GlobalPosition);
        if (distance <= _core.Status.MaxDashDistance) return Vector3.Zero;

        return (_core.GlobalPosition.DirectionTo(target.GlobalPosition)) with {Y = 0};
    }

    public override bool IsAttackRequested()
    {
        return false;
        ActorCore target = _core.Status.CurrentTarget;

        if (target == null || !Node.IsInstanceValid(target)) return false;

        float distance = _core.GlobalPosition.DistanceTo(target.GlobalPosition);
        return distance <= _core.Status.MaxDashDistance;
    }

    public override bool IsTargetLockHeld()
    {
        return _core.Status.CurrentTarget != null;
    }

    public override bool IsTargetLockRequested()
    {
        return false;
    }
}
