using Godot;
using System;
using System.Reflection.Metadata;

public partial class MotorModule : Node
{

    private const float MinTimescale = 0.001f;
    private const float MinSpeedThreshold = 0.01f;
    private const float MinDirectionSqLength = 0.001f;
    private const float MinDashDuration = 0.1f;
    private const float FloorSnapVelocity = -1f;
    private const float KinematicVelocityScale = 2.0f;

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
        Vector3 simVelocity = CalculateVelocity(_core.Velocity, inputDirection, maxSpeed, _core.IsOnFloor(), delta);

        ApplyScaledMovement(simVelocity);

        // Rotate
        // Uses simVelocity, not _core.Velocity, to avoid a one-frame lag
        if (inputDirection != Vector3.Zero)
        {
            Vector3 rotation = _core.Rotation;
            rotation.Y = GetTargetYaw(simVelocity, _core.Rotation.Y, delta);
            _core.Rotation = rotation;
        }
    }

    public void ProcessTargetingLocomotion(Vector3 inputDirection, CharacterBody3D target, float maxSpeed, float delta)
    {
        if (target == null) return;

        Vector3 simVelocity = CalculateVelocity(_core.Velocity, inputDirection, maxSpeed, _core.IsOnFloor(), delta);
        
        ApplyScaledMovement(simVelocity);

        // Rotate (Face Target)
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
            bool isSpeedingUp = signedSpeed < targetSpeed - MinSpeedThreshold;
            bool isSharpTurn = signedSpeed < 0f;

			if (isSharpTurn)
            {
                // Apply a sharper friction rate to make direction changes feel responsive
                accelRate = _status.Deceleration * _status.SharpTurnMultiplier;
            }
            else if (isSpeedingUp)
            {
                accelRate = _status.Acceleration;
            }
        }

        Vector3 targetVelocity = targetDirection * targetSpeed;

        horizontalVelocity = horizontalVelocity.MoveToward(targetVelocity, accelRate * delta);

        float currentY = currentVelocity.Y;
        if (isOnfloor)
        { 
			if (currentY < 0f) currentY = FloorSnapVelocity;
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
		if (flattened.LengthSquared() < MinDirectionSqLength) return currentRotationY;

        float targetYaw = Mathf.Atan2(-flattened.X, -flattened.Z);
        return Mathf.LerpAngle(currentRotationY, targetYaw, _status.TurnSpeed * delta);
    }

    public void StartDash(DashPayload dashPayload, bool snapRotation = true)
    {
        Vector3 direction = (dashPayload.TargetPosition - _core.GlobalPosition);
        direction.Y = 0;

        float distance = direction.Length() - dashPayload.StopOffset;
        if (distance < 0) distance = 0;

        direction = direction.LengthSquared() > MinDirectionSqLength
            ? direction.Normalized()
            : Vector3.Zero;

        if(snapRotation) SnapRotationTowards(direction);

        float duration = dashPayload.Duration > 0 ? dashPayload.Duration : MinDashDuration;
        // Dash decelerates linearly to zero, so average speed = initialSpeed / 2.
        // To cover full distance, initial speed must be doubled.
        float initialSpeed = (distance * 2f) / duration;

        dashPayload.DashVelocity = direction * initialSpeed;
        dashPayload.DashVelocity.Y = _core.Velocity.Y; // Inherit vertical momentum
        dashPayload.DashFriction = initialSpeed / duration;

        _status.CurrentDashPayload = dashPayload;
    }

    private void SnapRotationTowards(Vector3 direction)
    {
        if (direction.LengthSquared() <= MinDirectionSqLength) return;

        float targetYaw = Mathf.Atan2(-direction.X, -direction.Z);
        Vector3 currentRotation = _core.Rotation;
        currentRotation.Y = targetYaw;
        _core.Rotation = currentRotation;
    }

    public void ProcessDashMovement(float delta)
    {
        // DashPayload is a class - mutations write directly to stored instance
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
        if (_status.TimeScale > MinTimescale)
        {
            payload.DashVelocity = _core.Velocity / _status.TimeScale;
        }
    }

    // Returns the initial velocity vector for knockback
    public Vector3 CalculateKnockbackVelocity(Vector3 sourcePosition, float knockbackPower, float knockbackLift = 0f)
    {
        Vector3 direction = _core.GlobalPosition - sourcePosition;
        direction.Y = 0;
        direction = direction.Normalized();

        return new Vector3(direction.X * knockbackPower, knockbackLift, direction.Z * knockbackPower);
    }

    private void ApplyScaledMovement(Vector3 simVelocity)
    {
        _core.Velocity = simVelocity * _status.TimeScale;
        _core.MoveAndSlide();

        // MoveAndSlide() writes back to Velocity post-collision
        // Unscale here so next frame's simulation reads a clean, unscaled value -
        // otherwise slow-motion would compound across frames and bleed into movement speed.
        if (_status.TimeScale > MinTimescale) _core.Velocity /= _status.TimeScale;
    }
}
