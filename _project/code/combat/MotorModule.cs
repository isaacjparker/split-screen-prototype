using Godot;
using System;

public partial class MotorModule : Node
{
	private CharacterBody3D _core;

    // TODO: Remove and replace with Status reference
    private float _acceleration;
    private float _deceleration;
    private float _turnSpeed;
    private float _gravity;

    // TODO: Change CharacterBody3D to ActorCore
    // and remove Initialise parameters (other than ActorCore)
    // Grab reference to Status and store in _status.
    public void Initialise(CharacterBody3D core, float accel, float decel, float turnSpeed, float gravity)
    { 
        _core = core;
        _acceleration = accel;
        _deceleration = decel;
        _turnSpeed = turnSpeed;
        _gravity = gravity;
    }

    // ------------------------------------------------------------------------
    // Movement Logic
    // ------------------------------------------------------------------------
    public void ProcessLocomotion(Vector3 inputDirection, float maxSpeed, float delta)
    {
        // 1. Calculate Velocity
        _core.Velocity = CalculateVelocity(_core.Velocity, inputDirection, maxSpeed, _core.IsOnFloor(), delta);

        // 2. Rotate
        if (inputDirection != Vector3.Zero)
        {
            Vector3 rotation = _core.Rotation;
            rotation.Y = GetTargetYaw(_core.Velocity, _core.Rotation.Y, delta);
            _core.Rotation = rotation;
        }

        // 3. Apply
        _core.MoveAndSlide();
    }

    public void ProcessTargetingLocomotion(Vector3 inputDirection, CharacterBody3D target, float maxSpeed, float delta)
    {
        if (target == null) return;

        // 1. Move
        _core.Velocity = CalculateVelocity(_core.Velocity, inputDirection, maxSpeed, _core.IsOnFloor(), delta);
        _core.MoveAndSlide();

        // 2. Rotate (Face Target)
        Vector3 toTarget = (target.GlobalPosition - _core.GlobalPosition).Normalized();
        toTarget.Y = 0;

        float targetYaw = Mathf.Atan2(-toTarget.X, -toTarget.Z);
        Vector3 rot = _core.Rotation;
        rot.Y = Mathf.LerpAngle(rot.Y, targetYaw, _turnSpeed * delta);
        _core.Rotation = rot;
    }

	public Vector3 CalculateVelocity(Vector3 currentVelocity, Vector3 inputDir, float maxSpeed, bool isOnfloor, float delta)
	{
        Vector3 horizontalVelocity = new Vector3(currentVelocity.X, 0f, currentVelocity.Z);

        Vector3 targetDirection = inputDir.Normalized();
        float targetSpeed = inputDir.Length() * maxSpeed;
        float accelRate = _deceleration;

        if (inputDir != Vector3.Zero)
        {
            float signedSpeed = horizontalVelocity.Dot(targetDirection);
            bool isSpeedingUp = signedSpeed < targetSpeed - 0.01f;
            bool isSharpTurn = signedSpeed < 0f;

			if (!isSharpTurn && isSpeedingUp) accelRate = _acceleration;
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
            currentY += _gravity * delta;
        }

        return new Vector3(horizontalVelocity.X, currentY, horizontalVelocity.Z);
    }

    public float GetTargetYaw(Vector3 velocity, float currentRotationY, float delta)
    {
        Vector3 flattened = new Vector3(velocity.X, 0f, velocity.Z);
		if (flattened.LengthSquared() < 0.01f) return currentRotationY;

        float targetYaw = Mathf.Atan2(-flattened.X, -flattened.Z);
        return Mathf.LerpAngle(currentRotationY, targetYaw, _turnSpeed * delta);
    }

    public Tween Lunge(CharacterBody3D actor, Vector3 targetPosition, float duration, float proximityOffset)
    {
        // Calculate distance and direction
        Vector3 direction = (targetPosition - actor.GlobalPosition);
        direction.Y = 0;

        float distance = direction.Length() - proximityOffset;

        if (distance <= 0)
        {
            actor.Velocity = new Vector3(0, actor.Velocity.Y, 0);
            return actor.CreateTween(); // dummy tween
        }

        direction = direction.Normalized();

        float initialSpeed = (distance * 4.0f) / duration;

        actor.Velocity = new Vector3(direction.X * initialSpeed, actor.Velocity.Y, direction.Z * initialSpeed);


        // create tween through the actor's tree
        Tween tween = actor.CreateTween();
        tween.SetParallel(true);

        // Set transition for a "snappy" feel: Easeout Quad or Cubic is good for lunges
        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        // Tween the global position
        tween.TweenProperty(actor, "velocity:x", 0f, duration);
        tween.TweenProperty(actor, "velocity:z", 0f, duration);

        return tween;
    }

    public Tween ApplyKnockback(Vector3 sourcePosition, float knockbackPower)
    {
        Vector3 direction = (_core.GlobalPosition - sourcePosition);
        direction.Y = 0;
        direction = direction.Normalized();

        _core.Velocity = direction * knockbackPower;

        float duration = Mathf.Clamp(knockbackPower * 0.05f, 0.1f, 1.0f);

        Tween tween = _core.CreateTween();

        tween.SetParallel(true);

        tween.SetTrans(Tween.TransitionType.Cubic);
        tween.SetEase(Tween.EaseType.Out);

        tween.TweenProperty(_core, "velocity:x", 0f, duration);
        tween.TweenProperty(_core, "velocity:z", 0f, duration);

        return tween;
    }

    public void ProcessForcesLocomotion(float delta)
    {
        if (!_core.IsOnFloor())
        {
            Vector3 vel = _core.Velocity;
            vel.Y += _gravity * delta;
            _core.Velocity = vel;
        }

        _core.MoveAndSlide();
    }
}
