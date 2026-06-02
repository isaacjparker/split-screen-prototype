using Godot;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Invisible room marker. Place one in each room that should contain guards and set
/// its RoomSize to match the room. Spawns guards within the box, becomes their
/// IPatrolRegion (so they wander the room via the shared AgentPatrolState), respawns
/// them each morning, and despawns them at night.
///
/// At night it switches role: it becomes a wave emitter. Guided by the shared
/// WaveDirector, spawners furthest from the heart activate first and nearest last, so
/// the horde sweeps inward. Each spawner emits timed pulses of enemies that march on
/// the home heart (AgentWaveMarchState) until morning.
///
/// RoomSize is a per-instance value on the root, so resizing one spawner never affects
/// the others. It drives the PatrolArea box at design-time (Tool) and runtime, giving each
/// placed spawner its own uniquely-sized box without unpacking the scene.
/// </summary>
[Tool]
[GlobalClass]
public partial class EnemySpawner : Node3D, IPatrolRegion
{
    private Vector3 _roomSize = new Vector3(10f, 4f, 10f);

    /// <summary>Room bounds (the PatrolArea box is sized to this). Edit per instance.</summary>
    [Export]
    public Vector3 RoomSize
    {
        get => _roomSize;
        set
        {
            _roomSize = value;
            if (IsNodeReady()) ApplyRoomSize();
        }
    }

    [Export] public PackedScene EnemyScene;
    /// <summary>Child Area3D whose BoxShape3D defines the room bounds.</summary>
    [Export] public Area3D PatrolArea;
    [Export] public int Count = 3;
    [Export] public bool DespawnAtNight = true;

    /// <summary>
    /// When false this spawner never participates in night waves — it just populates its
    /// room each morning and (optionally) despawns at night. Set false to reuse this same
    /// node as a villager spawner.
    /// </summary>
    [Export] public bool EmitNightWaves = true;

    [ExportGroup("Night Waves")]
    /// <summary>Seconds between wave pulses once this spawner is active.</summary>
    [Export] public float PulseInterval = 4f;
    /// <summary>Hard cap on this spawner's living wave enemies (performance + pacing).</summary>
    [Export] public int MaxAlive = 8;
    /// <summary>Seconds a wave-enemy corpse lingers (melt VFX) before being freed.</summary>
    [Export] public float CorpseLinger = 2f;

    private BoxShape3D _box;
    private Node3D _boxNode;
    private static readonly RandomNumberGenerator _rng = new RandomNumberGenerator();
    private readonly List<ActorCore> _spawned = new List<ActorCore>();

    // --- Night wave state ---
    private readonly List<ActorCore> _waveEnemies = new List<ActorCore>();
    private bool _waving;
    private float _activationTimer;  // counts down to first pulse (distance stagger)
    private float _pulseTimer;

    public override void _Ready()
    {
        // Runs in editor (Tool) and at runtime: size the box from RoomSize.
        ApplyRoomSize();

        // Everything below is runtime-only.
        if (Engine.IsEditorHint()) return;

        if (EmitNightWaves) WaveDirector.Register(this);

        if (DayNightCycleManager.Instance != null)
        {
            DayNightCycleManager.Instance.DayStarted += OnDayStarted;
            DayNightCycleManager.Instance.NightStarted += OnNightStarted;
        }

        // No DayStarted fires at game start (StartDay runs immediate), so populate now.
        CallDeferred(nameof(SpawnGuards));
    }

    public override void _ExitTree()
    {
        if (Engine.IsEditorHint()) return;

        if (EmitNightWaves) WaveDirector.Unregister(this);

        if (DayNightCycleManager.Instance != null)
        {
            DayNightCycleManager.Instance.DayStarted -= OnDayStarted;
            DayNightCycleManager.Instance.NightStarted -= OnNightStarted;
        }
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint()) return;
        if (!_waving) return;

        float dt = (float)delta;

        // Distance stagger: wait out our activation delay before the first pulse.
        if (_activationTimer > 0f)
        {
            _activationTimer -= dt;
            if (_activationTimer > 0f) return;
            EmitWavePulse();        // fire immediately on activation
            _pulseTimer = PulseInterval;
            return;
        }

