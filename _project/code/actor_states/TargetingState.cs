using Godot;
using System;

public partial class TargetingState : ActorState
{
	public TargetingState(PlayerBrain brain) : base(brain)
    {
    }

    public override void EnterState()
    {
        
    }

	public override void ProcessState(float delta)
    {
        if (_brain.CurrentTarget == null || !GodotObject.IsInstanceValid(_brain.CurrentTarget))
        {
            _brain.CurrentTarget = null;
            _brain.StateMachine.ChangeState(new IdleMoveState(_brain));
            return;
        }

        if (!_brain.IsTargetPressed())
        {
            _brain.CurrentTarget = null;
            _brain.StateMachine.ChangeState(new IdleMoveState(_brain));
            return;
        }

        if (_brain.IsAttackJustPressed())
        {
            _brain.StateMachine.ChangeState(new AttackingState(_brain));
            return;
        }

        Vector3 inputDir = _brain.GetInputDirection();
        _brain.Motor.ProcessTargetingLocomotion(inputDir, _brain.CurrentTarget, _brain.MaxSpeed, delta);
    }

    public override void ExitState()
    {
        
    }
}
