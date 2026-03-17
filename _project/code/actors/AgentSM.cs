using Godot;
using System;

public partial class AgentSM : StateMachine
{
    public override void Initialise(ActorCore core)
    {
        base.Initialise(core);
        CurrentState = new AgentIdleState(_core);
        PreviousState = CurrentState;
        CurrentState.EnterState();
    }

    public override ActorState CreateHitState(Vector3 sourcePos, float power)
    {
        return new AgentHitState(_core, sourcePos, power);
    }
}
