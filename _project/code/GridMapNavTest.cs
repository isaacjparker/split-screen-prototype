using Godot;

public partial class GridMapNavTest : CharacterBody3D
{
    [Export] public Node3D NavTarget;
    [Export] public NavigationAgent3D NavAgent;
    [Export] public GridMap GridMap;

    public override void _Ready()
    {
        Callable.From(ActuallyTest).CallDeferred();
    }

    private async void ActuallyTest()
    {
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        var worldMap = GetWorld3D().NavigationMap;

        // Properly clear and re-set cells to force a real change, not a no-op
        var cellData = new System.Collections.Generic.List<(Vector3I cell, int item, int orient)>();
        foreach (Vector3I cell in GridMap.GetUsedCells())
        {
            cellData.Add((cell, GridMap.GetCellItem(cell), GridMap.GetCellItemOrientation(cell)));
        }

        // Clear all
        foreach (var (cell, _, _) in cellData)
        {
            GridMap.SetCellItem(cell, -1);
        }

        // Force sync the clear
        NavigationServer3D.MapForceUpdate(worldMap);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        GD.Print($"Regions after clear: {NavigationServer3D.MapGetRegions(worldMap).Count}");

        // Re-place all
        foreach (var (cell, item, orient) in cellData)
        {
            GridMap.SetCellItem(cell, item, orient);
        }

        // Force sync the re-place
        NavigationServer3D.MapForceUpdate(worldMap);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);

        var regions = NavigationServer3D.MapGetRegions(worldMap);
        GD.Print($"Regions after re-place: {regions.Count}");

        // Check actual polygon data on the regions
        for (int i = 0; i < Mathf.Min(2, regions.Count); i++)
        {
            var rid = regions[i];
            int polyCount = (int)NavigationServer3D.RegionGetConnectionsCount(rid);
            GD.Print($"Region {i}: connections={polyCount}, bounds={NavigationServer3D.RegionGetBounds(rid)}");
        }

        // Direct server queries
        GD.Print($"\nAgent at: {GlobalPosition}");
        var closest = NavigationServer3D.MapGetClosestPoint(worldMap, GlobalPosition);
        GD.Print($"Closest map point to agent: {closest}, distance: {GlobalPosition.DistanceTo(closest)}");

        // Try a path
        var path = NavigationServer3D.MapGetPath(worldMap, GlobalPosition, NavTarget.GlobalPosition, true);
        GD.Print($"Direct server path length: {path.Length}");
    }
}