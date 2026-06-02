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
			_core.StateMachine.ChangeState((_core.StateMachine as AgentSM)?.CreateReturnState() ?? new AgentPatrolState(_core));
			return;
		}

		// Leash: a guard that has strayed too far from its post gives up and returns.
		// Night wave enemies (WaveMode) skip the leash entirely so they can chase across
		// the whole maze and still resume their march on the heart afterwards.
		bool waveMode = (_core.StateMachine as AgentSM)?.WaveMode == true;
		if (!waveMode && _core.GlobalPosition.DistanceTo(_core.InitialSpawnPosition) > _status.MaxTargetLeashRange)
		{
			_status.CurrentTarget = null;
			_core.StateMachine.ChangeState(new AgentPatrolState(_core));
			return;
		}

		float distance = _core.GlobalPosition.DistanceTo(target.GlobalPosition);

		if (distance <= _status.MaxDashDistance)
		{
			_core.StateMachine.ChangeState(new AgentCombatState(_core));
			return;
		}

		// Navmesh-aware pursuit so agents follow corridors instead of snagging on walls.
		// Falls back to direct steering automatically when the actor has no NavigationAgent3D.
		_core.Motor.ProcessNavLocomotion(target.GlobalPosition, _status.MaxSpeed, delta);
    }

    public override void ExitState()
    {
        
    }
}
