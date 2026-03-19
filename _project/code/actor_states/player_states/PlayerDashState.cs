using Godot;
using System;

public partial class PlayerDashState : ActorState
{
	private readonly ActorState _previousState;
	private float _dashTimer;
	private float _dashDuration;

    public PlayerDashState(ActorCore core, ActorState previousState) : base(core)
    {
		_previousState = previousState ?? new PlayerIdleMoveState(core);
    }

    public override void EnterState()
    {
        Vector3 inputDir = _core.StateMachine.GetMovementDirection();
		DashPayload payload = _core.Combat.BuildDefaultDash(inputDir);
		_dashDuration = payload.Duration;
		_dashTimer = 0f;

		_core.Motor.StartDash(payload);
		_core.TriggerDashCam(_core.Status.CamDashDragFactor, _core.Status.CamDashDragDuration);

		_status.DefaultDashCooldownTimer = _status.DefaultDashCooldown;

		// Invulnerability during dash
		_core.SetHurtBoxEnabled(false);
    }

	public override void ProcessState(float delta)
    {
        _core.Motor.ProcessDashMovement(delta);
		_dashTimer += delta;

		if (_dashTimer >= _dashDuration)
		{
			ReturnToPreviousState();
		}
    }

    public override void ExitState()
    {
        _core.SetHurtBoxEnabled(true);
    }

	private void ReturnToPreviousState()
	{
		if (_previousState is PlayerTargetingState)
		{
			if (_core.Status.CurrentTarget != null && GodotObject.IsInstanceValid(_core.Status.CurrentTarget))
			{
				_core.StateMachine.ChangeState(new PlayerTargetingState(_core));
				return;
			}
		}

		_core.StateMachine.ChangeState(new PlayerIdleMoveState(_core));
	}
}
