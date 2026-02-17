using Godot;
using System;
using Godot.Collections;

public static class CombatUtils
{
    public static CharacterBody3D GetClosestTargetInCone(
        Vector3 origin,
        Vector3 forward,
        float maxDistance,
        float maxAngleDegrees,
        string targetGroup,
        SceneTree tree)
    { 
		CharacterBody3D bestTarget = null;
        float closestDistance = maxDistance;

        // Get all nodes in the target group (e.g. "enemies")
        Array<Node> candidates = tree.GetNodesInGroup(targetGroup);

        foreach (Node node in candidates)
        {
            if (node is CharacterBody3D body)
            {
                Vector3 toTarget = body.GlobalPosition - origin;
                float distance = toTarget.Length();

				if (distance > maxDistance) continue;

                // Angle check using dot product
                Vector3 directionToTarget = toTarget.Normalized();
                float dot = forward.Dot(directionToTarget);
                float angle = Mathf.RadToDeg(Mathf.Acos(dot));

                if (angle <= maxAngleDegrees)
                {
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        bestTarget = body;
                    }
                }
            }
        }
        return bestTarget;
    }

}
