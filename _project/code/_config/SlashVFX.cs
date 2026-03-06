using Godot;
using System;
using System.Dynamic;
using System.Numerics;

[Tool]
[GlobalClass]
public partial class SlashVFX : AttackVFX
{
    [Export] public PackedScene SlashScene;
    [Export] public float WipeArcDegrees {get; private set;} = 200.0f;
    [Export] public float WipeOutDelay {get; private set;} = 0.05f;
    [Export] public float WipeOutDuration {get; private set;} = 0.2f;

    public override Node3D Equip(ActorCore core)
    {
        if (SlashScene == null)
        {
            GD.PushWarning("SlashVFX: SlashScene is null. Effect will not play.");
            return null;
        }

        Sprite3D slashNode = SlashScene.Instantiate<Sprite3D>();
        slashNode.FlipH = core.Status.CurrentAttack.FlipH;
        slashNode.Texture = core.Status.CurrentAttack.SlashSprite;
        core.AddChild(slashNode);

        ShaderMaterial material = slashNode.MaterialOverride as ShaderMaterial;

        if (material != null)
        {
            material.SetShaderParameter("slash_texture", core.Status.CurrentAttack.SlashSprite);
            material.SetShaderParameter("progress", 0f);
            material.SetShaderParameter("arc_degrees", WipeArcDegrees);
        }
        else
        {
            GD.PushWarning("SlashVFX: Instantiated node's MaterialOverride is not a ShaderMaterial. Wipe effect will not play.");
        }

        slashNode.Visible = false;
        return slashNode;
    }

    public override void Unequip(ActorCore core)
    {
        if (core.Status.AttackVfxNode != null && GodotObject.IsInstanceValid(core.Status.AttackVfxNode))
        {
            core.Status.AttackVfxNode.QueueFree();
        }
    }

    public override void Apply(ActorCore core)
    {
        Sprite3D slashNode = core.Status.AttackVfxNode as Sprite3D;
        if (slashNode == null) return;

        ShaderMaterial material = slashNode.MaterialOverride as ShaderMaterial;
        if (material == null) return;

        // Hold duration spans the remainder of the active window plus the designer-specified delay
        // Apply() is called at the start of the active window, so we don't need to include Windup.
        float holdDuration = core.Status.CurrentAttack.Active + WipeOutDelay;

        slashNode.Visible = true;
        material.SetShaderParameter("progress", 1f);

        Tween tween = slashNode.CreateTween();
        tween.TweenInterval(holdDuration);
        tween.TweenMethod(Callable.From<float>(v => material.SetShaderParameter("progress", v)), 1f, 0f, WipeOutDuration);
        tween.TweenCallback(Callable.From(() => slashNode.Visible = false));
    }

    public override void Remove(ActorCore core)
    {
        // The Tween drives the full visual lifecycle for SlashVFX.
        // If the attack is cancelled mid-tween, Unequip() handles node cleanup
        // via QueueFree(), which implicitly kills any Tweens created on that node.
    }
}
