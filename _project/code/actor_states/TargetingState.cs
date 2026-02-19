using Godot;
using System;

public partial class TargetingState : ActorState
{
	public TargetingState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        
    }

	public override void ProcessState(float delta)
    {
        if (_core.CurrentTarget == null || !GodotObject.IsInstanceValid(_core.CurrentTarget))
        {
            _core.CurrentTarget = null;
            _core.StateMachine.ChangeState(new IdleMoveState(_core));
            return;
        }

        if (!_core.ActorInput.IsTargetLockHeld())
        {
            _core.CurrentTarget = null;
            _core.StateMachine.ChangeState(new IdleMoveState(_core));
            return;
        }

        if (_core.ActorInput.IsAttackRequested())
        {
            _core.StateMachine.ChangeState(new AttackingState(_core));
            return;
        }

        Vector3 moveDir = _core.ActorInput.GetMovementDirection();
        _core.Motor.ProcessTargetingLocomotion(moveDir, _core.CurrentTarget, _status.MaxSpeed, delta);
    }

    public override void ExitState()
    {
        
    }
}
