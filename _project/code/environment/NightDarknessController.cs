using Godot;

/// <summary>
/// Attach this as a script on the WorldEnvironment node in environment.tscn.
/// Tweens ambient light energy to near-zero at night so the maze goes dark
/// and player / torch OmniLights become the primary light sources.
/// </summary>
public partial class NightDarknessController : WorldEnvironment
{
    [ExportGroup("Ambient Energy")]
    [Export] public float DayAmbientEnergy = 1.0f;
    [Export] public float NightAmbientEnergy = 0.05f;
    [Export] public float TransitionDuration = 3.0f;

    [ExportGroup("Night Fog")]
    [Export] public float DayFogDensity = 0.02f;
    [Export] public float NightFogDensity = 0.08f;

    private Tween _tween;

    public override void _Ready()
    {
        DayNightCycleManager.Instance.NightStarted += OnNightStarted;
        DayNightCycleManager.Instance.DayStarted += OnDayStarted;
    }

    public override void _ExitTree()
    {
        if (DayNightCycleManager.Instance != null)
        {
            DayNightCycleManager.Instance.NightStarted -= OnNightStarted;
            DayNightCycleManager.Instance.DayStarted -= OnDayStarted;
        }
    }

    private void OnNightStarted() => TweenAmbient(NightAmbientEnergy, NightFogDensity);
    private void OnDayStarted() => TweenAmbient(DayAmbientEnergy, DayFogDensity);

    private void TweenAmbient(float targetEnergy, float targetFogDensity)
    {
        if (Environment == null) return;

        _tween?.Kill();
        _tween = CreateTween().SetParallel(true);

        _tween.TweenMethod(
            Callable.From<float>(e => Environment.AmbientLightEnergy = e),
            Environment.AmbientLightEnergy,
            targetEnergy,
            TransitionDuration
        ).SetTrans(Tween.TransitionType.Sine);

        _tween.TweenMethod(
            Callable.From<float>(d => Environment.FogDensity = d),
            Environment.FogDensity,
            targetFogDensity,
            TransitionDuration
        ).SetTrans(Tween.TransitionType.Sine);
    }
}
