using Godot;
using System;

public partial class AgentSM : StateMachine
{
    /// <summary>
    /// True for night-wave enemies. Flips the "what do I do when idle / when I lose
    /// my target" behaviour from guard patrol to marching on the heart, and tells the
    /// pursue chain to skip the home leash so they can travel the whole maze.
    /// </summary>
    public bool WaveMode;

    /// <summary>
    /// The state an agent falls back to when it has nothing to fight: guards return to
    /// patrol, wave enemies resume marching on the heart.
    /// </summary>
    public ActorState CreateReturnState()
    {
        return WaveMode ? new AgentWaveMarchState(_core) : new AgentPatrolState(_core);
    }

    public override void Initialise(ActorCore core)
    {
        base.Initialise(core);

        // Anchor the guard's patrol turf to wherever it was placed/spawned.
        _core.InitialSpawnPosition = _core.GlobalPosition;

        CurrentState = new AgentPatrolState(_core);
        PreviousState = CurrentState;
        CurrentState.EnterState();
    }

    public override ActorState CreateHitState(Vector3 sourcePos, float power)
    {
        return new AgentHitState(_core, sourcePos, power);
    }

    public override ActorState CreateDeathState(Vector3 sourcePos, float knockbackPower)
    {
        return new AgentDeathState(_core);
    }
}
