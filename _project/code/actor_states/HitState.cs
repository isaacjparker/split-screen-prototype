using Godot;
using System;

public partial class HitState : ActorState
{
    private Vector3 _sourcePos;
    private float _knockbackPower;
    private Tween _activeTween;

    public HitState(ActorCore core, Vector3 sourcePos, float power) : base(core)
    {
        _sourcePos = sourcePos;
        _knockbackPower = power;
    }

    public override void EnterState()
    {
        GD.Print($"Entered HitState. Knockback Power: {_knockbackPower}");

        // Play Animation (optional)

        _activeTween = _core.Motor.ApplyKnockback(_sourcePos, _knockbackPower);

        if (_activeTween != null)
        {
            _activeTween.Finished += OnKnockbackFinished;
        }
        else
        {
            ReturnToIdleMove();
        }
    }

	public override void ProcessState(float delta)
    {
        _core.Motor.ProcessForcesLocomotion(delta);
    }

    public override void ExitState()
    {
        if (_activeTween != null)
        {
            _activeTween.Finished -= OnKnockbackFinished;

            if (_activeTween.IsValid())
            {
                _activeTween.Kill();
            }

            _activeTween = null;
        }
    }

    private void OnKnockbackFinished()
    {
        GD.Print("Recovered from knockback!");
        ReturnToIdleMove();
    }

    private void ReturnToIdleMove()
    {
        _core.StateMachine.ChangeState(new IdleMoveState(_core));
    }


}
