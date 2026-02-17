using Godot;
using System;

public partial class AttackingState : ActorState
{
    private bool _nextAttackQueued = false;
    private bool _isComboWindowOpen = false;

    public AttackingState(PlayerBrain brain) : base(brain)
    {
    }

    public override void EnterState()
    {
        _brain.OnComboWindowOpen += HandleComboWindowOpen;
        _brain.OnComboWindowClose += HandleComboWindowClose;

        _nextAttackQueued = false;
        _isComboWindowOpen = false;

        // Must use Start() NOT Travel(). Travel() can lock up animation transition.
        _brain.AnimPlayback.Start(PlayerBrain.ANIM_ATK1);
        _brain.Combat.PerformMeleeLunge(_brain.CurrentTarget);
    }

	public override void ProcessState(float delta)
    {
        if (_isComboWindowOpen == false) return;

        if (_brain.IsAttackJustPressed())
        {
            _nextAttackQueued = true;
        }
    }

    
    public override void ExitState()
    {
        _brain.OnComboWindowOpen -= HandleComboWindowOpen;
        _brain.OnComboWindowClose -= HandleComboWindowClose;
    }

    /// <summary>
    /// Attempts to trigger the next attack in the combo chain based on the current animation.
    /// Returns a boolean as a safety check. If getCurrentNode() returns a non-attack
    /// animation node, we return false to allow the state machine to change states.
    /// </summary>
    private bool AdvanceCombo()
    {
        string current = _brain.AnimPlayback.GetCurrentNode();
        string nextState = "";

        switch (current)
        { 
            case PlayerBrain.ANIM_ATK1 : nextState = PlayerBrain.ANIM_ATK2; break;
            case PlayerBrain.ANIM_ATK2 : nextState = PlayerBrain.ANIM_ATK3; break;
            case PlayerBrain.ANIM_ATK3 : nextState = PlayerBrain.ANIM_ATK1; break;
        }

        if (!string.IsNullOrEmpty(nextState))
        {
            _isComboWindowOpen = false;

            _brain.AnimPlayback.Travel(nextState);
            _brain.Combat.PerformMeleeLunge(_brain.CurrentTarget);
            return true;
        }

        return false;
    }

    private void HandleComboWindowOpen()
    {
        _isComboWindowOpen = true;
    }

    private void HandleComboWindowClose()
    {
        _isComboWindowOpen = false;

        if (_nextAttackQueued)
        {
            _nextAttackQueued = false;
            if (AdvanceCombo()) return;
        }

        // If no attack queued, change state
        _brain.StateMachine.ChangeState(new IdleMoveState(_brain));
    }
}
