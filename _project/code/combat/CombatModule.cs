using Godot;


public partial class CombatModule : Node
{
    private ActorCore _core;
    private MotorModule _motor;
    private StatusModule _status;


    public void Initialise(ActorCore core, MotorModule motor)
    {
        _core = core;
        _motor = motor;
        _status = _core.Status;
    }

    public void PerformMeleeLunge(CharacterBody3D lockedTarget = null)
    {
        CharacterBody3D finalTarget = lockedTarget;

        // If not hard targting, look for a "soft" target
        if (finalTarget == null)
        { 
			Vector3 forward = -_core.GlobalTransform.Basis.Z;

        	CharacterBody3D target = CombatUtils.GetClosestTargetInCone(
				_core.GlobalPosition, 
				forward,
				_status.MaxLungeDistance,
				_status.LungeCone,
				_status.TargetGroup,
				GetTree()
			);
		}

        // Prepare lunge data
        Vector3 lungePos;
        float duration;
        float offset;

        if (finalTarget != null && IsInstanceValid(finalTarget))
        {
            lungePos = finalTarget.GlobalPosition;
            duration = _status.LungeDuration;
            offset = _status.LungeStopOffset;
        }
        else
        {
            Vector3 forward = -_core.GlobalTransform.Basis.Z;
            lungePos = _core.GlobalPosition + (forward * _status.LungeWhiffDistance);
            duration = _status.LungeWhiffDuration;
            offset = 0.0f;
        }

        // Execute lunge
        _motor.Lunge(_core, lungePos, duration, offset);
    }

    // TODO: Don't use paramete here -
    // We need modules to cache the runtime data source
    // But currently this is PlayerBrain which won't extend to enemies
    // So instead of refactoring our data approach right now
    // Come back later and change it so as to avoid parameters like this.
    public AttackPayload BuildAttackPayload()
    {
        return new AttackPayload 
        {
        BaseDamage = _status.BaseDamage,
        KnockbackPower = _status.KnockbackPower,
        HitStopDuration = _status.HitStopDuration,
        SourcePosition = _core.GlobalPosition
        };
    }
}
