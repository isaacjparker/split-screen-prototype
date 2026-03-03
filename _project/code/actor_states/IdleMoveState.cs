using Godot;
using System;

public partial class IdleMoveState : ActorState
{
    private float _scanTimer = 0f;

    public IdleMoveState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        // Play Idle/Move animation, reset attack queues, etc.
    }

	public override void ProcessState(float delta)
    {
        Vector3 moveDir = _core.ActorInput.GetMovementDirection();
        _core.Motor.ProcessLocomotion(moveDir, _status.MaxSpeed, delta);

        // Check for state transitions
        if (_core.ActorInput.IsAttackRequested())
        {
            _core.StateMachine.ChangeState(new AttackingState(_core, this));
            return;
        }

        // Automatic Targeting Logic
        if (!_core.Status.ManualTargeting)
        {
            _scanTimer -= delta;
            if (_scanTimer <= 0f)
            {
                _scanTimer = _core.Status.TargetingPollingRate;
                if (TryMoveToTargetingState()) return;
            }
        }

        if (_core.ActorInput.IsTargetLockRequested())
        {
            TryMoveToTargetingState();
            // If no target found, stay in IdleMove state
        }
    }

    public override void ExitState()
    {
        // Cleanup if neessary
    }

    private bool TryMoveToTargetingState()
    {
        CharacterBody3D foundTarget = CombatUtils.GetClosestTargetInCone(
                _core.GlobalPosition,
                -_core.GlobalTransform.Basis.Z,
                _status.MaxTargetScanRange,
                _status.MaxTargetScanAngle,
                _status.TargetGroup,
                _core.GetTree()
            );

            if (foundTarget != null)
            {
                _core.Status.CurrentTarget = foundTarget;
                _core.StateMachine.ChangeState(new TargetingState(_core));
                return true;
            }

        return false;
    }


}
