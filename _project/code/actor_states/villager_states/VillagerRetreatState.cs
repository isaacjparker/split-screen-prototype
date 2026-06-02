using Godot;

public partial class VillagerRetreatState : ActorState
{
    private const float RetreatDuration = 2.0f;
    private const float FleeDistance = 6.0f;

    private Vector3 _attackerPos;
    private float _timer;

    public VillagerRetreatState(ActorCore core, Vector3 attackerPos) : base(core)
    {
        _attackerPos = attackerPos;
    }

    public override void EnterState()
    {
        _timer = RetreatDuration;
    }

    public override void ProcessState(float delta)
    {
        _timer -= delta;

        if (_timer <= 0f)
        {
            _core.StateMachine.ChangeState(new AgentPatrolState(_core));
            return;
        }

        // Flee directly away from the attacker
        Vector3 awayDir = (_core.GlobalPosition - _attackerPos) with { Y = 0 };
        if (awayDir.LengthSquared() < 0.001f)
            awayDir = Vector3.Forward;
        else
            awayDir = awayDir.Normalized();

        Vector3 fleeTarget = _core.GlobalPosition + awayDir * FleeDistance;
        _core.Motor.ProcessNavLocomotion(fleeTarget, _status.MaxSpeed, delta);
    }

    public override void ExitState() { }
}
