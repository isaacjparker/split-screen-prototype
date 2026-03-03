using Godot;
using System;

public partial class TargetingState : ActorState
{
    private float _scanTimer = 0f;

	public TargetingState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        
    }

	public override void ProcessState(float delta)
    {
        if (_core.Status.CurrentTarget == null || !GodotObject.IsInstanceValid(_core.Status.CurrentTarget))
        {
            _core.Status.CurrentTarget = null;
            _core.StateMachine.ChangeState(new IdleMoveState(_core));
            return;
        }

        if (_core.ActorInput.IsAttackRequested())
        {
            _core.StateMachine.ChangeState(new AttackingState(_core, this));
            return;
        }

        if (!_core.Status.ManualTargeting)
        {
            float distToTarget = _core.GlobalPosition.DistanceTo(_core.Status.CurrentTarget.GlobalPosition);

            // 1. Leash Check (Run every frame for responsiveness)
            if (distToTarget > _core.Status.MaxTargetLeashRange)
            {
                _core.Status.CurrentTarget = null;
                _core.StateMachine.ChangeState(new IdleMoveState(_core));
                return;
            }

            // 2. Swap Check (Run on poll timer)
            _scanTimer -= delta;
            if (_scanTimer <= 0f)
            {
                _scanTimer = _core.Status.TargetingPollingRate;
                
                CharacterBody3D potentialTarget = CombatUtils.GetClosestTargetInCone(
                    _core.GlobalPosition,
                    -_core.GlobalTransform.Basis.Z,
                    _status.MaxTargetScanRange,
                    _status.MaxTargetScanAngle,
                    _status.TargetGroup,
                    _core.GetTree()
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
        else if (!_core.ActorInput.IsTargetLockHeld())
        {
            _core.Status.CurrentTarget = null;
            _core.StateMachine.ChangeState(new IdleMoveState(_core));
            return;
        }

        Vector3 moveDir = _core.ActorInput.GetMovementDirection();
        _core.Motor.ProcessTargetingLocomotion(moveDir, _core.Status.CurrentTarget, _status.MaxSpeed, delta);
    }

    public override void ExitState()
    {
        
    }
}
