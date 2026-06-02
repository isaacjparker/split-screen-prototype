using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Placed in the home room of the maze. Manages villager banking,
/// patrol bounds, respawn position, and morning orb income.
/// </summary>
public partial class HomeRoom : Node3D, IPatrolRegion
{
    [ExportGroup("References")]
    [Export] public Node3D HeartNode;
    [Export] public Area3D ThresholdArea;
    [Export] public Marker3D RespawnPoint;

    [ExportGroup("Patrol Bounds")]
    [Export] public float PatrolInnerRadius = 2.5f;
    [Export] public float PatrolOuterRadius = 8.0f;

    [ExportGroup("Morning Orb Income")]
    [Export] public PackedScene DropPrefab;
    [Export] public float OrbValuePerVillager = 5f;
    [Export] public int OrbsPerVillagerPerMorning = 2;

    public static HomeRoom Instance { get; private set; }

    public Vector3 HeartPosition => HeartNode != null ? HeartNode.GlobalPosition : GlobalPosition;
    public Vector3 RespawnPosition => RespawnPoint != null ? RespawnPoint.GlobalPosition : GlobalPosition;

    public IReadOnlyList<ActorCore> BankedVillagers => _bankedVillagers;
    private readonly List<ActorCore> _bankedVillagers = new List<ActorCore>();

    public event Action<ActorCore, ActorCore> OnVillagerBanked; // villager, rescuer

    private static readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    public override void _Ready()
    {
        Instance = this;

        if (ThresholdArea != null)
            ThresholdArea.BodyEntered += OnThresholdBodyEntered;

        DayNightCycleManager.Instance.DayStarted += HandleMorning;
    }

    public override void _ExitTree()
    {
        if (ThresholdArea != null)
            ThresholdArea.BodyEntered -= OnThresholdBodyEntered;

        if (DayNightCycleManager.Instance != null)
            DayNightCycleManager.Instance.DayStarted -= HandleMorning;
    }

    public void BankVillager(ActorCore villager, ActorCore rescuer)
    {
        if (_bankedVillagers.Contains(villager)) return;

        _bankedVillagers.Add(villager);
        villager.TreeExiting += () => _bankedVillagers.Remove(villager);

        // Home becomes this villager's patrol turf so the shared AgentPatrolState
        // wanders it around the heart rather than its original spawn point.
        villager.PatrolRegion = this;

        OnVillagerBanked?.Invoke(villager, rescuer);
    }

    // Returns a random patrol point in the annular region around the heart.
    public Vector3 GetRandomPatrolPoint()
    {
        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float radius = _rng.RandfRange(PatrolInnerRadius, PatrolOuterRadius);

        Vector3 offset = new Vector3(
            Mathf.Cos(angle) * radius,
            0f,
            Mathf.Sin(angle) * radius
        );

        return HeartPosition + offset;
    }

    private void OnThresholdBodyEntered(Node3D body)
    {
        if (body is not ActorCore villager) return;
        if (!villager.IsInGroup("villagers")) return;

        // The villager state machine will handle the full banking logic;
        // we emit the signal so the state can react cleanly.
        OnVillagerBanked?.Invoke(villager, null);
    }

    private void HandleMorning()
    {
        if (DropPrefab == null) return;

        _bankedVillagers.RemoveAll(v => !IsInstanceValid(v));

        foreach (ActorCore villager in _bankedVillagers)
        {
            for (int i = 0; i < OrbsPerVillagerPerMorning; i++)
                SpawnOrbAtHeart(OrbValuePerVillager);
        }
    }

    private void SpawnOrbAtHeart(float value)
    {
        MagnetDropModule drop = DropPrefab.Instantiate<MagnetDropModule>();
        if (drop == null) return;

        GetTree().CurrentScene.AddChild(drop);
        drop.GlobalPosition = HeartPosition + Vector3.Up * 0.8f;
        drop.Value = value;

        Vector3 randomDir = new Vector3(
            _rng.RandfRange(-1f, 1f),
            0f,
            _rng.RandfRange(-1f, 1f)
        ).Normalized();

        drop.Launch(randomDir * 3f + Vector3.Up * 2f);
    }
}
