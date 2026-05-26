using Godot;
using System;

public partial class PlayerSM : StateMachine
{
	[Export] public int PlayerSlot;
	[Export] private int _inputDeviceId;

	// Input cache
	private StringName _moveLeft, _moveRight, _moveUp, _moveDown, _startButton, _targetButton, _meleeAttack, _interactButton;

    public override bool IsAttackRequested()
    {
        if (IsTextInputFocused()) return false;
        return Input.IsActionJustPressed(_meleeAttack);
    }

    public override bool IsTargetLockHeld()
    {
        if (IsTextInputFocused()) return false;
        return Input.IsActionPressed(_targetButton);
    }

    public override bool IsTargetLockRequested()
    {
        if (IsTextInputFocused()) return false;
        return Input.IsActionJustPressed(_targetButton);
    }

    public override bool IsInteractRequested()
    {
        if (IsTextInputFocused()) return false;
        return Input.IsActionJustPressed(_interactButton);
    }

    public override void Initialise(ActorCore core)
    {
        base.Initialise(core);
        CurrentState = new PlayerIdleMoveState(_core);
        PreviousState = CurrentState;
        CurrentState?.EnterState();
    }

    private bool IsTextInputFocused()
    {
        Control focusOwner = GetViewport().GuiGetFocusOwner();
        if (focusOwner == null) return false;
        if (focusOwner is LineEdit) return true;
        if (focusOwner is TextEdit) return true;
        return false;
    }

    public override string GetInteractButtonName()
    {
        return "A";
    }

	public override Vector3 GetMovementDirection()
    {
        if (IsTextInputFocused()) return Vector3.Zero;
        Vector2 inputVec = Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);
        return new Vector3(inputVec.X, 0, inputVec.Y);
    }

    public override ActorState CreateHitState(Vector3 sourcePos, float power)
    {
        return new PlayerHitState(_core, sourcePos, power);
    }

    public override ActorState CreateDeathState(Vector3 sourcePos, float knockbackPower)
    {
        return new PlayerDeathState(_core, sourcePos, knockbackPower);
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
        _interactButton = $"interact_{deviceId}";
    }
}
