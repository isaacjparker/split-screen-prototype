using Godot;

public partial class VFXModule : Node
{
	private ActorCore _core;
	private Tween _flashTween;

	public void Initialise(ActorCore core)
	{
		_core = core;

		_core.OnAttackStarted += HandleAttackStarted;
		_core.OnAttackActive += HandleAttackActive;
		_core.OnAttackEnded += HandleAttackEnded;
	}

    public override void _ExitTree()
    {
        if (_core == null) return;

		_core.OnAttackStarted -= HandleAttackStarted;
		_core.OnAttackActive -= HandleAttackActive;
		_core.OnAttackEnded -= HandleAttackEnded;
    }

	public void PlayHitFlash()
	{
		if (_core.BodyMesh == null || _core.FlashMaterial == null) return;

		// Kill previous tween if we get hit again while flashing
		if (_flashTween != null && _flashTween.IsValid())
		{
			_flashTween.Kill();
		}

		float flashDuration = _core.Status.HitFlashTimer;
		float gapDuration = _core.Status.HitFlashGap;

		_flashTween = CreateTween();

		// First flash
		_core.BodyMesh.MaterialOverride = _core.FlashMaterial;
		_flashTween.TweenInterval(flashDuration);
		_flashTween.TweenCallback(Callable.From(() => _core.BodyMesh.MaterialOverride = null));

		// Gap, then second flash
		_flashTween.TweenInterval(gapDuration);
		_flashTween.TweenCallback(Callable.From(() => _core.BodyMesh.MaterialOverride = _core.FlashMaterial));
		_flashTween.TweenInterval(flashDuration);
		_flashTween.TweenCallback(Callable.From(() => _core.BodyMesh.MaterialOverride = null));
	}

	private void HandleAttackStarted(ActorCore core)
	{
		AttackVFX vfx = core.Status.CurrentAttack?.VFX;
		if (vfx == null) return;

		core.Status.AttackVfxNode = vfx.Equip(core);
	}

	private void HandleAttackActive(ActorCore core)
	{
		AttackVFX vfx = core.Status.CurrentAttack?.VFX;
		if (vfx == null) return;

		vfx.Apply(core);
	}

	private void HandleAttackEnded(ActorCore core)
	{
		AttackVFX vfx = core.Status.CurrentAttack?.VFX;
		if (vfx == null) return;

		vfx.Unequip(core);
		core.Status.AttackVfxNode = null;
	}
}
