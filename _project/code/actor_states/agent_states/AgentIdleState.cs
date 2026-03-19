using Godot;
using System;

public partial class AgentIdleState : ActorState
{
	private float _scanTimer = 0f;

    public AgentIdleState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        
    }

	public override void ProcessState(float delta)
    {
        _scanTimer -= delta;

		if (_scanTimer <= 0f)
		{
			_scanTimer = _status.TargetingPollingRate;

			ActorCore foundTarget = CombatUtils.GetHighestPriorityTarget(
				_core,
				-_core.GlobalTransform.Basis.Z, 
				_status.MaxTargetScanRange,
				_status.MaxTargetScanAngle
			);

			if (foundTarget != null)
			{
				_status.CurrentTarget = foundTarget;
				_core.StateMachine.ChangeState(new AgentPursueState(_core));
				return;
			}
		}
    }

    public override void ExitState()
    {
        
    }
}
