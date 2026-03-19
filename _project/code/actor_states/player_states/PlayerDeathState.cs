using Godot;
using System;

public partial class PlayerDeathState : ActorState
{
    public PlayerDeathState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        GD.Print("Player has died.");

		// TODO: Death animation, player death functionality
		_core.Velocity = Vector3.Zero;
    }

	public override void ProcessState(float delta)
    {
        
    }

    public override void ExitState()
    {
        
    }

    
}
