using Godot;
using System;
using System.Diagnostics.Metrics;

public partial class ProgressionModule : Node3D
{
    [ExportGroup("Drop Configuration")]
    [Export] public bool CanDrop = true;
    [Export] public PackedScene DropPrefab;
    [Export] public float TotalExperienceValue = 10f;
    [Export] public float OnHitDropFraction = 0.6f;
    [Export] public int DeathFountainDropCount = 4;
    [Export] public float DropLaunchSpeed = 4f;
    [Export] public float DropLaunchUpwardBias = 3f;
    [Export] public float DropSpawnHeightOffset = 0.8f;

    [ExportGroup("Progression")]
    [Export] public float BaseLevelThreshold = 100f;

    private float _droppedSoFar;

    public float CurrentExperience {get; private set;}
    public int CurrentLevel {get; private set;} = 1;
    public float ExperienceToNextLevel => GetThresholdForLevel(CurrentLevel);

    public event Action<float, float> OnExperienceChanged; // current, threshold
    public event Action<int> OnLevepUp;

    private static readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    public void HandleDamageDealt(float maxHealth, float damageTaken)
    {
        if (!CanDrop) return;
        if (DropPrefab == null) return;
        if (damageTaken <= 0f || maxHealth <= 0f) return;

        float damageFraction = damageTaken / maxHealth;
        float dropAmount = TotalExperienceValue * damageFraction * OnHitDropFraction;

        if (dropAmount <= 0f) return;

        SpawnDrop(dropAmount);
        _droppedSoFar += dropAmount;
    }

    public void HandleDeath()
    {
        if (!CanDrop) return;
        if (DropPrefab == null) return;

        float reserve = Mathf.Max(0f, TotalExperienceValue - _droppedSoFar);
        if (reserve <= 0f || DeathFountainDropCount <= 0) return;

        float perDrop = reserve / DeathFountainDropCount;

        for (int i = 0; i < DeathFountainDropCount; i++)
        {
            SpawnDrop(perDrop);
        }
    }

    public void AddExperience(float amount)
    {
        if (amount <= 0f) return;

        CurrentExperience += amount;

        while (CurrentExperience >= ExperienceToNextLevel)
        {
            CurrentExperience -= ExperienceToNextLevel;
            CurrentLevel++;
            OnLevepUp?.Invoke(CurrentLevel);
        }

        OnExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
    }

    public void Reset()
    {
        //CurrentExperience = 0f;
        //CurrentLevel = 1;
        _droppedSoFar = 0f;
        //OnExperienceChanged?.Invoke(CurrentExperience, ExperienceToNextLevel);
    }

    private void SpawnDrop(float value)
    {
        MagnetDropModule drop = DropPrefab.Instantiate<MagnetDropModule>();
        if (drop == null)
        {
            GD.PrintErr("ProgressionModule: DropPrefab root is not a MagnetDropModule.");
            return;
        }

        GetTree().CurrentScene.AddChild(drop);
        drop.GlobalPosition = GlobalPosition;
        drop.GlobalPosition += Vector3.Up * DropSpawnHeightOffset;
        
        drop.Value = value;

        // Random outward + upward velocity
        Vector3 randomDir = new Vector3(
            _rng.RandfRange(-1f, 1f),
            0f,
            _rng.RandfRange(-1f, 1f)
        ).Normalized();

        Vector3 launch = randomDir * DropLaunchSpeed + Vector3.Up * DropLaunchUpwardBias;
        drop.Launch(launch);
    }
    
    private float GetThresholdForLevel(int level)
    {
        return BaseLevelThreshold * level;
    }
}
