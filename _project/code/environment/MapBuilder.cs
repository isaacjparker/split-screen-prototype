using Godot;
using System.Collections.Generic;

public partial class MapBuilder : Node3D
{
    [Export] public float CellSize { get; set; } = 2.0f;
    [Export(PropertyHint.Layers3DPhysics)]
    public uint FloorCollisionMask { get; set; } = 1;
    [Export] public int CorridorMaxWidth { get; set; } = 4;
    [Export] public int GridMargin { get; set; } = 1;

    [Signal]
    public delegate void MapBuiltEventHandler();

    public MapGrid Grid { get; private set; }

    private bool _built;

    // -------------------------------------------------------
    //  Build trigger — waits for first physics frame so
    //  all collision bodies are registered before raycasting.
    // -------------------------------------------------------

    public override void _PhysicsProcess(double delta)
    {
        if (_built) return;

        _built = true;
        BuildMap();
        SetPhysicsProcess(false);
    }

    // -------------------------------------------------------
    //  Main build sequence
    // -------------------------------------------------------

    private void BuildMap()
    {
        List<Node3D> roomNodes = CollectRoomNodes();

        if (roomNodes.Count == 0)
        {
            GD.PrintErr("MapBuilder: No child Node3D rooms found.");
            return;
        }

        Aabb totalBounds = ComputeTotalBounds(roomNodes);
        InitializeGrid(totalBounds);

        ValidateRooms(roomNodes);

        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;

        for (int roomId = 0; roomId < roomNodes.Count; roomId++)
        {
            ProcessRoom(roomId, roomNodes[roomId], spaceState);
        }

        GD.Print($"MapBuilder: Grid built ({Grid.Width}x{Grid.Height}), {roomNodes.Count} rooms processed.");
        EmitSignal(SignalName.MapBuilt);
    }

    // -------------------------------------------------------
    //  Grid initialization from total bounds
    // -------------------------------------------------------

    private void InitializeGrid(Aabb totalBounds)
    {
        int width = Mathf.CeilToInt(totalBounds.Size.X / CellSize) + GridMargin * 2;
        int height = Mathf.CeilToInt(totalBounds.Size.Z / CellSize) + GridMargin * 2;

        Vector3 origin = new Vector3(
            totalBounds.Position.X - GridMargin * CellSize,
            0f,
            totalBounds.Position.Z - GridMargin * CellSize
        );

        Grid = new MapGrid();
        Grid.Initialize(width, height, CellSize, origin);
    }

    // -------------------------------------------------------
    //  Validation — warns about rooms that will produce
    //  inconsistent cell counts due to alignment issues.
    // -------------------------------------------------------

    private void ValidateRooms(List<Node3D> roomNodes)
    {
        for (int i = 0; i < roomNodes.Count; i++)
        {
            Node3D room = roomNodes[i];
            string roomName = room.Name;

            Vector3 pos = room.GlobalPosition;
            float xRemainder = Mathf.Abs(pos.X % CellSize);
            float zRemainder = Mathf.Abs(pos.Z % CellSize);
            float tolerance = 0.01f;

            if (xRemainder > tolerance && xRemainder < CellSize - tolerance)
            {
                GD.PushWarning($"MapBuilder: '{roomName}' X position ({pos.X}) is not a multiple of cell size ({CellSize}). This may cause inconsistent cell counts.");
            }

            if (zRemainder > tolerance && zRemainder < CellSize - tolerance)
            {
                GD.PushWarning($"MapBuilder: '{roomName}' Z position ({pos.Z}) is not a multiple of cell size ({CellSize}). This may cause inconsistent cell counts.");
            }

            Aabb bounds = ComputeNodeBounds(room);
            int cellsX = Mathf.RoundToInt(bounds.Size.X / CellSize);
            int cellsZ = Mathf.RoundToInt(bounds.Size.Z / CellSize);

            if (cellsX % 2 != 0)
            {
                GD.PushWarning($"MapBuilder: '{roomName}' has an odd cell width ({cellsX}). Even cell dimensions are required for reliable alignment.");
            }

            if (cellsZ % 2 != 0)
            {
                GD.PushWarning($"MapBuilder: '{roomName}' has an odd cell depth ({cellsZ}). Even cell dimensions are required for reliable alignment.");
            }
        }
    }

    // -------------------------------------------------------
    //  Per-room processing: raycast within bounds, detect
    //  corridor, then write cells and room data to Grid.
    // -------------------------------------------------------

