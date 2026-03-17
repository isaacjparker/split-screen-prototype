using Godot;
using System;

public partial class PlayerSM : StateMachine
{
	[Export] public int PlayerSlot;
	[Export] private int _inputDeviceId;

	// Input cache
	private StringName _moveLeft, _moveRight, _moveUp, _moveDown, _startButton, _targetButton, _meleeAttack;

	public override bool IsAttackRequested() => Input.IsActionJustPressed(_meleeAttack);
    public override bool IsTargetLockHeld() => Input.IsActionPressed(_targetButton);
    public override bool IsTargetLockRequested() => Input.IsActionJustPressed(_targetButton);

    public override void Initialise(ActorCore core)
    {
        base.Initialise(core);
        CurrentState = new PlayerIdleMoveState(_core);
        PreviousState = CurrentState;
        CurrentState?.EnterState();
    }

	public override Vector3 GetMovementDirection()
    {
        Vector2 inputVec = Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);
        return new Vector3(inputVec.X, 0, inputVec.Y);
    }

    public override ActorState CreateHitState(Vector3 sourcePos, float power)
    {
        return new PlayerHitState(_core, sourcePos, power);
    }
 
    public void AssignInputDevice(int deviceId)
    {
        _inputDeviceId = deviceId;
        _moveLeft = $"move_left_{deviceId}";
        _moveRight = $"move_right_{deviceId}";
        _moveUp = $"move_up_{deviceId}";
        _moveDown = $"move_down_{deviceId}";
        _startButton = $"start_{deviceId}";
        _targetButton = $"target_{deviceId}";
        _meleeAttack = $"melee_attack_{deviceId}";
    }
}
