using Godot;

/// <summary>
/// Anything an agent can treat as its "home turf" to wander within.
/// Implemented by HomeRoom (for banked villagers) and EnemySpawner (for guards),
/// so both share the single AgentPatrolState behaviour.
/// </summary>
public interface IPatrolRegion
{
    Vector3 GetRandomPatrolPoint();
}
