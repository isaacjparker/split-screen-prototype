using Godot;

/// <summary>
/// Placeholder world-space feedback label. Rises, fades, and frees itself. Billboarded so
/// it reads in every split-screen viewport. Swap the visual later for a proper VFX/icon —
/// the spawn contract is just Setup(text).
/// </summary>
public partial class FloatingLabel : Label3D
{
    [Export] public float RiseSpeed = 1.2f;     // metres/second upward
    [Export] public float Lifetime = 1.8f;      // seconds before it frees
    [Export] public float FadeStart = 0.6f;     // fraction of life before alpha ramps out

    private float _age;
    private float _baseAlpha = 1f;

    public override void _Ready()
    {
        _baseAlpha = Modulate.A;
    }

    /// <summary>Set the text shown (e.g. "+1" or "ATK UP"). Optional tint.</summary>
    public void Setup(string text, Color? color = null)
    {
        Text = text;
        if (color.HasValue)
        {
            Modulate = color.Value;
            _baseAlpha = color.Value.A;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        _age += dt;

        GlobalPosition += Vector3.Up * RiseSpeed * dt;

        float t = Lifetime > 0f ? _age / Lifetime : 1f;
        if (t >= FadeStart)
        {
            float fade = 1f - Mathf.InverseLerp(FadeStart, 1f, t);
            Color c = Modulate;
            c.A = _baseAlpha * Mathf.Clamp(fade, 0f, 1f);
            Modulate = c;
        }

        if (_age >= Lifetime) QueueFree();
    }
}
