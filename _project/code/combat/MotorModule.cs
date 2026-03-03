using Godot;
using System;

public partial class MotorModule : Node
{
	private ActorCore _core;
    private StatusModule _status;

    public void Initialise(ActorCore core)
    { 
        _core = core;
        _status = _core.Status;
    }

    // ------------------------------------------------------------------------
    // Movement Logic
    // ------------------------------------------------------------------------
    public void ProcessLocomotion(Vector3 inputDirection, float maxSpeed, float delta)
    {
        // 1. Calculate Simulation Velocity (Unscaled)
        // We use _core.Velocity as the storage for Simulation Velocity between frames
        Vector3 simVelocity = CalculateVelocity(_core.Velocity, inputDirection, maxSpeed, _core.IsOnFloor(), delta);

        // 2. Rotate
        if (inputDirection != Vector3.Zero)
        {
            Vector3 rotation = _core.Rotation;
            rotation.Y = GetTargetYaw(_core.Velocity, _core.Rotation.Y, delta);
            _core.Rotation = rotation;
        }

        // 3. Apply Time Dilation & Move
        _core.Velocity = simVelocity * _status.TimeScale;
        _core.MoveAndSlide();

        // 4. Restore Simulation Velocity
        if (_status.TimeScale > 0.001f) _core.Velocity /= _status.TimeScale;
    }

    public void ProcessTargetingLocomotion(Vector3 inputDirection, CharacterBody3D target, float maxSpeed, float delta)
    {
        if (target == null) return;

        // 1. Move (Scale -> Move -> Unscale)
        Vector3 simVelocity = CalculateVelocity(_core.Velocity, inputDirection, maxSpeed, _core.IsOnFloor(), delta);
        
        _core.Velocity = simVelocity * _status.TimeScale;
        _core.MoveAndSlide();
        if (_status.TimeScale > 0.001f) _core.Velocity /= _status.TimeScale;

        // 2. Rotate (Face Target)
        Vector3 toTarget = (target.GlobalPosition - _core.GlobalPosition).Normalized();
        toTarget.Y = 0;

        float targetYaw = Mathf.Atan2(-toTarget.X, -toTarget.Z);
        Vector3 rot = _core.Rotation;
        rot.Y = Mathf.LerpAngle(rot.Y, targetYaw, _status.TurnSpeed * delta);
        _core.Rotation = rot;
    }

	public Vector3 CalculateVelocity(Vector3 currentVelocity, Vector3 inputDir, float maxSpeed, bool isOnfloor, float delta)
	{
        Vector3 horizontalVelocity = new Vector3(currentVelocity.X, 0f, currentVelocity.Z);

        Vector3 targetDirection = inputDir.Normalized();
        float targetSpeed = inputDir.Length() * maxSpeed;
        float accelRate = _status.Deceleration;

        if (inputDir != Vector3.Zero)
        {
            float signedSpeed = horizontalVelocity.Dot(targetDirection);
            bool isSpeedingUp = signedSpeed < targetSpeed - 0.01f;
            bool isSharpTurn = signedSpeed < 0f;

			if (!isSharpTurn && isSpeedingUp) accelRate = _status.Acceleration;
        }

        Vector3 targetVelocity = targetDirection * targetSpeed;

        horizontalVelocity = horizontalVelocity.MoveToward(targetVelocity, accelRate * delta);

        float currentY = currentVelocity.Y;
        if (isOnfloor)
        { 
			if (currentY < 0f) currentY = -1f;
        }
        else
        {
            currentY += _status.Gravity * delta;
        }

        return new Vector3(horizontalVelocity.X, currentY, horizontalVelocity.Z);
    }

    public float GetTargetYaw(Vector3 velocity, float currentRotationY, float delta)
    {
        Vector3 flattened = new Vector3(velocity.X, 0f, velocity.Z);
		if (flattened.LengthSquared() < 0.01f) return currentRotationY;

        float targetYaw = Mathf.Atan2(-flattened.X, -flattened.Z);
        return Mathf.LerpAngle(currentRotationY, targetYaw, _status.TurnSpeed * delta);
    }

    public void StartDash(DashPayload dashPayload)
    {
        // 1. Calculate Direction & Distance (Initial Snapshot)
        Vector3 direction = (dashPayload.TargetPosition - _core.GlobalPosition);
        direction.Y = 0;

        float distance = direction.Length() - dashPayload.StopOffset;
        if (distance < 0) distance = 0;

        // 2. Rotate Actor
        if (direction.LengthSquared() > 0.001f)
        {
            direction = direction.Normalized();
            float targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
            Vector3 currentRotation = _core.Rotation;
            currentRotation.Y = targetYaw;
            _core.Rotation = currentRotation;
        }
        else
        {
            direction = Vector3.Zero;
        }

        // 3. Calculate Initial Velocity (Kinematic: V = 2 * D / T)
        float duration = dashPayload.Duration > 0 ? dashPayload.Duration : 0.1f;
        float initialSpeed = (distance * 2.0f) / duration;

        dashPayload.DashVelocity = direction * initialSpeed;
        dashPayload.DashVelocity.Y = _core.Velocity.Y; // Inherit vertical momentum

        // 4. Calculate Friction (F = V / T)
        dashPayload.DashFriction = initialSpeed / duration;

        // 5. Store in Status
        _status.CurrentDashPayload = dashPayload;
    }

    public void ProcessDashMovement(float delta)
    {
        DashPayload payload = _status.CurrentDashPayload;

        // 1. Decay Dash Velocity (Horizontal Friction)
        Vector3 horizontal = new Vector3(payload.DashVelocity.X, 0, payload.DashVelocity.Z);
        horizontal = horizontal.MoveToward(Vector3.Zero, payload.DashFriction * delta);

        // 2. Apply Gravity (Vertical)
        float vertical = payload.DashVelocity.Y;
        if (!_core.IsOnFloor())
        {
            vertical += _status.Gravity * delta;
        }

        payload.DashVelocity = new Vector3(horizontal.X, vertical, horizontal.Z);

        // 3. Apply Time Dilation & Move
        _core.Velocity = payload.DashVelocity * _status.TimeScale;
        _core.MoveAndSlide();

        // 4. Restore Simulation Velocity (Handle Collisions)
        if (_status.TimeScale > 0.001f)
        {
            payload.DashVelocity = _core.Velocity / _status.TimeScale;
        }

        _status.CurrentDashPayload = payload;
    }

    // Returns the initial velocity vector for knockback
    public Vector3 CalculateKnockbackVelocity(Vector3 sourcePosition, float knockbackPower)
    {
        Vector3 direction = (_core.GlobalPosition - sourcePosition);
        direction.Y = 0;
        direction = direction.Normalized();

        return direction * knockbackPower;
    }
}
