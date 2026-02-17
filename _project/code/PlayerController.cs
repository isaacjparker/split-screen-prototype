using Godot;
using System;

public partial class PlayerController : CharacterBody3D
{
    // Gameplay Config
    [Export] public int PlayerSlot;
    [Export] private int _inputDeviceId;

    // Locomotion Config
    [Export] private float _gravity = -9.8f;
	[Export] private float _maxSpeed = 5.0f;
    [Export] private float _jumpVelocity = 5.0f;
    [Export] private float _acceleration = 18.0f;
	[Export] private float _deceleration = 30.0f;
	[Export] private float _turnSpeed = 10.0f;
	private const float VELOCITY_THRESHOLD = 0.1f;

	// Input Runtime
	private Vector2 _inputDir;
    private float _inputMagnitude;
    private bool _inputActive;

    // Input string cache
    private StringName _moveLeft;
    private StringName _moveRight;
    private StringName _moveUp;
    private StringName _moveDown;
    private StringName _startButton;


    public override void _PhysicsProcess(double delta)
	{
		float fDelta = (float)delta; // cast once as it's used in multiple places

		// Process Inputs
        _inputDir = Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);
		_inputMagnitude = _inputDir.Length();
		_inputActive = _inputDir != Vector2.Zero;

        // Calculate locomotion
        Vector3 currentVelocity = GetVelocity(fDelta);

        // Rotate actor in direction of movement
		if (_inputActive)
        	RotateActorToDirection(currentVelocity, fDelta);

		// Commit velocity
        Velocity = currentVelocity;
        MoveAndSlide();
	}

    private Vector3 GetVelocity(float delta)
    {
		// Current velocity, horizontal only
        Vector3 horizontalVelocity = new Vector3(Velocity.X, 0f, Velocity.Z);

		// Defaults (no input = decelerate)
        Vector3 targetDirection = Vector3.Zero;
        float targetSpeed = 0f;
        float accelRate = _deceleration;

		// Input -> target direction/speed + choose accel/decel
		// Signed speed: positive = moving with input, negative = moving against it.
        if (_inputActive)
        {
            targetDirection = new Vector3(_inputDir.X, 0f, _inputDir.Y).Normalized();
            targetSpeed = _inputMagnitude * _maxSpeed;

            float signedSpeed = horizontalVelocity.Dot(targetDirection);

            bool isSpeedingUp = signedSpeed < targetSpeed - 0.01f;
            bool isSharpTurn = signedSpeed < 0f;

			if (!isSharpTurn && isSpeedingUp)
			{
				accelRate = _acceleration;
			}
        }

		// Apply horizontal acceleration/deceleration
        Vector3 targetVelocity = targetDirection * targetSpeed;
        horizontalVelocity = horizontalVelocity.MoveToward(targetVelocity, accelRate * delta);

		// Vertical velocity (gravity / floor stick)
		float currentY = Velocity.Y;
		if (IsOnFloor())
		{
			if (currentY < 0f)
				currentY = -1f;
		}
		else
		{
			currentY += _gravity * delta;
		}

		// Combine horizontal + vertical
		Vector3 velocity = new(horizontalVelocity.X, currentY, horizontalVelocity.Z);
		return velocity;
    }

    private void RotateActorToDirection(Vector3 velocity, float delta)
	{ 
		// Remove Y from velocity
        Vector3 flattenedVelocity = new Vector3(velocity.X, 0f, velocity.Z);

		// Guard against micro velocity
		if (flattenedVelocity.LengthSquared() < VELOCITY_THRESHOLD * VELOCITY_THRESHOLD)
            return;

        // yaw toward velocity; Godot forward is -Z
        float targetYaw = Mathf.Atan2(-flattenedVelocity.X, -flattenedVelocity.Z);
		// Lerp only around Y (in radians)
		Vector3 rotation = Rotation;
		rotation.Y = Mathf.LerpAngle(rotation.Y, targetYaw, _turnSpeed * delta);
		Rotation = rotation;
	}

    public void AssignInputDevice(int deviceId)
    {
        _inputDeviceId = deviceId;
        _moveLeft = $"move_left_{deviceId}";
		_moveRight = $"move_right_{deviceId}";
		_moveUp = $"move_up_{deviceId}";
		_moveDown = $"move_down_{deviceId}";
		_startButton = $"start_{deviceId}";
    }
}
