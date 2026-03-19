using Godot;
using System;

public partial class PlayerTargetingState : ActorState
{
    private float _scanTimer = 0f;

	public PlayerTargetingState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        GD.Print($"Entered PlayerTargetingState, target: {_status.CurrentTarget}");
    }

	public override void ProcessState(float delta)
    {
        if (_core.Status.CurrentTarget == null || !GodotObject.IsInstanceValid(_core.Status.CurrentTarget))
        {
            _core.Status.CurrentTarget = null;
            _core.StateMachine.ChangeState(new PlayerIdleMoveState(_core));
            return;
        }

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

        if (!_core.Status.ManualTargeting)
        {
            float distToTarget = _core.GlobalPosition.DistanceTo(_core.Status.CurrentTarget.GlobalPosition);

            // 1. Leash Check (Run every frame for responsiveness)
            if (distToTarget > _core.Status.MaxTargetLeashRange)
            {
                _core.Status.CurrentTarget = null;
                _core.StateMachine.ChangeState(new PlayerIdleMoveState(_core));
                return;
            }

            // 2. Swap Check (Run on poll timer)
            _scanTimer -= delta;
            if (_scanTimer <= 0f)
            {
                _scanTimer = _core.Status.TargetingPollingRate;
                
                ActorCore potentialTarget = CombatUtils.GetClosestActorInCone(
                    _core,
                    -_core.GlobalTransform.Basis.Z,
                    _status.MaxTargetScanRange,
                    _status.MaxTargetScanAngle,
                    _status.Faction
                );

                if (potentialTarget != null && potentialTarget != _core.Status.CurrentTarget)
                {
                    float distToPotential = _core.GlobalPosition.DistanceTo(potentialTarget.GlobalPosition);
                    
                    // Only swap if the new target is significantly closer
                    if (distToTarget - distToPotential > _core.Status.MaxTargetSwapDifference)
                    {
                        _core.Status.CurrentTarget = potentialTarget;
                    }
                }
            }
        }
        else if (!_core.StateMachine.IsTargetLockHeld())
        {
            _core.Status.CurrentTarget = null;
            _core.StateMachine.ChangeState(new PlayerIdleMoveState(_core));
            return;
        }

        Vector3 moveDir = _core.StateMachine.GetMovementDirection();
        _core.Motor.ProcessTargetingLocomotion(moveDir, _core.Status.CurrentTarget, _status.MaxSpeed, delta);
    }

    public override void ExitState()
    {
        
    }
}
