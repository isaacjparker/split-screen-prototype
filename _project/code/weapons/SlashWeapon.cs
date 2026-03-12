using Godot;

public partial class SlashWeapon : BaseWeapon
{
	private ActorCore _core;
	private Tween _wipeOutTween;

	public override void Equip(ActorCore core)
	{
		_core = core;

		_core.OnAttackStarted += HandleAttackStarted;
		_core.OnAttackActive += HandleAttackActive;
		_core.OnAttackEnded += HandleAttackEnded;
	}

	public override void Unequip()
	{
		if (_core == null) return;

        _core.OnAttackStarted -= HandleAttackStarted;
        _core.OnAttackActive -= HandleAttackActive;
        _core.OnAttackEnded -= HandleAttackEnded;

        Detach(_core);
        _core = null;
	}

	private SlashVFX GetSlashVFX()
	{
		return _core.Status.CurrentAttack?.VFX as SlashVFX;
	}

	private void HandleAttackStarted(ActorCore core)
	{
		SlashVFX vfx = GetSlashVFX();
		if (vfx == null) return;

    	core.Status.AttackVfxNode = Attach(core, vfx);
	}

	private void HandleAttackActive(ActorCore core)
	{
		SlashVFX vfx = GetSlashVFX();
		if (vfx == null) return;

    	Apply(core, vfx);
	}

	private void HandleAttackEnded(ActorCore core)
	{
    	Detach(core);
	}

	private Sprite3D Attach(ActorCore core, SlashVFX vfx)
	{
		if (vfx.SlashTemplate == null)
        {
            GD.PushWarning("SlashVFX: SlashScene is null. Effect will not play.");
            return null;
        }

        Sprite3D slashNode = vfx.SlashTemplate.Instantiate<Sprite3D>();
        slashNode.FlipH = vfx.FlipH;
        slashNode.Texture = vfx.SlashTexture;

		ShaderMaterial mat = slashNode.MaterialOverride as ShaderMaterial;
		if (mat != null)
		{
			mat = mat.Duplicate() as ShaderMaterial;
			slashNode.MaterialOverride = mat;
			mat.SetShaderParameter("slash_texture", vfx.SlashTexture);
			mat.SetShaderParameter("arc_degrees", vfx.WipeArcDegrees);
			mat.SetShaderParameter("progress", 1.0f);
		}


        core.AddChild(slashNode);
		slashNode.Position = new Vector3(0, 1, 0);
		slashNode.Visible = false;

        return slashNode;
	}

	private void Apply(ActorCore core, SlashVFX vfx)
	{
		Sprite3D slashNode = core.Status.AttackVfxNode as Sprite3D;
        if (slashNode == null) 
        {
            GD.Print("Slashnode is null.");
            return;
        }

		slashNode.Visible = true;

		ShaderMaterial mat = slashNode.MaterialOverride as ShaderMaterial;
		if (mat == null) return;

        // Apply is called at the start of the active Window
		// Remaining time until attack fully ends = Active + Recovery
		// Wipeout should finish exactly at COmboWindow.Y, so it begins at:
		// (Active + Recovery) - WipeOutDuration
		float remainingTime = core.Status.CurrentAttack.Active + core.Status.CurrentAttack.Recovery;
		float holdDuration = Mathf.Max(0f, remainingTime - vfx.WipeOutDuration);

		if (_wipeOutTween != null && _wipeOutTween.IsValid())
		{
			_wipeOutTween.Kill();
		}

		_wipeOutTween = CreateTween();
		_wipeOutTween.TweenInterval(holdDuration);
		_wipeOutTween.TweenProperty(mat, "shader_parameter/progress", 0.0f, vfx.WipeOutDuration);
	}

	private void Detach(ActorCore core)
	{
		if (_wipeOutTween != null && _wipeOutTween.IsValid())
		{
			_wipeOutTween.Kill();
			_wipeOutTween = null;
		}

		if (core.Status.AttackVfxNode != null && GodotObject.IsInstanceValid(core.Status.AttackVfxNode))
        {
            core.Status.AttackVfxNode.QueueFree();
			core.Status.AttackVfxNode = null;
        }
	}
}
