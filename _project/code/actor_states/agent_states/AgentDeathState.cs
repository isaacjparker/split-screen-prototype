using Godot;
using System;

public partial class AgentDeathState : ActorState
{
	private float _despawnTimer;

    public AgentDeathState(ActorCore core) : base(core)
    {
		_despawnTimer = 0.5f;		// Magic number. Change.
    }

    public override void EnterState()
    {
        GD.Print("Agent has died.");

		_core.Velocity = Vector3.Zero;

		// Disable hitbox and hurtbox so there's no combat interaction

		if (_core.HitBox != null)
		{
			_core.SetHitBoxEnabled(false);
		}

		if (_core.HurtBox != null)
		{
			_core.SetHurtBoxEnabled(false);
		}

		// TODO: Death animation, particle effect, loot drop etc.
    }

	public override void ProcessState(float delta)
    {
        _despawnTimer -= delta;

		if (_despawnTimer <= 0f)
		{
			_core.QueueFree();
		}
    }

    public override void ExitState()
    {
        
    }

    
}
