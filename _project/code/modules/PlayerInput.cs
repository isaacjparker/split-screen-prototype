using Godot;
using System;

public partial class PlayerInput : InputModule
{
    [Export] public int PlayerSlot;
	[Export] private int _inputDeviceId;

	// Input cache
    private StringName _moveLeft, _moveRight, _moveUp, _moveDown, _startButton, _targetButton, _meleeAttack;

	public override bool IsAttackRequested() => Input.IsActionJustPressed(_meleeAttack);
    public override bool IsTargetLockHeld() => Input.IsActionPressed(_targetButton);          // For holding (strafing)
    public override bool IsTargetLockRequested() => Input.IsActionJustPressed(_targetButton);  // For scanning -> state change

    public override Vector3 GetMovementDirection()
    { 
        Vector2 inputVec = Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);
        return new Vector3(inputVec.X, 0, inputVec.Y);
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
