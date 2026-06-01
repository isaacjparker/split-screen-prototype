namespace FogFluid;

// Pure C# 2D fluid solver based on Jos Stam, "Real-Time Fluid Dynamics for Games".
// No Godot dependency. Treat this class as a black box: you push density/velocity
// into it, call Step(), and read back the Density array.
//
// The grid is N x N interior cells with a 1-cell border, so every array is
// (N + 2) * (N + 2). Interior indices run 1..N. The border cells handle boundaries.
public sealed class FluidSolver
{
    public readonly int N;
    private readonly int _size;
    private readonly float _diff; // density diffusion rate
    private readonly float _visc; // velocity viscosity

    // Velocity (u = x, v = z) and their source/scratch buffers.
    private float[] _u, _v, _u0, _v0;
    // Density and its source/scratch buffer.
    private float[] _dens, _dens0;

    public FluidSolver(int n, float diffusion, float viscosity)
    {
        N = n;
        _size = (n + 2) * (n + 2);
        _diff = diffusion;
        _visc = viscosity;

        _u = new float[_size];
        _v = new float[_size];
        _u0 = new float[_size];
        _v0 = new float[_size];
        _dens = new float[_size];
        _dens0 = new float[_size];
    }

    /// Full (N+2)*(N+2) density array. Use Index(i, j) to read interior cells 1..N.
    public float[] Density => _dens;

    public int Index(int i, int j) => i + (N + 2) * j;

    // --- Injection API (call between Step()s) ---

    /// Add (or, with a negative amount, remove) density at an interior cell.
    public void AddDensity(int i, int j, float amount)
    {
        if (i < 1 || i > N || j < 1 || j > N) return;
        _dens0[Index(i, j)] += amount;
    }

    /// Add velocity at an interior cell. Units are domain-fractions per second
    /// (1.0 ~= cross the whole field in one second).
    public void AddVelocity(int i, int j, float vx, float vy)
    {
        if (i < 1 || i > N || j < 1 || j > N) return;
        int k = Index(i, j);
        _u0[k] += vx;
        _v0[k] += vy;
    }

    // --- Simulation ---

    public void Step(float dt, float densityDissipation)
    {
        VelStep(dt);
        DensStep(dt);

        // Dissipate and clamp density to [0, +inf) -> fog fades and never goes negative.
        float keep = 1f - densityDissipation * dt;
        if (keep < 0f) keep = 0f;
        for (int k = 0; k < _size; k++)
        {
            float d = _dens[k] * keep;
            _dens[k] = d < 0f ? 0f : d;
        }

        // Clear source buffers for next frame.
        System.Array.Clear(_u0, 0, _size);
        System.Array.Clear(_v0, 0, _size);
        System.Array.Clear(_dens0, 0, _size);
    }

    private void VelStep(float dt)
    {
        AddSource(_u, _u0, dt);
        AddSource(_v, _v0, dt);

        (_u0, _u) = (_u, _u0);
        Diffuse(1, _u, _u0, _visc, dt);
        (_v0, _v) = (_v, _v0);
        Diffuse(2, _v, _v0, _visc, dt);

        Project(_u, _v, _u0, _v0);

        (_u0, _u) = (_u, _u0);
        (_v0, _v) = (_v, _v0);
        Advect(1, _u, _u0, _u0, _v0, dt);
        Advect(2, _v, _v0, _u0, _v0, dt);

        Project(_u, _v, _u0, _v0);
    }

    private void DensStep(float dt)
    {
        AddSource(_dens, _dens0, dt);
        (_dens0, _dens) = (_dens, _dens0);
        Diffuse(0, _dens, _dens0, _diff, dt);
        (_dens0, _dens) = (_dens, _dens0);
        Advect(0, _dens, _dens0, _u, _v, dt);
    }

    private void AddSource(float[] x, float[] s, float dt)
    {
        for (int k = 0; k < _size; k++) x[k] += dt * s[k];
    }

