using Godot;
using System;


public partial class ActorCore : CharacterBody3D
{

    [ExportGroup("Components")]
    [Export] public MeshInstance3D BodyMesh;
    [Export] public Area3D HitBox;
    [Export] public Area3D HurtBox;
    [Export] public Sprite3D SlashVfx;
    [Export] public Material FlashMaterial;
    

    public InputModule ActorInput { get; private set; }
    public StatusModule Status { get; private set; }
    public MotorModule Motor { get; private set; }
    public CombatModule Combat { get; private set; }
    public VFXModule VFX {get; private set;}
    public StateMachine StateMachine { get; private set; }

    public event Action<float, float> OnCameraShake;
    public event Action<float> OnHitStop;
    public event Action<float, float> OnDash;


    public override void _Ready()
    {
        ActorInput = GetNode<InputModule>("InputModule");
        Status = GetNode<StatusModule>("StatusModule");
        Motor = GetNode<MotorModule>("MotorModule");
        Combat = GetNode<CombatModule>("CombatModule");
        VFX = GetNode<VFXModule>("VFXModule");
        StateMachine = GetNode<StateMachine>("StateMachine");

        if (ActorInput == null)
        {
            GD.PrintErr("ActorCore: ActorInput not found.");
            return;
        }

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

        if (VFX == null)
        {
            GD.PrintErr("ActorCore: VFXModule not found.");
            return;
        }

        if (StateMachine == null)
        {
            GD.PrintErr("ActorCore: StateMachine not found.");
            return;
        }

        Status.Initialise(this);
        Motor.Initialise(this);
        Combat.Initialise(this);
        VFX.Initialise(this);
        StateMachine.Initialise(new IdleMoveState(this));

        if (HitBox != null && GodotObject.IsInstanceValid(HitBox))
        { 
            if (HitBox.ProcessMode != Node.ProcessModeEnum.Disabled)
            { 
                HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;
            }
        }

        Status.OnKnockbackReceived += HandleKnockbackEvent;
    }

    public override void _Process(double delta)
    {
        if (Status.HitStopTimer > 0)
        {
            Status.HitStopTimer -= (float)delta;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Status.HitStopTimer > 0)
        {
            // Factor 100 = 100% stop (0.0 scale). Factor 0 = 0% stop (1.0 scale).
            Status.TimeScale = 1.0f - Mathf.Clamp(Status.HitStopFactor / 100.0f, 0.0f, 1.0f);
            StateMachine.ProcessState((float)delta * Status.TimeScale);
        }
        else
        {
            Status.TimeScale = 1.0f;
            StateMachine.ProcessState((float)delta);
        }
    }

    public override void _ExitTree()
    {
        Status.OnKnockbackReceived -= HandleKnockbackEvent;
    }

    public void HandleKnockbackEvent(Vector3 sourcePos, float power)
    {
        StateMachine.ChangeState(new HitState(this, sourcePos, power));
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
}
