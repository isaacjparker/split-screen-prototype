using Godot;
using System;
using System.ComponentModel;

public partial class PlayerDeathState : ActorState
{

    private Vector3 _sourcePos;
    private float _baseKnockback;

    private Vector3 _currentVelocity;
    private Vector3 _rotationAxis;
    private Basis _startBasis;
    private Basis _targetBasis;
    private float _rotationProgress;
    private float _rotationDuration;

    public PlayerDeathState(ActorCore core, Vector3 sourcePos, float knockbackPower) : base(core)
    {
        _sourcePos = sourcePos;
        _baseKnockback = knockbackPower;
    }

    public override void EnterState()
    {
        GD.Print("Player has died.");

		StatusModule status = _core.Status;
        _rotationDuration = status.DeathRotationDuration;

        _core.TriggerHitStop(status.DeathhitStopDuration, status.DeathHitStopFactor);

        // Build XZ knockback with fallback
        Vector3 toPlayer = _core.GlobalPosition - _sourcePos;
        toPlayer.Y = 0f;
        Vector3 horizontalDir;
        if (toPlayer.LengthSquared() > 0.001f)
            horizontalDir = toPlayer.Normalized();
        else
            horizontalDir = -_core.Transform.Basis.Z;
        
        // Rotation axis: perpendicular to knockback, parallel to ground.
        _rotationAxis = Vector3.Up.Cross(horizontalDir).Normalized();

        // Initial velocity: ampliied XZ knockback + vertical pop-up.
        float deathPower = _baseKnockback * status.DeathKnockbackMultiplier;
        Vector3 knockbackVel = _core.Motor.CalculateKnockbackVelocity(_sourcePos, deathPower);
        float popUpV = Mathf.Sqrt(2.0f * Mathf.Abs(status.Gravity) * status.DeathPopUpHeight);
        knockbackVel.Y = popUpV;
        _currentVelocity = knockbackVel;

        // Cache start and target rotation
        _startBasis = _core.Transform.Basis.Orthonormalized();
        _targetBasis = (new Basis(_rotationAxis, Mathf.Pi / 2.0f) * _startBasis).Orthonormalized();
        _rotationProgress = 0f;

    }

	public override void ProcessState(float delta)
    {
        // Decay simulation velocity (gravity + horizontal friction)
        Vector3 horizontal = new Vector3(_currentVelocity.X, 0f, _currentVelocity.Z);
        horizontal = horizontal.MoveToward(Vector3.Zero, _core.Status.Deceleration * 0.5f * delta);

        float vertical = _currentVelocity.Y;
        if (!_core.IsOnFloor())
            vertical += _core.Status.Gravity * delta;

        _currentVelocity = new Vector3(horizontal.X, vertical, horizontal.Z);

        // Apply time dilation to velocity, then restore simulation velocity
        _core.Velocity = _currentVelocity * _core.Status.TimeScale;
        _core.MoveAndSlide();
        if (_core.Status.TimeScale > 0.001f)
            _currentVelocity = _core.Velocity / _core.Status.TimeScale;

        // Rotate using same scaled delta
        if (_rotationProgress < _rotationDuration)
        {
            _rotationProgress += delta;
            float t = Mathf.Clamp(_rotationProgress / _rotationDuration, 0f, 1f);
            // Ease-out quad - quick initial pitch, gentle settle
            float easedT = 1f - Mathf.Pow(1f - t, 2f);
            _core.Basis = _startBasis.Slerp(_targetBasis, easedT);
        }
        else
        {
            _core.Basis = _targetBasis;
        }

        // Settle when grounded and rotation finished
        bool rotationDone = _rotationProgress >= _rotationDuration;
        bool slow = horizontal.LengthSquared() < 0.05f;

        if (rotationDone && _core.IsOnFloor() && slow)
            Settle();
    }

    public override void ExitState()
    {
        
    }

    private void Settle()
    {
        _currentVelocity = Vector3.Zero;
        _core.Velocity = Vector3.Zero;
        _core.Basis = _targetBasis;

        _core.SetHitBoxEnabled(false);
        _core.SetHurtBoxEnabled(false);
        _core.SetCollisionShapeEnabled(false);

        _core.StateMachine.ChangeState(new PlayerCorpseState(_core));
    }

    
}
