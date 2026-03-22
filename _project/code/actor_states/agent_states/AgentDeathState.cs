using Godot;
using System;

public partial class AgentDeathState : ActorState
{
	//private float _despawnTimer;
	private float MeltDuration = 1.5f;
	private bool _meltStarted;

    public AgentDeathState(ActorCore core) : base(core)
    {
		
    }

    public override void EnterState()
    {
        GD.Print("Agent has died.");

		_core.Velocity = Vector3.Zero;

		// Disable hitbox and hurtbox so there's no combat interaction

		if (_core.HitBox != null)
		{
			_core.SetHitBoxEnabled(false);
		}

		if (_core.HurtBox != null)
		{
			_core.SetHurtBoxEnabled(false);
		}

		_core.SetCollisionShapeEnabled(false);

		StartMeltEffect();
    }

	public override void ProcessState(float delta)
    {
        
    }

    public override void ExitState()
    {
        
    }

    private void StartMeltEffect()
	{
		MeshInstance3D meshInstance = _core.BodyMesh;

		if (meshInstance == null)
		{
			GD.PrintErr("AgentDeathState: No MeshInstance3D found for melt effect.");
			return;
		}

		ShaderMaterial sharedMaterial = meshInstance.GetSurfaceOverrideMaterial(0) as ShaderMaterial
									?? meshInstance.MaterialOverride as ShaderMaterial;
		
		if (sharedMaterial == null)
		{
			GD.PrintErr("AgentDeathState: No ShaderMaterial found on mesh.");
			return;
		}

		// Duplicate the material on death to have unique animation
		// We assign to SurfaceOverrideMaterial, not MaterialOverride
		// as Status assigns the design-time config material here.
		ShaderMaterial material = (ShaderMaterial)sharedMaterial.Duplicate();
		meshInstance.SetSurfaceOverrideMaterial(0, material);

		Tween tween = _core.CreateTween();
		tween.TweenMethod(Callable.From((float val) => material.SetShaderParameter("progress", val)), 0.0f, 1.0f, MeltDuration
							).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Quad);


		// Drop the face mesh to the ground
		if (_core.FaceMesh != null)
		{
			float groundY = 0.1f; // Adjust to sit on top of puddle
			float dropDuration = 0.6f;

			Tween faceTween = _core.CreateTween();
			Vector3 targetPos = new Vector3(
				_core.FaceMesh.Position.X,
				groundY,
				_core.FaceMesh.Position.Z
			);

			faceTween.TweenInterval(1.0f);
			faceTween.TweenProperty(_core.FaceMesh, "position", targetPos, dropDuration)
				.SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Bounce);
		}

		_meltStarted = true;
	}
}
