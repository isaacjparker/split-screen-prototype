using Godot;
using System;
using System.Threading.Tasks;


public partial class CombatModule : Node
{
    [ExportGroup("Lunge Settings")]
    [Export] private float _maxLungeDistance = 5.0f;
    [Export] private float _lungeCone = 45.0f;
    [Export] private string _targetGroup = "enemies";
    [Export] private float _lungeDuration = 0.2f;
    [Export] private float _lungeStopOffset = 1.5f;
    [Export] private float _lungeWhiffDistance = 1.0f;
    [Export] private float _lungeWhiffDuration = 0.15f;

    private CharacterBody3D _actor;
    private MotorModule _motor;
    

    public void Initialize(CharacterBody3D actor, MotorModule motor)
    {
        _actor = actor;
        _motor = motor;
    }

    public void PerformMeleeLunge(CharacterBody3D lockedTarget = null)
    {
        CharacterBody3D finalTarget = lockedTarget;

        // If not hard targting, look for a "soft" target
        if (finalTarget == null)
        { 
			Vector3 forward = -_actor.GlobalTransform.Basis.Z;

        	CharacterBody3D target = CombatUtils.GetClosestTargetInCone(
				_actor.GlobalPosition, 
				forward,
				_maxLungeDistance,
				_lungeCone,
				_targetGroup,
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
            duration = _lungeDuration;
            offset = _lungeStopOffset;
        }
        else
        {
            Vector3 forward = -_actor.GlobalTransform.Basis.Z;
            lungePos = _actor.GlobalPosition + (forward * _lungeWhiffDistance);
            duration = _lungeWhiffDuration;
            offset = 0.0f;
        }

        // Execute lunge
        _motor.Lunge(_actor, lungePos, duration, offset);
    }

	public void TakeDamage(int amount)
	{
		// Stub for future hit reactions/health
        GD.Print($"{_actor.Name} took {amount} damage!");
	}
}
