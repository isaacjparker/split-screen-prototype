using Godot;
using System;


public partial class ActorCore : CharacterBody3D
{

    [ExportGroup("Components")]
    [Export] public HitBox HitBox;
    [Export] public Area3D HurtBox;
    [Export] public Sprite3D SlashVfx;

    public InputModule ActorInput { get; private set; }
    public StatusModule Status { get; private set; }
    public MotorModule Motor { get; private set; }
    public CombatModule Combat { get; private set; }
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

        if (StateMachine == null)
        {
            GD.PrintErr("ActorCore: StateMachine not found.");
            return;
        }

        Status.Initialise(this);
        Motor.Initialise(this);
        Combat.Initialise(this);
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

    public override void _PhysicsProcess(double delta)
    {
        StateMachine.ProcessState((float)delta);
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
}
