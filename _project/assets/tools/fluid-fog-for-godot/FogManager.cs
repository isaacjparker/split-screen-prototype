using Godot;
using System.Collections.Generic;

namespace FogFluid;

// Drop this script on a single Node in your level. It owns the fluid sim, gathers
// every registered FogInfluencer, steps the simulation at a fixed rate, and uploads
// the density field into one shared texture that the fog shader samples.
//
// One manager runs the whole simulation. Split-screen does NOT multiply this cost:
// every viewport just samples the same texture (see README).
public partial class FogManager : Node
{
    public static FogManager Instance { get; private set; }

    [Export] public int GridSize = 64;                          // sim resolution (64 is a good start)
    [Export] public Vector2 WorldSize = new Vector2(40, 40);    // XZ extent the fog covers, in world units
    [Export] public Node3D FollowTarget;                        // optional; null = fog grid centred on origin
    [Export] public ShaderMaterial FogMaterial;                 // the ShaderMaterial used by your fog plane(s)

    [Export] public float Diffusion = 0.00001f;
    [Export] public float Viscosity = 0.00001f;
    [Export] public float Dissipation = 0.5f;   // higher = fog fades faster
    [Export] public float Ambient = 0.2f;       // continuous fog source per cell (0 = no ambient fog)
    [Export] public float SimRate = 30f;        // simulation steps per second
    [Export] public float DensityScale = 1.0f;  // multiplier applied when writing to the texture

    private FluidSolver _solver;
    private readonly List<FogInfluencer> _influencers = new();
    private ImageTexture _texture;
    private byte[] _pixels;
    private float[] _floatBuf;
    private double _accum;
    private Vector2 _origin; // world-space min corner (x, z)

    public override void _Ready()
    {
        Instance = this;
        _solver = new FluidSolver(GridSize, Diffusion, Viscosity);
        _floatBuf = new float[GridSize * GridSize];
        _pixels = new byte[GridSize * GridSize * sizeof(float)];

        var img = Image.CreateFromData(GridSize, GridSize, false, Image.Format.Rf, _pixels);
        _texture = ImageTexture.CreateFromImage(img);
        FogMaterial?.SetShaderParameter("density_tex", _texture);
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public void Register(FogInfluencer inf)
    {
        if (!_influencers.Contains(inf)) _influencers.Add(inf);
    }

    public void Unregister(FogInfluencer inf) => _influencers.Remove(inf);

    public override void _Process(double delta)
    {
        _accum += delta;
        double stepDt = 1.0 / SimRate;
        int steps = 0;
        while (_accum >= stepDt && steps < 3) // cap catch-up steps to avoid a spiral of death
        {
            Simulate((float)stepDt);
            _accum -= stepDt;
            steps++;
        }
        if (steps > 0) UploadTexture();
    }

    private void Simulate(float dt)
    {
        Vector3 center = FollowTarget != null ? FollowTarget.GlobalPosition : Vector3.Zero;
        _origin = new Vector2(center.X - WorldSize.X * 0.5f, center.Z - WorldSize.Y * 0.5f);

        if (Ambient > 0f)
            for (int i = 1; i <= GridSize; i++)
                for (int j = 1; j <= GridSize; j++)
                    _solver.AddDensity(i, j, Ambient);

        foreach (var inf in _influencers)
        {
            if (!GodotObject.IsInstanceValid(inf)) continue;

            Vector3 wp = inf.GlobalPosition;

            // Continuous (sub-cell) grid position. Do NOT snap to an int centre -
            // snapping is what makes the stamp teleport cell-by-cell as the object moves.
            float fx = (wp.X - _origin.X) / WorldSize.X * GridSize + 1f;
            float fy = (wp.Z - _origin.Y) / WorldSize.Y * GridSize + 1f;

            float rCells = Mathf.Max(1f, inf.Radius / WorldSize.X * GridSize);
            int ci = Mathf.RoundToInt(fx);
            int cj = Mathf.RoundToInt(fy);
            int r = Mathf.CeilToInt(rCells) + 1;

            for (int di = -r; di <= r; di++)
            {
                for (int dj = -r; dj <= r; dj++)
                {
                    int gi = ci + di, gj = cj + dj;
                    if (gi < 1 || gi > GridSize || gj < 1 || gj > GridSize) continue;

                    // Distance from the FRACTIONAL centre -> intensity shifts smoothly sub-cell.
                    float ddx = gi - fx;
                    float ddy = gj - fy;
                    float dist = Mathf.Sqrt(ddx * ddx + ddy * ddy) / rCells;
                    if (dist >= 1f) continue;

                    // Smooth bump: 1 at the centre, eases to 0 at the edge (no hard ring,
                    // no popping as cells enter/leave the window).
                    float falloff = 1f - dist * dist * (3f - 2f * dist);

                    if (inf.Mode == FogInfluencer.InfluenceMode.Clear)
                    {
                        _solver.AddDensity(gi, gj, -inf.Strength * falloff);
                    }
                    else // Push
                    {
                        Vector2 vel = inf.GridVelocity;
                        float gvx = vel.X / WorldSize.X * inf.Strength * falloff;
                        float gvy = vel.Y / WorldSize.Y * inf.Strength * falloff;
                        _solver.AddVelocity(gi, gj, gvx, gvy);
                        _solver.AddDensity(gi, gj, -inf.Strength * falloff * 0.5f);
                    }
                }
            }
        }

        _solver.Step(dt, Dissipation);
    }

    private void UploadTexture()
    {
        float[] dens = _solver.Density;
        int n = GridSize;
        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                float d = dens[_solver.Index(i + 1, j + 1)] * DensityScale;
                if (d < 0f) d = 0f;
                if (d > 1f) d = 1f;
                _floatBuf[i + n * j] = d;
            }
        }
        System.Buffer.BlockCopy(_floatBuf, 0, _pixels, 0, _pixels.Length);
        var img = Image.CreateFromData(n, n, false, Image.Format.Rf, _pixels);
        _texture.Update(img);
    }
}
