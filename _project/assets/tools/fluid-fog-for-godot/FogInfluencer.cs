using Godot;

namespace FogFluid;

// Attach this to (or as a child Node3D of) anything that should affect the fog.
//   - Push  : injects velocity from the node's own movement, so fog flows aside and back.
//   - Clear : removes density around the node (use for spells, fire, etc.).
// It self-registers with the FogManager and needs no collider or physics body.
public partial class FogInfluencer : Node3D
{
    public enum InfluenceMode { Push, Clear }

    [Export] public InfluenceMode Mode = InfluenceMode.Push;
    [Export] public float Radius = 2.0f;   // affected radius, in world units
    [Export] public float Strength = 1.0f; // tune to taste (try 1-5)

    // World-space velocity on the XZ plane (units/sec), derived from movement.
    public Vector2 GridVelocity { get; private set; }
    private Vector3 _lastPos;

    public override void _Ready()
    {
        _lastPos = GlobalPosition;
        // Deferred so it works regardless of whether the manager's _Ready ran first.
        CallDeferred(nameof(TryRegister));
    }

    private void TryRegister() => FogManager.Instance?.Register(this);

    public override void _ExitTree() => FogManager.Instance?.Unregister(this);

    public override void _Process(double delta)
    {
        if (delta <= 0) return;
        Vector3 p = GlobalPosition;
        Vector3 d = (p - _lastPos) / (float)delta;
        GridVelocity = new Vector2(d.X, d.Z);
        _lastPos = p;
    }
}
