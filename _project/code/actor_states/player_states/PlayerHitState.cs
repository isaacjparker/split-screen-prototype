using Godot;
using System;

public partial class PlayerHitState : ActorState
{
    private Vector3 _sourcePos;
    private float _knockbackPower;
    private Vector3 _currentVelocity;

    public PlayerHitState(ActorCore core, Vector3 sourcePos, float power) : base(core)
    {
        _sourcePos = sourcePos;
        _knockbackPower = power;
    }

    public override void EnterState()
    {
        GD.Print($"Entered HitState. Knockback Power: {_knockbackPower}");

        _core.HitFlash.PlayHitFlash();

        // Play Animation (optional)
        _currentVelocity = _core.Motor.CalculateKnockbackVelocity(_sourcePos, _knockbackPower);
        _currentVelocity.Y = _core.Velocity.Y; // Inherit vertical momentum (gravity)

        // Immediate application to ensure we don't lose a frame of movement if TimeScale is 1.0
        if (_core.Status.TimeScale > 0.001f)
        {
            _core.Velocity = _currentVelocity * _core.Status.TimeScale;
        }
    }

	public override void ProcessState(float delta)
    {
        // 1. Decay the "Simulation Velocity"
        // Using MoveToward acts like friction
        float friction = _core.Status.Deceleration; 
        
        // Apply friction only to horizontal movement
        Vector3 horizontal = new Vector3(_currentVelocity.X, 0, _currentVelocity.Z);
        horizontal = horizontal.MoveToward(Vector3.Zero, friction * delta);

        // Apply gravity to vertical movement
        float vertical = _currentVelocity.Y;
        if (!_core.IsOnFloor())
        {
            vertical += _core.Status.Gravity * delta;
        }

        _currentVelocity = new Vector3(horizontal.X, vertical, horizontal.Z);

        // 2. Apply Time Dilation to the Physics Velocity (Scale DOWN)
        _core.Velocity = _currentVelocity * _core.Status.TimeScale;
        
        _core.MoveAndSlide();

        // 3. Restore Simulation Velocity from Physics Result (Scale UP)
        if (_core.Status.TimeScale > 0.001f) _currentVelocity = _core.Velocity / _core.Status.TimeScale;

        // 4. Check exit condition (Horizontal only)
        if (horizontal.LengthSquared() < 0.1f)
        {
            ReturnToIdleMove();
        }
    }

    public override void ExitState()
    {
        // Ensure the physics body has the correct "Simulation Velocity" when leaving the state.
        // This prevents momentum loss if the next state reads _core.Velocity immediately.
        _core.Velocity = _currentVelocity;
    }

    private void ReturnToIdleMove()
    {
        _core.StateMachine.ChangeState(new PlayerIdleMoveState(_core));
    }


}
