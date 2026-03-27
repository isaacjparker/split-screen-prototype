using System;
using Godot;

/// <summary>
/// Autoload singleton that drives the day/night cycle.
/// Register this as an autoload in Project -> Project Settings -> Globals.
/// </summary>
public partial class DayNightCycleManager : Node
{
	[ExportGroup("References")]
	[Export] public SunRuntimeSet SunRuntimeSet {get; private set;}
	

	[ExportGroup("Colours")]
	[Export] public Color DayColour {get; private set;} = new Color(1.0f, 0.95f, 0.84f);
	[Export] public Color NightColour {get; private set;} = new Color(0.12f, 0.11f, 0.25f);

	[ExportGroup("Light Energy")]
	[Export] public float DayEnergy {get; private set;} = 1.0f;
	[Export] public float NightEnergy {get; private set;} = 0.25f;

	[ExportGroup("Timing (seconds)")]
	[Export] public float DayDuration {get; private set;} = 120f;
	[Export] public float NightDuration {get; private set;} = 60f;
	[Export] public float ColourTransitionDuration {get; private set;} = 2f;
	[Export] public float NightAngleTransitionDuration {get; private set;} = 3f;

	[ExportGroup("Sun Arc (degrees)")]
	// rotation at dawn/dusk - low o nthe horizon
	[Export] public float SunElevationLow {get; private set;} = -15f;
	// rotation at midday - nearly overhead but still slightly south
	[Export] public float SunElevationHigh {get; private set;} = -75f;
	// rotation at sunrise - east side
	[Export] public float SunAzimuthStart {get; private set;} = 60f;
	// rotation at sunset - west side
	[Export] public float SunAzimuthEnd {get; private set;} = -60f;
	// rotation during night - straight down, no directional shadow
	[Export] public float NightElevation {get; private set;} = -90f;

	public static DayNightCycleManager Instance { get; private set; }

	public DayNightCycleManager()
	{
		Instance = this;
	}

	public enum Phase {Day, Night}
	public Phase CurrentPhase {get; private set;} = Phase.Day;

	/// <summary>0‑1 progress through the full day+night cycle.</summary>
	public float CycleProgress
	{
		get
		{
			float totalDuration = DayDuration + NightDuration;
			if (totalDuration <= 0f) return 0f;
 
			float elapsed = CurrentPhase == Phase.Day
				? _phaseElapsed
				: DayDuration + _phaseElapsed;
 
			return elapsed / totalDuration;
		}
	}
 
	/// <summary>0‑1 progress through the day phase. Returns 0 during night.</summary>
	public float DayProgress => CurrentPhase == Phase.Day ? _phaseProgress : 0f;
 
	/// <summary>0‑1 progress through the night phase. Returns 0 during day.</summary>
	public float NightProgress => CurrentPhase == Phase.Night ? _phaseProgress : 0f;



	private float _phaseElapsed;
	private float _currentPhaseDuration => CurrentPhase == Phase.Day ? DayDuration : NightDuration;
	private float _phaseProgress => _phaseElapsed / _currentPhaseDuration;
	private DirectionalLight3D _sun;
	private Tween _colourTween;
	private Tween _angleTween;
	private Tween _energyTween;


	public event Action DayStarted;
	public event Action NightStarted;
	public event Action<float> PhaseProgressChanged;

	public override void _Ready()
	{
		if (SunRuntimeSet == null)
		{
			GD.PushWarning("DayNightCycleManager: No SunRuntimeSet assigned. " +
			               "Assign one in the inspector.");
			return;
		}
 
		if (SunRuntimeSet.Count > 0)
		{
			foreach (var sun in SunRuntimeSet.GetAll())
			{
				SetSun(sun);
				break;
			}
		}
		else
		{
			SunRuntimeSet.ItemAdded += OnSunAdded;
		}
	}

    public override void _ExitTree()
    {
        SunRuntimeSet.ItemAdded -= OnSunAdded;
    }