        _pulseTimer -= dt;
        if (_pulseTimer <= 0f)
        {
            _pulseTimer = PulseInterval;
            EmitWavePulse();
        }
    }

    // --- IPatrolRegion ---
    public Vector3 GetRandomPatrolPoint()
    {
        if (_box == null || _boxNode == null) return GlobalPosition;

        Vector3 he = _box.Size * 0.5f;
        Vector3 local = new Vector3(_rng.RandfRange(-he.X, he.X), 0f, _rng.RandfRange(-he.Z, he.Z));
        return _boxNode.GlobalTransform * local;
    }

    // --- Daytime guards ---
    public void SpawnGuards()
    {
        if (EnemyScene == null)
        {
            GD.PushWarning($"EnemySpawner '{Name}': No EnemyScene assigned.");
            return;
        }

        ClearGuards();

        for (int i = 0; i < Count; i++)
        {
            ActorCore enemy = EnemyScene.Instantiate<ActorCore>();
            AddChild(enemy);

            // Position after AddChild, then overwrite the spawn anchor the agent
            // captured during its _Ready (it saw the pre-placement origin).
            Vector3 pos = GetRandomPatrolPoint();
            enemy.GlobalPosition = pos;
            enemy.InitialSpawnPosition = pos;
            enemy.PatrolRegion = this;

            ApplyDifficultyHealth(enemy);

            _spawned.Add(enemy);
        }
    }

    /// <summary>
    /// Applies the per-night enemy HP curve to a freshly spawned actor. Only affects
    /// Enemy-faction actors, so reusing this spawner for villagers leaves them untouched.
    /// </summary>
    private static void ApplyDifficultyHealth(ActorCore actor)
    {
        if (actor.Status == null || actor.Status.Faction != Faction.Enemy) return;

        float scaled = actor.Status.MaxHealth * WaveDirector.EnemyHealthMultiplier;
        actor.Status.SetMaxHealth(scaled, refill: true);
    }

    public void ClearGuards()
    {
        // A villager that's been rescued and banked is no longer ours to clean up — it
        // belongs to the home room now. Leave banked actors alive and forget them.
        IReadOnlyList<ActorCore> banked = HomeRoom.Instance?.BankedVillagers;

        foreach (ActorCore e in _spawned)
        {
            if (!IsInstanceValid(e)) continue;
            if (banked != null && banked.Contains(e)) continue;
            e.QueueFree();
        }
        _spawned.Clear();
    }

    // --- Night waves ---
    private void EmitWavePulse()
    {
        if (EnemyScene == null) return;

        // Prune dead/freed before counting against the cap.
        _waveEnemies.RemoveAll(e => !IsInstanceValid(e) || !e.Status.IsAlive);

        int budget = Mathf.Min(WaveDirector.EnemiesPerPulse, MaxAlive - _waveEnemies.Count);
        for (int i = 0; i < budget; i++)
            SpawnWaveEnemy();
    }

    private void SpawnWaveEnemy()
    {
        ActorCore enemy = EnemyScene.Instantiate<ActorCore>();
        AddChild(enemy);

        Vector3 pos = GetRandomPatrolPoint();
        enemy.GlobalPosition = pos;
        enemy.InitialSpawnPosition = pos;

        ApplyDifficultyHealth(enemy);

        // Switch the brain from "guard this room" to "march on the heart".
        if (enemy.StateMachine is AgentSM sm)
        {
            sm.WaveMode = true;
            enemy.StateMachine.ChangeState(new AgentWaveMarchState(enemy));
        }

        // AgentDeathState plays the melt VFX but doesn't free the body — clean it up
        // a moment after it dies so corpses don't pile up across the night.
        enemy.OnDeath += OnWaveEnemyDied;

        _waveEnemies.Add(enemy);
    }

    private void OnWaveEnemyDied(ActorCore enemy)
    {
        enemy.OnDeath -= OnWaveEnemyDied;
        _waveEnemies.Remove(enemy);

        if (!IsInstanceValid(enemy)) return;

        SceneTreeTimer timer = GetTree().CreateTimer(CorpseLinger);
        timer.Timeout += () =>
        {
            if (IsInstanceValid(enemy)) enemy.QueueFree();
        };
    }

    private void OnDayStarted()
    {
        if (EmitNightWaves)
        {
            WaveDirector.NotifyDayStarted();

            // Day stops new pulses, but enemies already loose in the maze keep coming —
            // players have to hunt down every straggler. A late wave enemy still marching
            // on the heart can now cross paths with the morning's fresh guards.
            _waving = false;
        }

        SpawnGuards();
    }

    private void OnNightStarted()
    {
        if (DespawnAtNight) ClearGuards();

        if (!EmitNightWaves) return;

        WaveDirector.NotifyNightStarted();

        // Begin waves; furthest spawners get a 0 delay, nearest wait out the stagger window.
        _waving = true;
        _activationTimer = WaveDirector.GetActivationDelay(this);
        _pulseTimer = PulseInterval;
    }

    /// <summary>
    /// Finds the PatrolArea's CollisionShape3D, ensures it owns a unique BoxShape3D
    /// (so this instance's size is independent of every other spawner), and sizes it.
    /// </summary>
    private void ApplyRoomSize()
    {
        if (PatrolArea == null) return;

        CollisionShape3D cs = null;
        foreach (Node child in PatrolArea.GetChildren())
        {
            if (child is CollisionShape3D found)
            {
                cs = found;
                break;
            }
        }

        if (cs == null) return;

        BoxShape3D box = cs.Shape as BoxShape3D;

        // Make the shape unique to this instance so resizing never touches the shared resource.
        if (box == null)
        {
            box = new BoxShape3D { ResourceLocalToScene = true };
            cs.Shape = box;
        }
        else if (!box.ResourceLocalToScene)
        {
            box = (BoxShape3D)box.Duplicate();
            box.ResourceLocalToScene = true;
            cs.Shape = box;
        }

        box.Size = _roomSize;

        _box = box;
        _boxNode = cs;
    }
}
