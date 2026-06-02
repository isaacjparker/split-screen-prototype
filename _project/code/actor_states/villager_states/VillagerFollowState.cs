using Godot;

public partial class VillagerFollowState : ActorState
{
    private const float FollowDistance = 1.8f;
    private const float BankRadius = 3.0f;

    private ActorCore _leader;

    public VillagerFollowState(ActorCore core, ActorCore leader) : base(core)
    {
        _leader = leader;
    }

    public override void EnterState() { }

    public override void ProcessState(float delta)
    {
        // Leader gone or dead → find another or go idle
        if (_leader == null || !Node.IsInstanceValid(_leader) || !_leader.Status.IsAlive)
        {
            _core.StateMachine.ChangeState(new VillagerIdleState(_core));
            return;
        }

        // Banked when near the home heart
        if (HomeRoom.Instance != null)
        {
            float distToHeart = _core.GlobalPosition.DistanceTo(HomeRoom.Instance.HeartPosition);
            if (distToHeart <= BankRadius)
            {
                HomeRoom.Instance.BankVillager(_core, _leader);
                _core.StateMachine.ChangeState(new AgentPatrolState(_core));
                return;
            }
        }

        // Follow the leader
        float distToLeader = _core.GlobalPosition.DistanceTo(_leader.GlobalPosition);
        if (distToLeader > FollowDistance)
            _core.Motor.ProcessNavLocomotion(_leader.GlobalPosition, _status.MaxSpeed, delta);
        else
            _core.Motor.ProcessLocomotion(Vector3.Zero, _status.MaxSpeed, delta);
    }

    public override void ExitState() { }

    public ActorCore GetLeader() => _leader;
}
