using Godot;
using System;

public partial class AgentPursueState : ActorState
{
    public AgentPursueState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        
    }

	public override void ProcessState(float delta)
    {
        ActorCore target = _status.CurrentTarget;

		if (target == null || !Node.IsInstanceValid(target))
		{
			_status.CurrentTarget = null;
			_core.StateMachine.ChangeState(new AgentIdleState(_core));
			return;
		}

		float distance = _core.GlobalPosition.DistanceTo(target.GlobalPosition);

		if (distance <= _status.MaxDashDistance)
		{
			_core.StateMachine.ChangeState(new AgentCombatState(_core));
			return;
		}

		Vector3 moveDir = (_core.GlobalPosition.DirectionTo(target.GlobalPosition)) with {Y = 0};
		_core.Motor.ProcessLocomotion(moveDir, _status.MaxSpeed, delta);
    }

    public override void ExitState()
    {
        
    }
}
