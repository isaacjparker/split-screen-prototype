using Godot;
using System;

public partial class StatusModule : Node
{
    private ActorCore _core;

    [ExportGroup("Vitals")]
    [Export] public float MaxHealth = 100.0f;
    [Export] public float KnockbackResistance = 0.0f;

    [ExportGroup("Locomotion")]
    [Export] public float Gravity = -9.8f;
    [Export] public float MaxSpeed = 5.0f;
    [Export] public float Acceleration = 18.0f;
    [Export] public float Deceleration = 30.0f;
    [Export] public float TurnSpeed = 10.0f;

    [ExportGroup("Targeting")]
    [Export] public float MaxTargetRange = 20.0f;
    [Export] public float MaxTargetScanAngle = 180.0f;
    [Export] public string TargetGroup = "enemies";
    [Export] public CharacterBody3D CurrentTarget;

    [ExportGroup("Offense")]
    [Export] public float BaseDamage = 5.0f;
    [Export] public float KnockbackPower = 15.0f;
    [Export] public float HitStopDuration = 0.2f;
    [Export] public int ComboIndex = 0;
    [Export] public float ComboWindow = 0.6f;
    [Export] public WeaponData WeaponData;

    [ExportGroup("Dash")]
    [Export] public float MaxDashDistance = 5.0f;
    [Export] public float DashCone = 90.0f;
    [Export] public float DashDuration = 0.2f;
    [Export] public float DashStopOffset = 1.5f;
    [Export] public float DashWhiffDistance = 1.0f;
    [Export] public float DashWhiffDuration = 0.15f;

    [ExportGroup("Camera")]
    [Export] public float CamSmoothingAccel = 5.0f;
    [Export] public float CamCatchUpAccel = 12.0f;
    [Export] public float CamHitStopDuration = 0.5f;
    [Export] public float CamShakeMagnitude = 2.0f;
    [Export] public float CamShakeDuration = 1.0f;
    [Export] public float CamShakeNoiseFreq = 2.0f;
    [Export] public float CamDashDragFactor = 0.5f;
    [Export] public float CamDashDragDuration = 0.5f;

    public float CurrentHealth { get; private set; }

    public void Initialise(ActorCore core)
    {
        _core = core;
        CurrentHealth = MaxHealth;

        _core.HurtBox.AreaEntered += OnHurtBoxAreaEntered;
    }

    public override void _ExitTree()
    {
        _core.HurtBox.AreaEntered -= OnHurtBoxAreaEntered;
    }

    public event Action<float, float> OnHealthChanged;
    public event Action OnZeroHealth;
    public event Action<Vector3, float> OnKnockbackReceived;

    public void ApplyDamage(AttackPayload payload)
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth - payload.BaseDamage, 0.0f, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, payload.BaseDamage);

        float effectiveKnockback = payload.KnockbackPower - KnockbackResistance;

        if (effectiveKnockback > 0)
        {
            OnKnockbackReceived?.Invoke(payload.SourcePosition, effectiveKnockback);
        }

        if (CurrentHealth <= 0)
        {
            OnZeroHealth?.Invoke();
        }
    }
    public void ApplyHealing(float healAmount) 
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + healAmount, 0.0f, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, healAmount);
    }
    public void ModifySpeedAbsolute(float multiplier, float duration) { }
    public void ModifySpeedPercentage(float multiplier, float duration) { }


    public void OnHurtBoxAreaEntered(Area3D area)
    {
        if (area is HitBox hitBox)
        {
            ApplyDamage(hitBox.Payload);
        }
    }
}
