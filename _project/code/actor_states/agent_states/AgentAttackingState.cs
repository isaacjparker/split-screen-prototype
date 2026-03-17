using Godot;
using System;

public partial class AgentAttackingState : ActorState
{
    public AgentAttackingState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        WeaponData weaponData = _core.Status.EquippedWeapon;

		_status.ComboIndex = 0;
		_status.CurrentAttack = weaponData.Attacks[0];

		_status.ComboTimer = 0f;
		_status.NextAttackQueued = false;
		_status.HitboxActive = false;

		_core.RaiseAttackStarted();
		_core.Motor.StartDash(_core.Combat.BuildMeleeDash(_core.Status.CurrentTarget));
		_core.TriggerDashCam(_core.Status.CamDashDragFactor, _core.Status.CamDashDragDuration);
    }

	public override void ProcessState(float delta)
    {
        _core.Motor.ProcessDashMovement(delta);
		_status.ComboTimer += delta;

		float endOfActive = _status.CurrentAttack.Windup + _status.CurrentAttack.Active;

		// Activate hitbox at windup end
		if (!_status.HitboxActive && _status.ComboTimer >= _status.CurrentAttack.Windup && _status.ComboTimer < endOfActive)
		{
			ActivateHitbox();
		}

		// Deactivate hitbox at end of active window
		if (_status.HitboxActive && _status.ComboTimer >= endOfActive)
		{
			DeactivateHitbox();
		}

		// Recovery complete - return to combat
		float totalDuration = endOfActive + _status.CurrentAttack.Recovery;
		if (_status.ComboTimer >= totalDuration)
		{
			ReturnToCombat();
			return;
		}
    }

    public override void ExitState()
    {
        DeactivateHitbox();
		_core.HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;

		_status.ComboIndex = 0;
		_core.RaiseAttackEnded();
    }

	private void ActivateHitbox()
	{
		_status.HitboxActive = true;
		_status.ActivePayload = _core.Combat.BuildAttackPayload(_status.CurrentAttack);
		_core.HitBox.ProcessMode = Node.ProcessModeEnum.Inherit;
		AudioManager.Instance.CreateAudio(_core.Status.ActivePayload.AttackAudio);

		_core.RaiseAttackActive();
	}

	private void DeactivateHitbox()
	{
		_status.HitboxActive = false;
		_core.HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;
	}

	private void ReturnToCombat()
	{
		if (_status.CurrentTarget != null && Node.IsInstanceValid(_status.CurrentTarget))
		{
			_core.StateMachine.ChangeState(new AgentCombatState(_core));
		}
		else
		{
			_status.CurrentTarget = null;
			_core.StateMachine.ChangeState(new AgentIdleState(_core));
		}
	}
}
