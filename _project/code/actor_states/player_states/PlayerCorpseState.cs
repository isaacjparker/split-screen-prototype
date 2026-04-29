using Godot;
using System;

public partial class PlayerCorpseState : ActorState
{
    private Label3D _resetLabel;

    public PlayerCorpseState(ActorCore core) : base(core)
    {
    }

    public override void EnterState()
    {
        _resetLabel = _core.ResetLabel;
        if (_resetLabel != null)
        {
            string buttonName = _core.StateMachine.GetInteractButtonName();
            _resetLabel.Text = $"Press {buttonName} to reset";
            _resetLabel.Visible = true;
            _resetLabel.TopLevel = true;
            _resetLabel.GlobalPosition = _core.GlobalPosition + Vector3.Up * 1.5f;
        }
    }

    public override void ProcessState(float delta)
    {
        if (_core.StateMachine.IsInteractRequested())
        {
            _core.Reset(_core.InitialSpawnPosition, _core.InitialSpawnBasis);
        }
    }

    public override void ExitState()
    {
        if (_resetLabel != null)
        {
            _resetLabel.Visible = false;
        }
    }

    
}
