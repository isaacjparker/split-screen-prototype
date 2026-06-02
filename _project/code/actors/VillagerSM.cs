using Godot;

public partial class VillagerSM : StateMachine
{
    public override void Initialise(ActorCore core)
    {
        base.Initialise(core);
        CurrentState = new VillagerIdleState(_core);
        PreviousState = CurrentState;
        CurrentState.EnterState();
    }

    public override ActorState CreateHitState(Vector3 sourcePos, float power)
    {
        return new VillagerHitState(_core, sourcePos, power);
    }

    public override ActorState CreateDeathState(Vector3 sourcePos, float knockbackPower)
    {
        return new AgentDeathState(_core);
    }
}
