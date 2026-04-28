using Godot;
using System;

public partial class AgentCombatState : ActorState
{
	private float _attackDelayTimer;
	private float _strafeDirection;
	private float _strafeFlipTimer;

	private static readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    public AgentCombatState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        _attackDelayTimer = _rng.RandfRange(1.0f, 3.0f);
		_strafeDirection = _rng.Randf() > 0.5f ? 1f : -1f;
		_strafeFlipTimer = _rng.RandfRange(1.5f, 3.0f);
    }

	public override void ProcessState(float delta)
    {
		ActorCore target = _status.CurrentTarget;

		if (target == null || !Node.IsInstanceValid(target) || !target.Status.IsAlive)
		{
			_status.CurrentTarget = null;
			_core.StateMachine.ChangeState(new AgentIdleState(_core));
			return;
		}

		float distance = _core.GlobalPosition.DistanceTo(target.GlobalPosition);

		// If target has moved out of engagement range, pursue again
		if (distance > _status.MaxDashDistance * 1.5f)
		{
			_core.StateMachine.ChangeState(new AgentPursueState(_core));
			return;
		}

		// Circle strafing around target
		Vector3 toTarget = (_core.GlobalPosition.DirectionTo(target.GlobalPosition)) with { Y = 0 };
        Vector3 strafeDir = new Vector3(-toTarget.Z, 0, toTarget.X) * _strafeDirection;
        _core.Motor.ProcessTargetingLocomotion(strafeDir, target, _status.MaxSpeed * 0.5f, delta);
 
		// Periodically flip strafe direction
		_strafeFlipTimer -= delta;
		if (_strafeFlipTimer <= 0f)
		{
			_strafeDirection *= -1f;
			_strafeFlipTimer = _rng.RandfRange(1.5f, 3.0f);
		}

		// Count down to attack
		_attackDelayTimer -= delta;
		if (_attackDelayTimer <= 0f)
		{
			if (_status.EquippedWeapon == null)
			{
				_attackDelayTimer = _rng.RandfRange(1.0f, 3.0f);
				return;
			}
			_core.StateMachine.ChangeState(new AgentAttackingState(_core));
			return;
		}
    }

    public override void ExitState()
    {
        
    }
}
