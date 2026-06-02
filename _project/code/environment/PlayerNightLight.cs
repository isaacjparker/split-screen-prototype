using Godot;

/// <summary>
/// Attach this as a script on an OmniLight3D child of the player actor.
/// Fades the light in at night so each player has a small visibility bubble.
/// </summary>
public partial class PlayerNightLight : OmniLight3D
{
    [Export] public float NightLightEnergy = 1.8f;
    [Export] public float TransitionDuration = 2.5f;

    private Tween _tween;

    public override void _Ready()
    {
        LightEnergy = 0f;

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

    private void OnNightStarted()
    {
        TweenEnergy(NightLightEnergy);
    }

    private void OnDayStarted()
    {
        TweenEnergy(0f);
    }

    private void TweenEnergy(float target)
    {
        _tween?.Kill();
        _tween = CreateTween();
        _tween.TweenProperty(this, "light_energy", target, TransitionDuration)
              .SetTrans(Tween.TransitionType.Sine);
    }
}
