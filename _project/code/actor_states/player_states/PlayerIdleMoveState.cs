using Godot;

public partial class PlayerIdleMoveState : ActorState
{
    private float _scanTimer = 0f;

    public PlayerIdleMoveState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        // Play Idle/Move animation, reset attack queues, etc.
    }

	public override void ProcessState(float delta)
    {
        Vector3 moveDir = _core.StateMachine.GetMovementDirection();
        _core.Motor.ProcessLocomotion(moveDir, _status.MaxSpeed, delta);

        // Check for state transitions
        if (_core.StateMachine.IsAttackRequested())
        {
            _core.StateMachine.ChangeState(new PlayerAttackingState(_core, this));
            return;
        }

        if (_core.StateMachine.IsDashRequested() && _status.DefaultDashCooldownTimer <= 0f)
        {
            _core.StateMachine.ChangeState(new PlayerDashState(_core, this));
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

        if (_core.StateMachine.IsTargetLockRequested())
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
        ActorCore foundTarget = CombatUtils.GetClosestActorInCone(
                _core,
                -_core.GlobalTransform.Basis.Z,
                _status.MaxTargetScanRange,
                _status.MaxTargetScanAngle,
                _status.Faction
            );

            if (foundTarget != null)
            {
                _core.Status.CurrentTarget = foundTarget;
                _core.StateMachine.ChangeState(new PlayerTargetingState(_core));
                return true;
            }

        return false;
    }


}
