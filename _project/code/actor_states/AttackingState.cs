using Godot;
using System;

public partial class AttackingState : ActorState
{
    private bool _nextAttackQueued = false;
    private bool _isComboWindowOpen = false;

    public AttackingState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        _core.OnComboWindowOpen += HandleComboWindowOpen;
        _core.OnComboWindowClose += HandleComboWindowClose;

        _nextAttackQueued = false;
        _isComboWindowOpen = false;

        // Must use Start() NOT Travel(). Travel() can lock up animation transition.
        _core.AnimPlayback.Start(ActorCore.ANIM_ATK1);
        _core.Combat.PerformMeleeLunge(_core.CurrentTarget);
    }

	public override void ProcessState(float delta)
    {
        _core.Motor.ProcessForcesLocomotion(delta);

        if (_isComboWindowOpen == false) return;

        if (_core.ActorInput.IsAttackRequested())
        {
            _nextAttackQueued = true;
        }
    }

    
    public override void ExitState()
    {
        _core.OnComboWindowOpen -= HandleComboWindowOpen;
        _core.OnComboWindowClose -= HandleComboWindowClose;

        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;
    }

    /// <summary>
    /// Attempts to trigger the next attack in the combo chain based on the current animation.
    /// Returns a boolean as a safety check. If getCurrentNode() returns a non-attack
    /// animation node, we return false to allow the state machine to change states.
    /// </summary>
    private bool AdvanceCombo()
    {
        string current = _core.AnimPlayback.GetCurrentNode();
        string nextState = "";

        switch (current)
        { 
            case ActorCore.ANIM_ATK1 : nextState = ActorCore.ANIM_ATK2; break;
            case ActorCore.ANIM_ATK2 : nextState = ActorCore.ANIM_ATK3; break;
            case ActorCore.ANIM_ATK3 : nextState = ActorCore.ANIM_ATK1; break;
        }

        if (!string.IsNullOrEmpty(nextState))
        {
            _isComboWindowOpen = false;

            _core.AnimPlayback.Travel(nextState);
            _core.Combat.PerformMeleeLunge(_core.CurrentTarget);
            return true;
        }

        return false;
    }

    private void HandleComboWindowOpen()
    {
        _isComboWindowOpen = true;

        _core.HitBox.SetPayload(_core.Combat.BuildAttackPayload());

        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Inherit;
    }

    private void HandleComboWindowClose()
    {
        _isComboWindowOpen = false;

        _core.HitBox.ProcessMode = Node.ProcessModeEnum.Disabled;

        if (_nextAttackQueued)
        {
            _nextAttackQueued = false;
            if (AdvanceCombo()) return;
        }

        // If no attack queued, change state
        _core.StateMachine.ChangeState(new IdleMoveState(_core));
    }
}
