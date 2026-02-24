using Godot;

public readonly struct LungePayload
{
    public readonly Vector3 TargetPosition;
    public readonly float Duration;
    public readonly float StopOffset;
    public readonly bool IsWhiff; // Helpful for animation logic if needed

    public LungePayload(Vector3 target, float duration, float offset, bool isWhiff)
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

    public LungePayload CalculateMeleeLunge(CharacterBody3D lockedTarget = null)
    {
        CharacterBody3D finalTarget = lockedTarget;

        // If not hard targting, look for a "soft" target
        if (finalTarget == null)
        { 
			Vector3 forward = -_core.GlobalTransform.Basis.Z;

        	finalTarget = CombatUtils.GetClosestTargetInCone(
				_core.GlobalPosition, 
				forward,
				_status.MaxLungeDistance,
				_status.LungeCone,
				_status.TargetGroup,
				GetTree()
			);
		}

        LungePayload lungePayload;

        if (finalTarget != null && IsInstanceValid(finalTarget))
        {
            lungePayload = new LungePayload(finalTarget.GlobalPosition, _status.LungeDuration, _status.LungeStopOffset, false);
        }
        else
        {
            Vector3 forward = -_core.GlobalTransform.Basis.Z;
            Vector3 lungePos = _core.GlobalPosition + (forward * _status.LungeWhiffDistance);

            lungePayload = new LungePayload(lungePos, _status.LungeWhiffDuration, 0.0f, true);
        }

        return lungePayload;
    }

    // TODO: Don't use parameter here -
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