    private void Diffuse(int b, float[] x, float[] x0, float diff, float dt)
    {
        float a = dt * diff * N * N;
        LinSolve(b, x, x0, a, 1f + 4f * a);
    }

    private void LinSolve(int b, float[] x, float[] x0, float a, float c)
    {
        float invC = 1f / c;
        for (int k = 0; k < 20; k++)
        {
            for (int i = 1; i <= N; i++)
                for (int j = 1; j <= N; j++)
                    x[Index(i, j)] = (x0[Index(i, j)] + a * (
                        x[Index(i - 1, j)] + x[Index(i + 1, j)] +
                        x[Index(i, j - 1)] + x[Index(i, j + 1)])) * invC;
            SetBnd(b, x);
        }
    }

    private void Advect(int b, float[] d, float[] d0, float[] u, float[] v, float dt)
    {
        float dt0 = dt * N;
        for (int i = 1; i <= N; i++)
        {
            for (int j = 1; j <= N; j++)
            {
                float x = i - dt0 * u[Index(i, j)];
                float y = j - dt0 * v[Index(i, j)];

                if (x < 0.5f) x = 0.5f;
                if (x > N + 0.5f) x = N + 0.5f;
                int i0 = (int)x; int i1 = i0 + 1;

                if (y < 0.5f) y = 0.5f;
                if (y > N + 0.5f) y = N + 0.5f;
                int j0 = (int)y; int j1 = j0 + 1;

                float s1 = x - i0; float s0 = 1f - s1;
                float t1 = y - j0; float t0 = 1f - t1;

                d[Index(i, j)] =
                    s0 * (t0 * d0[Index(i0, j0)] + t1 * d0[Index(i0, j1)]) +
                    s1 * (t0 * d0[Index(i1, j0)] + t1 * d0[Index(i1, j1)]);
            }
        }
        SetBnd(b, d);
    }

    private void Project(float[] u, float[] v, float[] p, float[] div)
    {
        float h = 1f / N;
        for (int i = 1; i <= N; i++)
            for (int j = 1; j <= N; j++)
            {
                div[Index(i, j)] = -0.5f * h * (
                    u[Index(i + 1, j)] - u[Index(i - 1, j)] +
                    v[Index(i, j + 1)] - v[Index(i, j - 1)]);
                p[Index(i, j)] = 0f;
            }
        SetBnd(0, div);
        SetBnd(0, p);

        LinSolve(0, p, div, 1f, 4f);

        for (int i = 1; i <= N; i++)
            for (int j = 1; j <= N; j++)
            {
                u[Index(i, j)] -= 0.5f * (p[Index(i + 1, j)] - p[Index(i - 1, j)]) / h;
                v[Index(i, j)] -= 0.5f * (p[Index(i, j + 1)] - p[Index(i, j - 1)]) / h;
            }
        SetBnd(1, u);
        SetBnd(2, v);
    }

    private void SetBnd(int b, float[] x)
    {
        for (int i = 1; i <= N; i++)
        {
            x[Index(0, i)] = b == 1 ? -x[Index(1, i)] : x[Index(1, i)];
            x[Index(N + 1, i)] = b == 1 ? -x[Index(N, i)] : x[Index(N, i)];
            x[Index(i, 0)] = b == 2 ? -x[Index(i, 1)] : x[Index(i, 1)];
            x[Index(i, N + 1)] = b == 2 ? -x[Index(i, N)] : x[Index(i, N)];
        }
        x[Index(0, 0)] = 0.5f * (x[Index(1, 0)] + x[Index(0, 1)]);
        x[Index(0, N + 1)] = 0.5f * (x[Index(1, N + 1)] + x[Index(0, N)]);
        x[Index(N + 1, 0)] = 0.5f * (x[Index(N, 0)] + x[Index(N + 1, 1)]);
        x[Index(N + 1, N + 1)] = 0.5f * (x[Index(N, N + 1)] + x[Index(N + 1, N)]);
    }
}
