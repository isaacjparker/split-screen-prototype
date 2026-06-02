using Godot;
using System.Collections.Generic;

/// <summary>
/// Shared bookkeeping for night waves. Not a node — spawners register themselves and
/// drive it. Owns the night counter (for difficulty) and the distance-to-heart stagger:
/// spawners furthest from home activate first, nearest last, so the horde sweeps inward.
///
/// When GameLoopManager (M6) arrives it can take over NightNumber / difficulty by setting
/// the public knobs here.
/// </summary>
public static class WaveDirector
{
    private static readonly List<EnemySpawner> _spawners = new List<EnemySpawner>();

    // --- Difficulty knobs (tunable; later driven by GameLoopManager) ---
    public static int BaseEnemiesPerPulse = 2;
    public static int EnemiesPerPulseGrowth = 1;   // +this many per pulse, per night
    public static int PerPlayerPulseBonus = 1;     // +this many per pulse, per extra player
    public static float StaggerWindow = 12f;       // seconds: furthest=0 .. nearest=StaggerWindow
    public static float EnemyHealthGrowthPerNight = 0.25f; // +fraction of base max HP per night

    public static int NightNumber { get; private set; }
    private static bool _nightCounted;

    /// <summary>
    /// Active local players, clamped to [1,4]. Counted from the live actor set rather
    /// than the multiplayer manager so this stays decoupled (and works if players
    /// join/leave mid-run).
    /// </summary>
    public static int ActivePlayerCount
    {
        get
        {
            if (RuntimeSets.Instance == null || RuntimeSets.Instance.Actors == null) return 1;

            int players = 0;
            foreach (ActorCore a in RuntimeSets.Instance.Actors.GetAll())
            {
                if (GodotObject.IsInstanceValid(a) && a.Status != null && a.Status.Faction == Faction.Player)
                    players++;
            }
            return Mathf.Clamp(players, 1, 4);
        }
    }

    /// <summary>
    /// Enemies emitted per pulse this night. Grows each night, and scales up with the
    /// number of players so a 3- or 4-player run feels as busy as a solo one.
    /// </summary>
    public static int EnemiesPerPulse =>
        BaseEnemiesPerPulse
        + Mathf.Max(0, NightNumber - 1) * EnemiesPerPulseGrowth
        + (ActivePlayerCount - 1) * PerPlayerPulseBonus;

    /// <summary>
    /// Multiplier applied to a freshly spawned enemy's base max health. Rises each night
    /// survived so the same enemies get tougher as the run goes on (player HP stays fixed).
    /// Day-1 guards spawn before any night, so they sit at x1.
    /// </summary>
    public static float EnemyHealthMultiplier =>
        1f + NightNumber * EnemyHealthGrowthPerNight;

    public static void Register(EnemySpawner s)
    {
        if (!_spawners.Contains(s)) _spawners.Add(s);
    }

    public static void Unregister(EnemySpawner s)
    {
        _spawners.Remove(s);
        if (_spawners.Count == 0)
        {
            // Level unloaded — reset so a fresh run starts at night 1.
            NightNumber = 0;
            _nightCounted = false;
        }
    }

    /// <summary>Called by every spawner on NightStarted; only the first advances the counter.</summary>
    public static void NotifyNightStarted()
    {
        if (_nightCounted) return;
        _nightCounted = true;
        NightNumber++;
    }

    public static void NotifyDayStarted()
    {
        _nightCounted = false;
    }

    /// <summary>
    /// Seconds this spawner should wait after nightfall before it begins emitting.
    /// Furthest spawner from the heart = 0 (fires first); nearest = StaggerWindow (fires last).
    /// </summary>
    public static float GetActivationDelay(EnemySpawner spawner)
    {
        Vector3 heart = HomeRoom.Instance != null ? HomeRoom.Instance.HeartPosition : Vector3.Zero;

        float myDist = spawner.GlobalPosition.DistanceTo(heart);
        float maxDist = 0f;
        float minDist = float.MaxValue;

        foreach (EnemySpawner s in _spawners)
        {
            if (!GodotObject.IsInstanceValid(s)) continue;
            float d = s.GlobalPosition.DistanceTo(heart);
            if (d > maxDist) maxDist = d;
            if (d < minDist) minDist = d;
        }

        if (maxDist - minDist < 0.001f) return 0f;

        float t = (myDist - minDist) / (maxDist - minDist); // 0 = nearest, 1 = furthest
        return (1f - t) * StaggerWindow;                     // furthest -> 0, nearest -> window
    }
}
