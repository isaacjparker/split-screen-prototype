using Godot;

public partial class CombatModule : Node
{
    private ActorCore _core;
    private StatusModule _status;


    public void Initialise(ActorCore core)
    {
        _core = core;
        _status = _core.Status;
    }

    public DashPayload BuildDefaultDash(Vector3 inputDirection)
    {
        Vector3 dashDir = inputDirection;
        dashDir.Y = 0;

        // If no input, dash forward
        if (dashDir.LengthSquared() < 0.01f)
        {
            dashDir = -_core.GlobalTransform.Basis.Z;
        }

        dashDir = dashDir.Normalized();

        Vector3 dashTarget = _core.GlobalPosition + (dashDir * _status.DefaultDashDistance);
        return new DashPayload(dashTarget, _status.DefaultDashDuration, 0f, true);
    }

    public DashPayload BuildMeleeDash(ActorCore lockedTarget = null)
    {
        ActorCore finalTarget = lockedTarget;

        // If not hard targting, look for a "soft" target
        if (finalTarget == null)
        { 
			Vector3 forward = -_core.GlobalTransform.Basis.Z;

        	finalTarget = CombatUtils.GetClosestActorInCone(
				_core, 
				forward,
				_status.MaxDashDistance,
				_status.DashCone,
				_status.Faction
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

    
    public AttackPayload BuildAttackPayload(AttackData attackData)
    {
        return new AttackPayload 
        {
        SourceActor = _core,
        BaseDamage = attackData.BaseDamage,
        KnockbackPower = attackData.KnockbackPower,
        HitStopDuration = attackData.HitStopDuration,
        HitStopFactor = attackData.HitStopFactor,
        SourcePosition = _core.GlobalPosition,
        AttackAudio = attackData.AttackAudio,
        ImpactAudio = attackData.ImpactAudio
        };
    }
}
