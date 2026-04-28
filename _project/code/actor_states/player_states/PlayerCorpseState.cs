using Godot;
using System;

public partial class PlayerCorpseState : ActorState
{
    public PlayerCorpseState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        
    }

    public override void ProcessState(float delta)
    {
        if (_core.StateMachine.IsInteractRequested())
        {
            _core.Reset(_core.InitialSpawnPosition, _core.InitialSpawnBasis);
        }
    }

    public override void ExitState()
    {
        
    }

    
}
