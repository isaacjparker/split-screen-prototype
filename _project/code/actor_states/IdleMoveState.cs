using Godot;
using System;

public partial class IdleMoveState : ActorState
{
    public IdleMoveState(PlayerBrain brain) : base(brain)
    {
    }

    public override void EnterState()
    {
        // Play Idle/Move animation, reset attack queues, etc.
    }

	public override void ProcessState(float delta)
    {
        Vector3 inputDir = _brain.GetInputDirection();
        _brain.Motor.ProcessLocomotion(inputDir, _brain.MaxSpeed, delta);

        // Check for state transitions
        if (_brain.IsAttackJustPressed())
        {
            _brain.StateMachine.ChangeState(new AttackingState(_brain));
            return;
        }

        if (_brain.IsTargetJustPressed())
        {
            CharacterBody3D foundTarget = CombatUtils.GetClosestTargetInCone(
                _brain.GlobalPosition,
                -_brain.GlobalTransform.Basis.Z,
                _brain.MaxTargetRange,
                _brain.MaxTargetScanAngle,
                _brain.TargetGroup,
                _brain.GetTree()
            );

            if (foundTarget != null)
            {
                _brain.CurrentTarget = foundTarget;
                _brain.StateMachine.ChangeState(new TargetingState(_brain));
                return;
            }

            // If no target found, stay in IdleMove state
        }
    }

    public override void ExitState()
    {
        // Cleanup if neessary
    }


}
