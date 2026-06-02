using Godot;

public partial class VillagerIdleState : ActorState
{
    private const float RescueRadius = 2.5f;
    private float _scanTimer;

    public VillagerIdleState(ActorCore core) : base(core) { }

    public override void EnterState()
    {
        _scanTimer = 0f;
    }

    public override void ProcessState(float delta)
    {
        _core.Motor.ProcessLocomotion(Vector3.Zero, _status.MaxSpeed, delta);

        _scanTimer -= delta;
        if (_scanTimer > 0f) return;
        _scanTimer = _status.TargetingPollingRate;

        ActorCore nearbyPlayer = FindNearestPlayer();
        if (nearbyPlayer == null) return;

        _core.StateMachine.ChangeState(new VillagerFollowState(_core, nearbyPlayer));
    }

    public override void ExitState() { }

    private ActorCore FindNearestPlayer()
    {
        ActorCore best = null;
        float bestDist = RescueRadius * RescueRadius;

        foreach (ActorCore actor in RuntimeSets.Instance.Actors.GetAll())
        {
            if (actor == null || !Node.IsInstanceValid(actor)) continue;
            if (!actor.Status.IsAlive) continue;
            if (actor.Status.Faction != Faction.Player) continue;

            float distSq = _core.GlobalPosition.DistanceSquaredTo(actor.GlobalPosition);
            if (distSq < bestDist)
            {
                bestDist = distSq;
                best = actor;
            }
        }

        return best;
    }
}
