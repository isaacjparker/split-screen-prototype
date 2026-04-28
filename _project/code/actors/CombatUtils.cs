using System.Collections.Generic;
using Godot;

public static class CombatUtils
{
    private static readonly List<ActorCore> _candidateBuffer = new List<ActorCore>();

    public static ActorCore GetClosestActorInCone(
        ActorCore self,
        Vector3 forward,
        float maxDistance,
        float maxAngleDegrees,
        Faction callerFaction)
    { 

        GetCandidatesInCone(self, forward, maxDistance, maxAngleDegrees, callerFaction, false);

		ActorCore bestTarget = null;
        float closestDistance = maxDistance;

        foreach (ActorCore actor in _candidateBuffer)
        {
            float distance = self.GlobalPosition.DistanceTo(actor.GlobalPosition);
            
            if (distance < closestDistance)
            {
                closestDistance = distance;
                bestTarget = actor;
            }
        }

        return bestTarget;
    }

    public static ActorCore GetHighestPriorityTarget(
        ActorCore self,
        Vector3 forward, 
        float maxRange,
        float maxAngleDegrees)
    {
        GetCandidatesInCone(self, forward, maxRange, maxAngleDegrees, self.Status.Faction, true);

        ActorCore bestTarget = null;
        float bestScore = -1f;

        StatusModule status = self.Status;
        float highestThreat = status.ThreatTable.GetHighestThreat();

        foreach (ActorCore actor in _candidateBuffer)
        {
            if (actor == self) continue;

            FactionRelation relation = FactionManager.GetRelation(status.Faction, actor.Status.Faction);

            float distance = self.GlobalPosition.DistanceTo(actor.GlobalPosition);

            // Proximity score: 1.0 at zero distance, 0.0 at max range
            float proximityScore = 1f - (distance / maxRange);

            // Faction score: Hostile = 1.0, neutral = 0.25
            float factionScore = relation == FactionRelation.Hostile ? 1f : 0.25f;

            // Threat score: normalized against highest threat, 0 if no threats
            float threatScore = 0f;
            if (highestThreat > 0f)
            {
                threatScore = status.ThreatTable.GetThreat(actor) / highestThreat;
            }

            float totalScore = (proximityScore * status.ProximityWeight)
                            + (factionScore * status.FactionWeight)
                            + (threatScore * status.ThreatWeight);
            
            if (totalScore > bestScore)
            {
                bestScore = totalScore;
                bestTarget = actor;
            }
        }

        return bestTarget;
    }

    private static void GetCandidatesInCone(
        ActorCore self,
        Vector3 forward,
        float maxDistance,
        float maxAngleDegrees,
        Faction callerFaction,
        bool requireLineOfSight)
    {
        _candidateBuffer.Clear();

        foreach(ActorCore actor in RuntimeSets.Instance.Actors.GetAll())
        {
            if (actor == self) continue;
            if (!actor.Status.IsAlive) continue;
            if (!FactionManager.IsHostile(callerFaction, actor.Status.Faction)) continue;

            Vector3 toTarget = actor.GlobalPosition - self.GlobalPosition;
            float distance = toTarget.Length();

            if (distance > maxDistance) continue;

            Vector3 directionToTarget = toTarget.Normalized();
            float dot = forward.Dot(directionToTarget);
            float angle = Mathf.RadToDeg(Mathf.Acos(dot));

            if (angle > maxAngleDegrees * 0.5f) continue;

            if (requireLineOfSight && !HasLineOfSight(self, actor)) continue;

            _candidateBuffer.Add(actor);
        }
    }

    private static bool HasLineOfSight(ActorCore from, ActorCore to)
    {
        PhysicsDirectSpaceState3D spaceState = from.GetWorld3D().DirectSpaceState;

        // Raycast from face mesh hieght rather than feet
        Vector3 origin = from.FaceMesh.GlobalPosition + Vector3.Up;
        Vector3 target = to.FaceMesh.GlobalPosition + Vector3.Up;

        var query = PhysicsRayQueryParameters3D.Create(origin, target);
        query.CollisionMask = 1; // Physics layer only

        var result = spaceState.IntersectRay(query);

        // No hit means clear line of sight
        // Hit on the target's collider also counts as visible
        return result.Count == 0 || (GodotObject)result["collider"] == to;
    }

}
