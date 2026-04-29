using Godot;
using System;

public partial class MagnetDropModule : Node3D
{
    [Export] public float Value = 1f;
    [Export] public float Lifetime = 8f;
    [Export] public float AttractRadius = 4f;
    [Export] public float AbsorbRadius = 0.3f;
    [Export] public float AttractAcceleration = 25f;
    [Export] public float MaxAttractSpeed = 20f;
    [Export] public float Gravity = -9.8f;
    [Export] public float GroundY = 0.1f;
    [Export] public float GroundFriction = 8f;
    [Export] public float MagnetDelay = 1.5f;


    private Vector3 _velocity;
    private MagnetTargetModule _attractTarget;
    private float _lifetimeTimer;
    private bool _grounded;
    private float _magnetDelayTimer;

    public event Action<MagnetDropModule> OnAbsorbed;

    public void Launch(Vector3 initialVelocity)
    {
        _velocity = initialVelocity;
        _grounded = false;
        _lifetimeTimer = Lifetime;
        _magnetDelayTimer = MagnetDelay;
    }

    public override void _PhysicsProcess(double delta)
    {
        float fDelta = (float)delta;

        _lifetimeTimer -= fDelta;
        _magnetDelayTimer -= fDelta;
        if (_lifetimeTimer <= 0f)
        {
            QueueFree();
            return;
        }

        // Drop attract target if it's gone or deactivated
        if (_attractTarget != null && (!Node.IsInstanceValid(_attractTarget) || !_attractTarget.Active))
        {
            _attractTarget = null;
        }

        bool canMagnetise = _grounded && _magnetDelayTimer <= 0f;

        // Acquire if we don't have one
        if (canMagnetise && _attractTarget == null)
        {
            _attractTarget = FindNearestTargetInRange();
        }

        if (_attractTarget != null)
        {
            Vector3 toTarget = _attractTarget.GlobalPosition - GlobalPosition;
            float distance = toTarget.Length();

            if (distance <= AbsorbRadius)
            {
                Absorb();
                return;
            }

            Vector3 dir = toTarget.Normalized();
            _velocity = _velocity.MoveToward(dir * MaxAttractSpeed, AttractAcceleration * fDelta);
        }
        else
        {
            if (!_grounded)
            {
                _velocity.Y += Gravity * fDelta;
            }
            else
            {
                Vector3 horizontal = new Vector3(_velocity.X, 0f, _velocity.Z);
                horizontal = horizontal.MoveToward(Vector3.Zero, GroundFriction * fDelta);
                _velocity = new Vector3(horizontal.X, 0f, horizontal.Z);
            }
        }

        Vector3 newPos = GlobalPosition + _velocity * fDelta;

        if (_attractTarget == null && newPos.Y <= GroundY)
        {
            newPos.Y = GroundY;
            if (_velocity.Y < 0f) _velocity.Y = 0f;
            _grounded = true;
        }

        GlobalPosition = newPos;
    }

    private MagnetTargetModule FindNearestTargetInRange()
    {
        MagnetTargetModule nearest = null;
        float nearestDist = AttractRadius;

        foreach (MagnetTargetModule target in MagnetTargetRegistry.GetAll())
        {
            if (!target.Active) continue;
            if (!Node.IsInstanceValid(target)) continue;

            float dist = target.GlobalPosition.DistanceTo(GlobalPosition);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = target;
            }
        }

        return nearest;
    }

    private void Absorb()
    {
        _attractTarget?.HandleAbsorb(this);
        OnAbsorbed?.Invoke(this);
        QueueFree();
    }
}
