using Godot;
using System;

public partial class StateMachine : Node
{
    protected ActorCore _core;

	public ActorState CurrentState { get; protected set; }
	public ActorState PreviousState { get; protected set; }

    public virtual void Initialise(ActorCore core)
    {
        _core = core;
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

    public virtual ActorState CreateHitState(Vector3 sourcePos, float power)
    {
        return null;
    }

    public virtual Vector3 GetMovementDirection() => Vector3.Zero;
    public virtual bool IsAttackRequested() => false;
    public virtual bool IsTargetLockHeld() => false;
    public virtual bool IsTargetLockRequested() => false;
}
