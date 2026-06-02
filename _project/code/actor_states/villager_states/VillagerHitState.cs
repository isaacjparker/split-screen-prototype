using Godot;

/// <summary>
/// Handles knockback for villagers and then enters retreat instead of re-engaging.
/// </summary>
public partial class VillagerHitState : AgentHitState
{
    public VillagerHitState(ActorCore core, Vector3 sourcePos, float power)
        : base(core, sourcePos, power) { }

    protected override void ReturnFromHit()
    {
        _core.StateMachine.ChangeState(new VillagerRetreatState(_core, _sourcePos));
    }
}
