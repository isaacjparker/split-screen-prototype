using Godot;
using System;

public partial class AttackingState : ActorState
{

    private float _stateTimer = 0f;
    private AttackData _currentAttack;
    private bool _nextAttackQueued = false;
    private bool _hitboxActive = false;

    public AttackingState(ActorCore core) : base(core)
    {
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

        _stateTimer = 0f;
        _nextAttackQueued = false;
        _hitboxActive = false;

        DashPayload dashPayload = _core.Combat.CalculateMeleeDash(_core.Status.CurrentTarget);
        _core.Motor.Dash(_core, dashPayload);

        _core.TriggerDashCam(_core.Status.CamDashDragFactor, _core.Status.CamDashDragDuration);
    }

	public override void ProcessState(float delta)
    {
        _core.Motor.ProcessForcesLocomotion(delta);

        _stateTimer += delta;

        // Check input immediately to act on in this frame
        if (_stateTimer >= _currentAttack.ComboWindow.X && _stateTimer <= _currentAttack.ComboWindow.Y)
        {
            if (_core.ActorInput.IsAttackRequested())
            {
                _nextAttackQueued = true;
            }
        }

        if (!_hitboxActive && _stateTimer >= _currentAttack.Windup)
        {
            ActivateHitbox();
        }

        float endOfActive = _currentAttack.Windup + _currentAttack.Active;

        if (_stateTimer >= endOfActive)
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
                _core.Status.ComboIndex = 0;
                _core.StateMachine.ChangeState(new IdleMoveState(_core));
            }
        }

        float totalDuration = endOfActive + _currentAttack.Recovery;

        if (_stateTimer >= totalDuration)
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

        AttackPayload attackPayload = _core.Combat.BuildAttackPayload(_currentAttack);

        _core.HitBox.SetPayload(attackPayload);
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

            _core.StateMachine.ChangeState(new AttackingState(_core));
            return;
        }
        else
        {
            _core.Status.ComboIndex = 0;
            _core.StateMachine.ChangeState(new IdleMoveState(_core));
            return;
        }
    }

    private bool IsNextAttackAvailable()
    {
        return (_core.Status.ComboIndex + 1) < _core.Status.WeaponData.Attacks.Length;
    }
}
