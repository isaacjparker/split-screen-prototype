using Godot;
using System.Collections.Generic;
using System.Linq;

public enum CellType
{
    Empty,
    Floor
}

public struct CellData
{
    public CellType Type;
    public int RoomId;

    public static readonly CellData EmptyCell = new() { Type = CellType.Empty, RoomId = -1 };
}

public class RoomData
{
    public int Id { get; }
    public HashSet<Vector2I> Cells { get; } = new();
    public bool IsCorridor { get; internal set; }

    public RoomData(int id)
    {
        Id = id;
    }
}

public class MapGrid
{
    private CellData[,] _cells;
    private readonly Dictionary<int, RoomData> _rooms = new();

    private float _cellSize;
    private float _originX;
    private float _originZ;
    private int _width;
    private int _height;

    public int Width => _width;
    public int Height => _height;

    private static readonly Vector2I[] CardinalOffsets =
    {
        Vector2I.Up,
        Vector2I.Down,
        Vector2I.Left,
        Vector2I.Right
    };

    // -------------------------------------------------------
    //  Initialization — called once by MapBuilder
    // -------------------------------------------------------

    public void Initialize(int width, int height, float cellSize, Vector3 origin)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;
        _originX = origin.X;
        _originZ = origin.Z;

        _cells = new CellData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int z = 0; z < height; z++)
            {
                _cells[x, z] = CellData.EmptyCell;
            }
        }
    }

    // -------------------------------------------------------
    //  Write API — used exclusively by MapBuilder
    // -------------------------------------------------------

    public void RegisterRoom(int roomId, bool isCorridor = false)
    {
        _rooms[roomId] = new RoomData(roomId) { IsCorridor = isCorridor };
    }

    public void SetCell(int x, int z, CellType type, int roomId)
    {
        if (!InBounds(x, z)) return;

        _cells[x, z] = new CellData { Type = type, RoomId = roomId };

        if (type == CellType.Floor && roomId >= 0 && _rooms.TryGetValue(roomId, out RoomData room))
        {
            room.Cells.Add(new Vector2I(x, z));
        }
    }

    // -------------------------------------------------------
    //  Coordinate conversion
    // -------------------------------------------------------

    public Vector2I WorldToCell(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt((worldPosition.X - _originX) / _cellSize);
        int z = Mathf.FloorToInt((worldPosition.Z - _originZ) / _cellSize);
        return new Vector2I(x, z);
    }

    public Vector3 CellToWorld(Vector2I cell)
    {
        float x = cell.X * _cellSize + _originX + _cellSize * 0.5f;
        float z = cell.Y * _cellSize + _originZ + _cellSize * 0.5f;
        return new Vector3(x, 0f, z);
    }

    // -------------------------------------------------------
    //  Cell queries
    // -------------------------------------------------------

    public bool InBounds(int x, int z)
    {
        return x >= 0 && x < _width && z >= 0 && z < _height;
    }

    public bool InBounds(Vector2I cell)
    {
        return InBounds(cell.X, cell.Y);
    }

    public CellData GetCell(int x, int z)
    {
        return InBounds(x, z) ? _cells[x, z] : CellData.EmptyCell;
    }

    public CellData GetCell(Vector2I cell)
    {
        return GetCell(cell.X, cell.Y);
    }

    public CellData GetCellAtWorld(Vector3 worldPosition)
    {
        Vector2I coord = WorldToCell(worldPosition);
        return GetCell(coord);
    }

    public bool IsFloor(Vector2I cell)
    {
        return GetCell(cell).Type == CellType.Floor;
    }

    public List<Vector2I> GetNeighbours(Vector2I cell)
    {
        var neighbours = new List<Vector2I>(4);

        foreach (Vector2I offset in CardinalOffsets)
        {
            Vector2I neighbour = cell + offset;
            if (InBounds(neighbour))
            {
                neighbours.Add(neighbour);
            }
        }

        return neighbours;
    }

    public bool IsBoundaryCell(Vector2I cell)
    {
        if (!IsFloor(cell)) return false;

        foreach (Vector2I offset in CardinalOffsets)
        {
            Vector2I neighbour = cell + offset;
            if (!InBounds(neighbour) || GetCell(neighbour).Type == CellType.Empty)
            {
                return true;
            }
        }

        return false;
    }

    // -------------------------------------------------------
    //  Room queries
    // -------------------------------------------------------

    public RoomData GetRoom(int roomId)
    {
        return _rooms.TryGetValue(roomId, out RoomData room) ? room : null;
    }

    public RoomData GetRoomAtCell(Vector2I cell)
    {
        CellData data = GetCell(cell);
        return data.RoomId >= 0 ? GetRoom(data.RoomId) : null;
    }

    public RoomData GetRoomAtWorld(Vector3 worldPosition)
    {
        return GetRoomAtCell(WorldToCell(worldPosition));
    }

    public IEnumerable<RoomData> GetAllRooms()
    {
        return _rooms.Values;
    }

    public IEnumerable<RoomData> GetRooms()
    {
        return _rooms.Values.Where(r => !r.IsCorridor);
    }

    public IEnumerable<RoomData> GetCorridors()
    {
        return _rooms.Values.Where(r => r.IsCorridor);
    }

    public List<Vector2I> GetBoundaryCells(int roomId)
    {
        if (!_rooms.TryGetValue(roomId, out RoomData room))
        {
            return new List<Vector2I>();
        }

        var boundary = new List<Vector2I>();

        foreach (Vector2I cell in room.Cells)
        {
            if (IsBoundaryCell(cell))
            {
                boundary.Add(cell);
            }
        }

        return boundary;
    }
}