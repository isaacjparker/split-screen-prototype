using Godot;
using System;

public partial class AttackingState : ActorState
{

    //private float _stateTimer = 0f;
    private AttackData _currentAttack;
    private bool _nextAttackQueued = false;
    private bool _hitboxActive = false;
    private readonly ActorState _previousState;
    private Vector3 _dashVelocity;
    private float _dashFriction;

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

        _currentAttack = weaponData.Attacks[index];

        if (_core.SlashVfx != null && _currentAttack.SlashSprite != null)
        {
            _core.SlashVfx.Texture = _currentAttack.SlashSprite;
        }

        _status.ComboTimer = 0f;
        _nextAttackQueued = false;
        _hitboxActive = false;

        DashPayload dashPayload = _core.Combat.CalculateMeleeDash(_core.Status.CurrentTarget);
        _dashVelocity = _core.Motor.CalculateDashVelocity(_core, dashPayload);
        _dashVelocity.Y = _core.Velocity.Y; // Inherit vertical momentum

        // Calculate the exact friction needed to stop the dash in the given duration
        // Friction = InitialVelocity / Duration
        float horizontalSpeed = new Vector3(_dashVelocity.X, 0, _dashVelocity.Z).Length();
        _dashFriction = dashPayload.Duration > 0 ? horizontalSpeed / dashPayload.Duration : 0f;

        _core.TriggerDashCam(_core.Status.CamDashDragFactor, _core.Status.CamDashDragDuration);
    }

	public override void ProcessState(float delta)
    {
        // 1. Decay Dash Velocity
        Vector3 horizontal = new Vector3(_dashVelocity.X, 0, _dashVelocity.Z);
        horizontal = horizontal.MoveToward(Vector3.Zero, _dashFriction * delta);

        float vertical = _dashVelocity.Y;
        if (!_core.IsOnFloor())
        {
            vertical += _core.Status.Gravity * delta;
        }

        _dashVelocity = new Vector3(horizontal.X, vertical, horizontal.Z);

        // 2. Apply Time Dilation (Scale DOWN)
        _core.Velocity = _dashVelocity * _core.Status.TimeScale;
        _core.MoveAndSlide();

        // 3. Restore Simulation Velocity (Scale UP)
        if (_core.Status.TimeScale > 0.001f) _dashVelocity = _core.Velocity / _core.Status.TimeScale;

        _status.ComboTimer += delta;

        // Check input immediately to act on in this frame
        if (_status.ComboTimer >= _currentAttack.ComboWindow.X && _status.ComboTimer <= _currentAttack.ComboWindow.Y)
        {
            if (_core.ActorInput.IsAttackRequested())
            {
                _nextAttackQueued = true;
            }
        }

        if (!_hitboxActive && _status.ComboTimer >= _currentAttack.Windup)
        {
            ActivateHitbox();
        }

        float endOfActive = _currentAttack.Windup + _currentAttack.Active;

        if (_status.ComboTimer >= endOfActive)
        {
            if (_hitboxActive)
            {
                DeactivateHitbox();
            }

            // Interrupt recovery phase for combo if possible
            if (_nextAttackQueued && IsNextAttackAvailable())
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

        float totalDuration = endOfActive + _currentAttack.Recovery;

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
        _hitboxActive = true;

        if (_core.SlashVfx != null) _core.SlashVfx.Visible = true;

        _core.Status.ActivePayload = _core.Combat.BuildAttackPayload(_currentAttack);
        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    private void DeactivateHitbox()
    {
        _hitboxActive = false;

        if (_core.SlashVfx != null) _core.SlashVfx.Visible = false;
        
        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;
    }

    private void TryAdvanceCombo()
    {
        if (_nextAttackQueued && IsNextAttackAvailable())
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
