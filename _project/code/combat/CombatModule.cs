using Godot;

public readonly struct DashPayload
{
    public readonly Vector3 TargetPosition;
    public readonly float Duration;
    public readonly float StopOffset;
    public readonly bool IsWhiff; // Helpful for animation logic if needed

    public DashPayload(Vector3 target, float duration, float offset, bool isWhiff)
    {
        TargetPosition = target;
        Duration = duration;
        StopOffset = offset;
        IsWhiff = isWhiff;
    }
}

public partial class CombatModule : Node
{
    private ActorCore _core;
    private StatusModule _status;


    public void Initialise(ActorCore core)
    {
        _core = core;
        _status = _core.Status;
    }

    public DashPayload CalculateMeleeDash(CharacterBody3D lockedTarget = null)
    {
        CharacterBody3D finalTarget = lockedTarget;

        // If not hard targting, look for a "soft" target
        if (finalTarget == null)
        { 
			Vector3 forward = -_core.GlobalTransform.Basis.Z;

        	finalTarget = CombatUtils.GetClosestTargetInCone(
				_core.GlobalPosition, 
				forward,
				_status.MaxDashDistance,
				_status.DashCone,
				_status.TargetGroup,
				GetTree()
			);
		}

        DashPayload dashPayload;

        if (finalTarget != null && IsInstanceValid(finalTarget))
        {
            Vector3 startPos = _core.GlobalPosition;
            Vector3 targetPos = finalTarget.GlobalPosition;
            Vector3 toTarget = targetPos - startPos;
            toTarget.Y = 0; // Flatten to match MotorModule logic

            float stopOffset = _status.DashStopOffset;

            if (toTarget.Length() > _status.MaxDashDistance)
            {
                targetPos = startPos + (toTarget.Normalized() * _status.MaxDashDistance);
                stopOffset = 0f; // Don't stop short if we are maxing out distance
            }

            dashPayload = new DashPayload(targetPos, _status.DashDuration, stopOffset, false);
        }
        else
        {
            Vector3 forward = -_core.GlobalTransform.Basis.Z;
            Vector3 dashPos = _core.GlobalPosition + (forward * _status.DashWhiffDistance);

            dashPayload = new DashPayload(dashPos, _status.DashWhiffDuration, 0.0f, true);
        }

        return dashPayload;
    }

    // TODO: Don't use parameter here -
    // We need modules to cache the runtime data source
    // But currently this is PlayerBrain which won't extend to enemies
    // So instead of refactoring our data approach right now
    // Come back later and change it so as to avoid parameters like this.
    public AttackPayload BuildAttackPayload(AttackData attackData)
    {
        return new AttackPayload 
        {
        BaseDamage = attackData.BaseDamage,
        KnockbackPower = attackData.KnockbackPower,
        HitStopDuration = attackData.HitStopDuration,
        HitStopFactor = attackData.HitStopFactor,
        SourcePosition = _core.GlobalPosition
        };
    }
}
