using Godot;
using System;

public partial class StateMachine : Node
{
	public ActorState CurrentState { get; private set; }
	public ActorState PreviousState { get; private set; }

    public void Initialise(ActorState startingState)
    {
        CurrentState = startingState;
        PreviousState = startingState;
        CurrentState?.EnterState();
    }

    public void ChangeState(ActorState newState)
    {
        CurrentState?.ExitState();
        PreviousState = CurrentState;
        CurrentState = newState;
        CurrentState?.EnterState();
    }

    public void ProcessState(float delta)
    {
        CurrentState?.ProcessState(delta);
    }
}