    private void ProcessRoom(int roomId, Node3D roomNode, PhysicsDirectSpaceState3D spaceState)
    {
        Aabb bounds = ComputeNodeBounds(roomNode);

        Vector2I minCell = Grid.WorldToCell(bounds.Position);
        Vector2I maxCell = Grid.WorldToCell(bounds.End);

        float rayFromY = bounds.End.Y + 1f;
        float rayToY = bounds.Position.Y - 1f;

        var floorCells = new List<Vector2I>();

        for (int x = minCell.X; x <= maxCell.X; x++)
        {
            for (int z = minCell.Y; z <= maxCell.Y; z++)
            {
                Vector2I coord = new Vector2I(x, z);
                Vector3 worldPos = Grid.CellToWorld(coord);

                Vector3 from = new Vector3(worldPos.X, rayFromY, worldPos.Z);
                Vector3 to = new Vector3(worldPos.X, rayToY, worldPos.Z);

                var query = PhysicsRayQueryParameters3D.Create(from, to, FloorCollisionMask);
                var result = spaceState.IntersectRay(query);

                if (result.Count > 0)
                {
                    floorCells.Add(coord);
                }
            }
        }

        bool isCorridor = DetectCorridor(floorCells);

        Grid.RegisterRoom(roomId, isCorridor);

        foreach (Vector2I cell in floorCells)
        {
            Grid.SetCell(cell.X, cell.Y, CellType.Floor, roomId);
        }
    }

    // -------------------------------------------------------
    //  Corridor detection — narrow in one axis, long in the
    //  other, based on the cell footprint dimensions.
    // -------------------------------------------------------

    private bool DetectCorridor(List<Vector2I> cells)
    {
        if (cells.Count == 0) return false;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (Vector2I cell in cells)
        {
            if (cell.X < minX) minX = cell.X;
            if (cell.X > maxX) maxX = cell.X;
            if (cell.Y < minZ) minZ = cell.Y;
            if (cell.Y > maxZ) maxZ = cell.Y;
        }

        int spanX = maxX - minX + 1;
        int spanZ = maxZ - minZ + 1;

        int narrow = Mathf.Min(spanX, spanZ);
        int wide = Mathf.Max(spanX, spanZ);

        return narrow <= CorridorMaxWidth && wide > CorridorMaxWidth;
    }

    // -------------------------------------------------------
    //  Room node collection — direct Node3D children only
    // -------------------------------------------------------

    private List<Node3D> CollectRoomNodes()
    {
        var rooms = new List<Node3D>();

        foreach (Node child in GetChildren())
        {
            if (child is Node3D node3D)
            {
                rooms.Add(node3D);
            }
        }

        return rooms;
    }

    // -------------------------------------------------------
    //  AABB computation from collision shapes
    // -------------------------------------------------------

    private Aabb ComputeTotalBounds(List<Node3D> roomNodes)
    {
        Aabb total = ComputeNodeBounds(roomNodes[0]);

        for (int i = 1; i < roomNodes.Count; i++)
        {
            total = total.Merge(ComputeNodeBounds(roomNodes[i]));
        }

        return total;
    }

    private Aabb ComputeNodeBounds(Node3D node)
    {
        bool first = true;
        Aabb bounds = new Aabb();
        GatherBoundsRecursive(node, ref bounds, ref first);
        return bounds;
    }

    private void GatherBoundsRecursive(Node node, ref Aabb bounds, ref bool first)
    {
        if (node is CollisionShape3D collisionShape && collisionShape.Shape is BoxShape3D box)
        {
            if (collisionShape.GetParent() is CollisionObject3D body
                && (body.CollisionLayer & FloorCollisionMask) != 0)
            {
                Vector3 position = collisionShape.GlobalPosition;
                Vector3 halfExtents = box.Size * 0.5f;
                Basis basis = collisionShape.GlobalTransform.Basis;

                Vector3 worldHalfExtents = new Vector3(
                    Mathf.Abs(basis.X.X) * halfExtents.X + Mathf.Abs(basis.Y.X) * halfExtents.Y + Mathf.Abs(basis.Z.X) * halfExtents.Z,
                    Mathf.Abs(basis.X.Y) * halfExtents.X + Mathf.Abs(basis.Y.Y) * halfExtents.Y + Mathf.Abs(basis.Z.Y) * halfExtents.Z,
                    Mathf.Abs(basis.X.Z) * halfExtents.X + Mathf.Abs(basis.Y.Z) * halfExtents.Y + Mathf.Abs(basis.Z.Z) * halfExtents.Z
                );

                Aabb shapeAabb = new Aabb(position - worldHalfExtents, worldHalfExtents * 2f);

                bounds = first ? shapeAabb : bounds.Merge(shapeAabb);
                first = false;
            }
        }

        foreach (Node child in node.GetChildren())
        {
            GatherBoundsRecursive(child, ref bounds, ref first);
        }
    }
}