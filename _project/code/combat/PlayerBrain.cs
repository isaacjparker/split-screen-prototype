using Godot;
using System;


public partial class PlayerBrain : CharacterBody3D
{
    [Export] public int PlayerSlot;
    [Export] private int _inputDeviceId;

    [ExportGroup("References")]
    [Export] public AnimationTree AnimTree;
    [Export] public StateMachine StateMachine;

    [ExportGroup("Combat")]
    [Export] public HitBox HitBox;
    [Export] public Area3D HurtBox;
    [Export] public float BaseDamage = 5.0f;
    [Export] public float KnockbackPower = 5.0f;
    [Export] public float HitStopDuration = 0.2f;

    [ExportGroup("Locomotion")]
    [Export] private float _gravity = -9.8f;
    [Export] public float MaxSpeed = 5.0f;
    [Export] private float _acceleration = 18.0f;
    [Export] private float _deceleration = 30.0f;
    [Export] private float _turnSpeed = 10.0f;

    [ExportGroup("Targeting")]
    [Export] public float MaxTargetRange = 20.0f;
    [Export] public float MaxTargetScanAngle = 180.0f;
    [Export] public string TargetGroup = "enemies";


    public MotorModule Motor { get; private set; }
    public CombatModule Combat { get; private set; }
    public StatusModule Status { get; private set; }
    public CharacterBody3D CurrentTarget { get; set; }  // Settable by states

    // Animation State Machine
    public AnimationNodeStateMachinePlayback AnimPlayback;

    // State Names (must match AnimationTree Node names exactly)
    public const string ANIM_IDLE = "idle";
    public const string ANIM_ATK1 = "attack1_1handed";
    public const string ANIM_ATK2 = "attack2_1handed";
    public const string ANIM_ATK3 = "attack3_1handed";

    // Input cache
    private StringName _moveLeft, _moveRight, _moveUp, _moveDown, _startButton, _targetButton, _meleeAttack;

    /// <summary>
    /// --- INPUT API ---
    /// Input can be from hardware (device), this entity, other entities,
    /// or the environment.
    /// </summary>
    public bool IsAttackJustPressed() => Input.IsActionJustPressed(_meleeAttack);
    public bool IsTargetPressed() => Input.IsActionPressed(_targetButton);          // For holding (strafing)
    public bool IsTargetJustPressed() => Input.IsActionJustPressed(_targetButton);  // For scanning -> state change
    public event Action OnComboWindowOpen;
    public event Action OnComboWindowClose;

    // Should we have a OnLungeRequested? So we slightly decouple
    // components?

    public override void _Ready()
    {
        Motor = GetNode<MotorModule>("MotorModule");
        Motor.Initialise(this, _acceleration, _deceleration, _turnSpeed, _gravity);

        Combat = GetNode<CombatModule>("CombatModule");
        Combat.Initialise(this, Motor);

        Status = GetNode<StatusModule>("StatusModule");
        Status.Initialise(this);

        if (StateMachine == null)
        {
            GD.PrintErr("PlayerBrain: StateMachine not assigned.");
            return;
        }

        if (AnimTree == null)
        {
            GD.PrintErr("PlayerBrain: AnimationTree not assigned.");
            return;
        }

        StateMachine.Initialise(new IdleMoveState(this));
        AnimPlayback = (AnimationNodeStateMachinePlayback)AnimTree.Get("parameters/playback");

        if (HitBox.ProcessMode != Node.ProcessModeEnum.Disabled)
        { 
            HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;
        }
        
    }

    public override void _PhysicsProcess(double delta)
    {
        StateMachine.ProcessState((float)delta);
    }

    public Vector3 GetInputDirection()
    { 
        Vector2 inputVec = Input.GetVector(_moveLeft, _moveRight, _moveUp, _moveDown);
        return new Vector3(inputVec.X, 0, inputVec.Y);
    }

    public void AssignInputDevice(int deviceId)
    {
        _inputDeviceId = deviceId;
		_moveLeft = $"move_left_{deviceId}";
		_moveRight = $"move_right_{deviceId}";
		_moveUp = $"move_up_{deviceId}";
		_moveDown = $"move_down_{deviceId}";
		_startButton = $"start_{deviceId}";
        _targetButton = $"target_{deviceId}";
        _meleeAttack = $"melee_attack_{deviceId}";
    }

    // ------------------------------------------------------------------------
    // Animation Events: CALLED BY ANIMATION PLAYER (Method Track)
    // Place open keyframe at start of the "Action" phase.
    // Place close keryframe at end of "Action" phase.
    // ------------------------------------------------------------------------
    public void OpenComboWindow()
    { 
        OnComboWindowOpen?.Invoke();
    }

    public void CloseComboWindow()
    {
        OnComboWindowClose?.Invoke();
    }
}
