using Godot;
using System;
using System.Reflection.Metadata.Ecma335;

public partial class StatusModule : Node
{
    private ActorCore _core;

    [ExportGroup("Vitals")]
    [Export] public float MaxHealth = 100.0f;
    [Export] public float KnockbackResistance = 0.0f;

    [ExportGroup("Visuals")]
    [Export] public Material BodyMaterial;
    [Export] public Material FaceMaterial;
    [Export] public Material HitFlashMaterial;

    [ExportGroup("Locomotion")]
    [Export] public float TimeScale = 1.0f;
    [Export] public float Gravity = -9.8f;
    [Export] public float MaxSpeed = 5.0f;
    [Export] public float Acceleration = 18.0f;
    [Export] public float Deceleration = 30.0f;
    [Export] public float TurnSpeed = 10.0f;
    [Export] public float SharpTurnMultiplier = 2.5f;

    [ExportGroup("Targeting")]
    [Export] public bool ManualTargeting = false;
    [Export] public float TargetingPollingRate = 0.1f;
    [Export] public float MaxTargetScanRange = 16.0f;
    [Export] public float MaxTargetScanAngle = 150.0f;
    [Export] public float MaxTargetLeashRange = 18.0f;
    [Export] public float MaxTargetSwapDifference = 2.0f;
    [Export] public Faction Faction = Faction.Dummy;
    [Export] public ActorCore CurrentTarget;
    [Export] public float ProximityWeight = 1.0f;
    [Export] public float FactionWeight = 1.0f;
    [Export] public float ThreatWeight = 1.0f;
    [Export] public float ThreatPerHit = 10.0f;
    [Export] public float ThreatDecayRate = 3.0f;

    [ExportGroup("Combat")]
    public int ComboIndex = 0;
    public float ComboTimer = 0f;
    public WeaponData EquippedWeapon;
    public BaseWeapon ActiveWeaponBehaviour;
    [Export] public WeaponData DefaultWeapon;
    public float HitStopTimer = 0f;
    public float HitStopFactor = 0f;
    public float HitFlashTimer = 0.1f;
    public float HitFlashGap = 0.05f;
    public AttackData CurrentAttack;
    public bool HitboxActive = false;
    public bool NextAttackQueued = false;
    public Sprite3D AttackVfxNode;

    [ExportGroup("Attack Dash")]
    [Export] public float MaxDashDistance = 5.0f;
    [Export] public float DashCone = 90.0f;
    [Export] public float DashDuration = 0.2f;
    [Export] public float DashStopOffset = 1.5f;
    [Export] public float DashWhiffDistance = 1.0f;
    [Export] public float DashWhiffDuration = 0.15f;

    [ExportGroup("Default Dash")]
    [Export] public float DefaultDashDistance = 5.0f;
    [Export] public float DefaultDashDuration = 0.2f;
    [Export] public float DefaultDashCooldown = 0.6f;
    public float DefaultDashCooldownTimer = 0f;
    public DashPayload CurrentDashPayload;

    [ExportGroup("Death")]
    [Export] public float DeathKnockbackMultiplier = 1.5f;
    [Export] public float DeathPopUpHeight = 0.5f;
    [Export] public float DeathRotationDuration = 0.6f;
    [Export] public float DeathhitStopDuration = 0.7f;
    [Export] public float DeathHitStopFactor = 80.0f;

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
    public AttackPayload ActivePayload; // Set by AttackingState
    public ThreatTable ThreatTable {get; private set;} = new ThreatTable();

    public bool IsAlive => CurrentHealth > 0f;

    public void Initialise(ActorCore core)
    {
        _core = core;
        CurrentHealth = MaxHealth;

        _core.HurtBox.AreaEntered += OnHurtBoxAreaEntered;
        
        if (_core.HitBox != null)
        {
            _core.HitBox.AreaEntered += OnHitBoxAreaEntered;
        }
    }

    public override void _ExitTree()
    {
        _core.HurtBox.AreaEntered -= OnHurtBoxAreaEntered;
        if (_core.HitBox != null) _core.HitBox.AreaEntered -= OnHitBoxAreaEntered;
    }

    public event Action<float, float> OnHealthChanged;
    public event Action<Vector3, float> OnKnockbackReceived;

    public void ProcessThreats(float delta)
    {
        ThreatTable.Decay(delta, ThreatDecayRate);
    }

    public void ApplyDamage(AttackPayload payload)
    {
        if (CurrentHealth <= 0) return;     // Don't apply damage to dead actor

        CurrentHealth = Mathf.Clamp(CurrentHealth - payload.BaseDamage, 0.0f, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, payload.BaseDamage);

        _core.TriggerHitStop(payload.HitStopDuration, payload.HitStopFactor);

        float effectiveKnockback = payload.KnockbackPower - KnockbackResistance;

        if (effectiveKnockback > 0)
        {
            OnKnockbackReceived?.Invoke(payload.SourcePosition, effectiveKnockback);
        }

        if (payload.SourceActor != null)
        {
            ThreatTable.AddThreat(payload.SourceActor, ThreatPerHit);
        }

        if (CurrentHealth <= 0)
        {
            _core.HandleDeathEvent(payload.SourcePosition, effectiveKnockback);
        }
    }
    public void ApplyHealing(float healAmount) 
    {
        CurrentHealth = Mathf.Clamp(CurrentHealth + healAmount, 0.0f, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, healAmount);
    }
    public void ModifySpeedAbsolute(float multiplier, float duration) { }
    public void ModifySpeedPercentage(float multiplier, float duration) { }


    // Fired when WE get hit by something
    public void OnHurtBoxAreaEntered(Area3D area)
    {
        // We assume the area is a HitBox from an attacker.
        // We resolve the attacker by checking the Owner of the area.
        if (area.Owner is ActorCore attacker)
        {
            // Prevent self-damage if collision layers aren't perfect
            if (attacker == _core) return;
            
            if (!FactionManager.IsHostile(attacker.Status.Faction, _core.Status.Faction)) return;

            ApplyDamage(attacker.Status.ActivePayload);
        }
    }

    // Fired when OUR HitBox hits something (like a HurtBox)
    public void OnHitBoxAreaEntered(Area3D area)
    {
        // Safety check: Don't freeze if we hit ourselves (e.g. our own HurtBox)
        if (area.Owner == _core) return;

        if (area.Owner is ActorCore target)
        {
            if (!FactionManager.IsHostile(_core.Status.Faction, target.Status.Faction)) return;
            AudioManager.Instance.CreateAudio(ActivePayload.ImpactAudio);
        }
        
        // We successfully hit a target. Trigger our own HitStop (Freeze).
        // Note: This assumes our HitBox only masks with HurtBoxes.
        _core.TriggerHitStop(ActivePayload.HitStopDuration, ActivePayload.HitStopFactor);
    }

    public void Reset()
    {
        CurrentHealth = MaxHealth;
        //ThreatTable.Clear();
        OnHealthChanged?.Invoke(CurrentHealth, 0f);
    }
}
