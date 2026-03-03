using Godot;

public partial class VFXModule : Node
{
	private ActorCore _core;
	private Tween _flashTween;

	public void Initialise(ActorCore core)
	{
		_core = core;
	}

	public void PlayHitFlash()
	{
		if (_core.BodyMesh == null || _core.FlashMaterial == null) return;

		// Kill previous tween if we get hit again while flashing
		if (_flashTween != null && _flashTween.IsValid())
		{
			_flashTween.Kill();
		}

		_flashTween = CreateTween();
		_core.BodyMesh.MaterialOverride = _core.FlashMaterial;
		_flashTween.TweenInterval(_core.Status.HitFlashTimer);
		_flashTween.TweenCallback(Callable.From(() => _core.BodyMesh.MaterialOverride = null));
	}
}
