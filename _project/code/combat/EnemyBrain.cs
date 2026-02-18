using Godot;
using System;

public partial class EnemyBrain : CharacterBody3D
{
    [Export] private Area3D HurtBox;

    [ExportGroup("Locomotion")]
    [Export] private float _gravity = -9.8f;
    [Export] public float MaxSpeed = 5.0f;
    [Export] private float _acceleration = 18.0f;
    [Export] private float _deceleration = 30.0f;
    [Export] private float _turnSpeed = 10.0f;

    public MotorModule Motor { get; private set; }

    private bool _isKnockedBack = false;
    private Tween _activeKnockbackTween;

    public override void _Ready()
    {
        if (HurtBox == null)
        {
            GD.PrintErr("Enemy missing HurtBox reference.");
            return;
        }

        Motor = GetNode<MotorModule>("MotorModule");

        Motor.Initialise(this, _acceleration, _deceleration, _turnSpeed, _gravity);

        HurtBox.AreaEntered += OnHurtBoxAreaEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        float fDelta = (float)delta;

        if (_isKnockedBack)
        {
            Motor.ProcessForcesLocomotion(fDelta);
        }
        else
        {
            // Still call locomotion to process gravity while standing still
            Motor.ProcessLocomotion(Vector3.Zero, 0f, fDelta);
        }
    }

    public override void _ExitTree()
    {
        if (HurtBox != null)
        { 
            HurtBox.AreaEntered -= OnHurtBoxAreaEntered;
        }
        
        if (_activeKnockbackTween != null)
        {
            _activeKnockbackTween.Finished -= OnKnockbackFinished;
            _activeKnockbackTween.Kill(); // Force the engine to stop the animation
        }
    }

    private void OnHurtBoxAreaEntered(Area3D area)
    {
        if (area is HitBox hitBox)
        {
            AttackPayload payload = hitBox.Payload;

            GD.Print($"Hit! Took {payload.BaseDamage} damage. Knockback: {payload.KnockbackPower}");

            _isKnockedBack = true;

            if (_activeKnockbackTween != null && _activeKnockbackTween.IsValid())
{
            _activeKnockbackTween.Finished -= OnKnockbackFinished;
            _activeKnockbackTween.Kill(); // Kill the old momentum so the new hit takes over cleanly
}

            _activeKnockbackTween = Motor.ApplyKnockback(payload.SourcePosition, payload.KnockbackPower);
            _activeKnockbackTween.Finished += OnKnockbackFinished;
        }
    }

    private void OnKnockbackFinished()
    {
        _isKnockedBack = false;
        GD.Print("Enemy recovered from knockback!");

        if (_activeKnockbackTween != null)
        {
            _activeKnockbackTween.Finished -= OnKnockbackFinished;
            
            // Null it out so we don't accidentally hold a reference to a dead Tween
            _activeKnockbackTween = null; 
        }
    }
}
