using Godot;
using System;

/// <summary>
/// Night-wave behaviour. The enemy marches toward the home heart (where banked
/// villagers shelter), navigating corridors via the navmesh. While marching it
/// scans for hostiles; if a player or banked villager enters its cone it hands off
/// to the normal pursue/combat chain. When that target is lost it returns here
/// (via AgentSM.CreateReturnState) and resumes the march — it never "gives up" and
/// leashes home like a guard does.
/// </summary>
public partial class AgentWaveMarchState : ActorState
{
    private float _scanTimer;

    public AgentWaveMarchState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        _scanTimer = 0f;
    }

    public override void ProcessState(float delta)
    {
        // Look for someone to fight on the way in.
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

        // March to the heart. If there's no home room (shouldn't happen at night),
        // just hold position.
        if (HomeRoom.Instance == null) return;

        _core.Motor.ProcessNavLocomotion(HomeRoom.Instance.HeartPosition, _status.MaxSpeed, delta);
    }

    public override void ExitState()
    {
    }
}
