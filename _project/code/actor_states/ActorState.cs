using Godot;
using System;

public abstract class ActorState
{
    protected ActorCore _core;
    protected StatusModule _status;

    public ActorState(ActorCore core)
    {
        _core = core;
        _status = _core.Status;
    }

    public abstract void EnterState();
    public abstract void ProcessState(float delta);
    public abstract void ExitState();
}
