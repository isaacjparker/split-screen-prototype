using Godot;

/// <summary>
/// Shared wander/guard state. The agent wanders its patrol turf and scans for
/// hostiles each poll; spotting one switches to pursuit.
///
/// Turf priority:
///   1. _core.PatrolRegion (an EnemySpawner's room area, or HomeRoom for banked villagers)
///   2. fallback: wander within FallbackRadius of _core.InitialSpawnPosition (hand-placed actors)
///
/// Villagers reuse this safely: their MaxTargetScanRange is 0, so the scan finds nothing
/// and they never enter pursuit.
/// </summary>
public partial class AgentPatrolState : ActorState
{
    private const float ArrivalRadius = 1.5f;
    private const float WaypointIdleDuration = 1.5f;
    private const float FallbackRadius = 6.0f;

    private static readonly RandomNumberGenerator _rng = new RandomNumberGenerator();

    private Vector3 _currentWaypoint;
    private float _idleTimer;
    private float _scanTimer;
    private bool _hasWaypoint;

    public AgentPatrolState(ActorCore core) : base(core) { }

    public override void EnterState()
    {
        _hasWaypoint = false;
        _idleTimer = 0f;
        _scanTimer = 0f;
    }

    public override void ProcessState(float delta)
    {
        // --- Detection: spotting a hostile interrupts the patrol ---
        _scanTimer -= delta;
        if (_scanTimer <= 0f)
        {
            _scanTimer = _status.TargetingPollingRate;

            ActorCore foundTarget = CombatUtils.GetHighestPriorityTarget(
                _core,
                -_core.GlobalTransform.Basis.Z,
                _status.MaxTargetScanRange,
                _status.MaxTargetScanAngle
            );

            if (foundTarget != null)
            {
                _status.CurrentTarget = foundTarget;
                _core.StateMachine.ChangeState(new AgentPursueState(_core));
                return;
            }
        }

        // --- Wander ---
        if (_idleTimer > 0f)
        {
            _core.Motor.ProcessLocomotion(Vector3.Zero, _status.MaxSpeed, delta);
            _idleTimer -= delta;
            return;
        }

        if (!_hasWaypoint || _core.Motor.IsNavTargetReached(_currentWaypoint, ArrivalRadius))
        {
            _currentWaypoint = GetPatrolPoint();
            _hasWaypoint = true;
            _idleTimer = WaypointIdleDuration;
            return;
        }

        _core.Motor.ProcessNavLocomotion(_currentWaypoint, _status.MaxSpeed, delta);
    }

    public override void ExitState() { }

    private Vector3 GetPatrolPoint()
    {
        if (_core.PatrolRegion != null)
            return _core.PatrolRegion.GetRandomPatrolPoint();

        // Hand-placed actor with no spawner: wander around where it was placed.
        float angle = _rng.RandfRange(0f, Mathf.Tau);
        float radius = _rng.RandfRange(0f, FallbackRadius);
        Vector3 offset = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        return _core.InitialSpawnPosition + offset;
    }
}
