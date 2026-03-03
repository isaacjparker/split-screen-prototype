using Godot;
using System;

public partial class AttackingState : ActorState
{
    private readonly ActorState _previousState;


    public AttackingState(ActorCore core, ActorState previousState) : base(core)
    {
        _previousState = previousState ?? new IdleMoveState(core);
    }

    public override void EnterState()
    {
        WeaponData weaponData = _core.Status.WeaponData;
        int index = _core.Status.ComboIndex;

        if (index >= weaponData.Attacks.Length)
        {
            GD.PushWarning($"Combo Index {index} out of bounds. Resetting.");
            _core.Status.ComboIndex = 0;
            index = 0;
        }

        _status.CurrentAttack = weaponData.Attacks[index];

        if (_core.SlashVfx != null && _status.CurrentAttack.SlashSprite != null)
        {
            _core.SlashVfx.Texture = _status.CurrentAttack.SlashSprite;
        }

        _status.ComboTimer = 0f;
        _status.NextAttackQueued = false;
        _status.HitboxActive = false;

        _core.Motor.StartDash(_core.Combat.BuildMeleeDash(_core.Status.CurrentTarget));

        _core.TriggerDashCam(_core.Status.CamDashDragFactor, _core.Status.CamDashDragDuration);
    }

	public override void ProcessState(float delta)
    {
        _core.Motor.ProcessDashMovement(delta);

        _status.ComboTimer += delta;

        // Check input immediately to act on in this frame
        if (_status.ComboTimer >= _status.CurrentAttack.ComboWindow.X && _status.ComboTimer <= _status.CurrentAttack.ComboWindow.Y)
        {
            if (_core.ActorInput.IsAttackRequested())
            {
                _status.NextAttackQueued = true;
            }
        }

        if (!_status.HitboxActive && _status.ComboTimer >= _status.CurrentAttack.Windup)
        {
            ActivateHitbox();
        }

        float endOfActive = _status.CurrentAttack.Windup + _status.CurrentAttack.Active;

        if (_status.ComboTimer >= endOfActive)
        {
            if (_status.HitboxActive)
            {
                DeactivateHitbox();
            }

            // Interrupt recovery phase for combo if possible
            if (_status.NextAttackQueued && IsNextAttackAvailable())
            {
                TryAdvanceCombo();
                return;                 // Stop processing frame immediately
            }

            // Check for movement cancellation
            Vector3 moveDir = _core.ActorInput.GetMovementDirection();

            if (moveDir.LengthSquared() > 0.01f)
            {
                ReturnToLocomotion();
                return;
            }
        }

        float totalDuration = endOfActive + _status.CurrentAttack.Recovery;

        if (_status.ComboTimer >= totalDuration)
        {
            TryAdvanceCombo();  // This will fail queue check and naturally return to Idle
            return;
        }
    }

    public override void ExitState()
    {
        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;

        if (_core.SlashVfx != null)
        {
            _core.SlashVfx.Visible = false;
        }
    }

    private void ActivateHitbox()
    {
        _status.HitboxActive = true;

        if (_core.SlashVfx != null) _core.SlashVfx.Visible = true;

        _core.Status.ActivePayload = _core.Combat.BuildAttackPayload(_status.CurrentAttack);
        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    private void DeactivateHitbox()
    {
        _status.HitboxActive = false;

        if (_core.SlashVfx != null) _core.SlashVfx.Visible = false;
        
        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;
    }

    private void TryAdvanceCombo()
    {
        if (_status.NextAttackQueued && IsNextAttackAvailable())
        {
            _core.Status.ComboIndex++;

            // Pass the original previous state forward to the next attack in the chain
            _core.StateMachine.ChangeState(new AttackingState(_core, _previousState));
            return;
        }
        else
        {
            ReturnToLocomotion();
            return;
        }
    }

    private void ReturnToLocomotion()
    {
        _core.Status.ComboIndex = 0;

        // If Manual Targeting is enabled, we don't scan. We just check if we still have a valid target.
        if (_core.Status.ManualTargeting)
        {
            if (_core.Status.CurrentTarget != null && GodotObject.IsInstanceValid(_core.Status.CurrentTarget))
            {
                _core.StateMachine.ChangeState(new TargetingState(_core));
                return;
            }
            _core.StateMachine.ChangeState(new IdleMoveState(_core));
            return;
        }

        // Automatic Targeting Logic: Scan for a target immediately upon exiting attack
        CharacterBody3D foundTarget = CombatUtils.GetClosestTargetInCone(
            _core.GlobalPosition,
            -_core.GlobalTransform.Basis.Z,
            _core.Status.MaxTargetScanRange,
            _core.Status.MaxTargetScanAngle,
            _core.Status.TargetGroup,
            _core.GetTree()
        );

        if (foundTarget != null)
        {
            _core.Status.CurrentTarget = foundTarget;
            _core.StateMachine.ChangeState(new TargetingState(_core));
        }
        else
        {
            _core.StateMachine.ChangeState(new IdleMoveState(_core));
        }
    }

    private bool IsNextAttackAvailable()
    {
        return (_core.Status.ComboIndex + 1) < _core.Status.WeaponData.Attacks.Length;
    }
}