	private void OnSunAdded(DirectionalLight3D sun)
	{
		SunRuntimeSet.ItemAdded -= OnSunAdded;
		SetSun(sun);
	}
 
	private void SetSun(DirectionalLight3D sun)
	{
		_sun = sun;
		StartDay(immediate: true);
	}

	public override void _Process(double delta)
	{
		_phaseElapsed += (float)delta;
		PhaseProgressChanged?.Invoke(_phaseProgress);

		if (CurrentPhase == Phase.Day)
		{
			UpdateSunArc(_phaseProgress);
		}

		if (_phaseElapsed >= _currentPhaseDuration)
		{
			AdvancePhase();
		}
	}

	private void UpdateSunArc(float progress)
	{
		float elevationFactor = Mathf.Sin(progress * Mathf.Pi);
		float elevation = Mathf.Lerp(SunElevationLow, SunElevationHigh, elevationFactor);

		float azimuth = Mathf.Lerp(SunAzimuthStart, SunAzimuthEnd, progress);

		_sun.RotationDegrees = new Vector3(elevation, azimuth, 0f);
	}

	private void AdvancePhase()
	{
		if (CurrentPhase == Phase.Day)
		{
			StartNight();
		}
		else
		{
			StartDay();
		}
	}

	private void StartDay(bool immediate = false)
	{
		GD.Print("DayNightCycleManager: Day Started.");
		CurrentPhase = Phase.Day;
		_phaseElapsed = 0f;

		TweenColour(DayColour, immediate);
		TweenEnergy(DayEnergy, immediate);
		KillTween(ref _angleTween);

		if (!immediate)
		{
			DayStarted?.Invoke();
		}
	}

	private void StartNight(bool immediate = false)
	{
		GD.Print("DayNightCycleManager: Night Started.");

		CurrentPhase = Phase.Night;
		_phaseElapsed = 0f;

		TweenColour(NightColour, immediate);
		TweenEnergy(NightEnergy, immediate);
		TweenToNightAngle(immediate);

		NightStarted?.Invoke();
	}


	private void TweenColour(Color target, bool immediate)
	{
		if (_sun == null) return;

		KillTween(ref _colourTween);

		if (immediate)
		{
			_sun.LightColor = target;
			return;
		}

		_colourTween = CreateTween();
		_colourTween.TweenProperty(_sun, "light_color", target, ColourTransitionDuration)
		            .SetTrans(Tween.TransitionType.Sine)
		            .SetEase(Tween.EaseType.InOut);

	}

	private void TweenEnergy(float target, bool immediate)
	{
		if (_sun == null) return;

		KillTween(ref _energyTween);

		if (immediate)
		{
			_sun.LightEnergy = target;
			return;
		}

		_energyTween = CreateTween();
		_energyTween.TweenProperty(_sun, "light_energy", target, ColourTransitionDuration)
		            .SetTrans(Tween.TransitionType.Sine)
		            .SetEase(Tween.EaseType.InOut);
	}
	private void TweenToNightAngle(bool immediate)
	{
		if (_sun == null) return;

		KillTween(ref _angleTween);

		Vector3 nightRot = new Vector3(NightElevation, 0f, 0f);

		if (immediate)
		{
			_sun.RotationDegrees = nightRot;
			return;
		}

		_angleTween = CreateTween().SetParallel(true);
		_angleTween.TweenProperty(_sun, "rotation_degrees:x", NightElevation, NightAngleTransitionDuration)
		           .SetTrans(Tween.TransitionType.Sine)
		           .SetEase(Tween.EaseType.InOut);
		_angleTween.TweenProperty(_sun, "rotation_degrees:y", 0f, NightAngleTransitionDuration)
		           .SetTrans(Tween.TransitionType.Sine)
		           .SetEase(Tween.EaseType.InOut);
	}


	private static void KillTween(ref Tween tween)
	{
		if (tween != null && tween.IsValid())
		{
			tween.Kill();
		}

		tween = null;
	}
}
