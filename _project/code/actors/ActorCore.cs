using Godot;
using System;


public partial class ActorCore : CharacterBody3D
{

    [ExportGroup("Components")]
    [Export] public MeshInstance3D BodyMesh;
    [Export] public MeshInstance3D FaceMesh;
    [Export] public Area3D HitBox;
    [Export] public Area3D HurtBox;
    

    //public InputModule ActorInput { get; private set; }
    public StatusModule Status { get; private set; }
    public MotorModule Motor { get; private set; }
    public CombatModule Combat { get; private set; }
    public EquipmentModule Equipment {get; private set;}
    public HitFlash HitFlash {get; private set;}
    public StateMachine StateMachine { get; private set; }

    public event Action<float, float> OnCameraShake;
    public event Action<float> OnHitStop;
    public event Action<float, float> OnDash;
    public event Action<ActorCore> OnAttackStarted;
    public event Action<ActorCore> OnAttackActive;
    public event Action<ActorCore> OnAttackEnded;
    public event Action<ActorCore> OnDeath;



    public override void _Ready()
    {
        //ActorInput = GetNode<InputModule>("InputModule");
        Status = GetNode<StatusModule>("StatusModule");
        Motor = GetNode<MotorModule>("MotorModule");
        Combat = GetNode<CombatModule>("CombatModule");
        Equipment = GetNode<EquipmentModule>("EquipmentModule");
        HitFlash = GetNode<HitFlash>("HitFlash");
        StateMachine = GetNode<StateMachine>("StateMachine");

        //if (ActorInput == null)
        //{
            //GD.PrintErr("ActorCore: ActorInput not found.");
            //return;
        //}

        if (Status == null)
        {
            GD.PrintErr("ActorCore: StatusModule not found.");
            return;
        }

        if (Motor == null)
        {
            GD.PrintErr("ActorCore: MotorModule not found.");
            return;
        }

        if (Combat == null)
        {
            GD.PrintErr("ActorCore: CombatModule not found.");
            return;
        }

        if (Equipment == null)
        {
            GD.PrintErr("ActorCore: EquipmentModule not found.");
            return;
        }

        if (HitFlash == null)
        {
            GD.PrintErr("ActorCore: HitFlash not found.");
            return;
        }

        if (StateMachine == null)
        {
            GD.PrintErr("ActorCore: StateMachine not found.");
            return;
        }

        //ActorInput.Initialise(this);
        Status.Initialise(this);
        Motor.Initialise(this);
        Combat.Initialise(this);
        Equipment.Initialise(this);
        HitFlash.Initialise(this);
        StateMachine.Initialise(this);

        if (HitBox != null && GodotObject.IsInstanceValid(HitBox))
        { 
            if (HitBox.ProcessMode != Node.ProcessModeEnum.Disabled)
            { 
                SetHitBoxEnabled(false);
            }
        }

        if (Status.DefaultWeapon != null)
        {
            Equipment.EquipWeapon(Status.DefaultWeapon);
        }
        

        Status.OnKnockbackReceived += HandleKnockbackEvent;

        // Visuals
        if (Status.BodyMaterial != null)
            BodyMesh.SetSurfaceOverrideMaterial(0, Status.BodyMaterial);

        if (Status.FaceMaterial != null)
            FaceMesh.SetSurfaceOverrideMaterial(0, Status.FaceMaterial);
    }

    public override void _Process(double delta)
    {
        float fDelta = (float)delta;

        if (Status.HitStopTimer > 0)
        {
            Status.HitStopTimer -= fDelta;
        }

        Status.ProcessThreats(fDelta);
    }

    public override void _PhysicsProcess(double delta)
    {
        float fDelta = (float)delta;

        if (Status.HitStopTimer > 0)
        {
            // Factor 100 = 100% stop (0.0 scale). Factor 0 = 0% stop (1.0 scale).
            Status.TimeScale = 1.0f - Mathf.Clamp(Status.HitStopFactor / 100.0f, 0.0f, 1.0f);
            StateMachine.ProcessState(fDelta * Status.TimeScale);
        }
        else
        {
            Status.TimeScale = 1.0f;
            StateMachine.ProcessState(fDelta);
        }

        if (Status.DefaultDashCooldownTimer > 0f)
            Status.DefaultDashCooldownTimer -= fDelta;
    }

    public override void _ExitTree()
    {
        Status.OnKnockbackReceived -= HandleKnockbackEvent;
    }

    public void HandleKnockbackEvent(Vector3 sourcePos, float power)
    {
        StateMachine.ChangeState(StateMachine.CreateHitState(sourcePos, power));
    }

    public void HandleDeathEvent()
    {
        StateMachine.ChangeState(StateMachine.CreateDeathState());
        OnDeath?.Invoke(this);
    }

    public void TriggerDashCam(float dragFactor, float duration)
    {
        OnDash?.Invoke(dragFactor, duration);
    }

    public void TriggerHitStop(float hitStopDuration, float hitStopFactor)
    {
        float camShakeMagnitude = Status.CurrentAttack?.CamShakeMagnitude ?? Status.CamShakeMagnitude;
        
        Status.HitStopTimer = hitStopDuration;
        Status.HitStopFactor = hitStopFactor;
        OnCameraShake?.Invoke(Status.CamShakeDuration, camShakeMagnitude);
        OnHitStop?.Invoke(hitStopDuration);
    }

    public void SetHitBoxEnabled(bool enabled)
    {
        if (HitBox != null)
            HitBox.SetDeferred("process_mode", (int)(enabled ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled));
    }

    public void SetHurtBoxEnabled(bool enabled)
    {
        if (HurtBox != null)
            HurtBox.SetDeferred("process_mode", (int)(enabled ? Node.ProcessModeEnum.Inherit : Node.ProcessModeEnum.Disabled));
    }

    public void RaiseAttackStarted() => OnAttackStarted?.Invoke(this);
    public void RaiseAttackActive()  => OnAttackActive?.Invoke(this);
    public void RaiseAttackEnded()   => OnAttackEnded?.Invoke(this);
}
