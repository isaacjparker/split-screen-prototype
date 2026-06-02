using Godot;

/// <summary>
/// Player-only feedback + progression glue. Listens to the sibling ProgressionModule's
/// level-ups (driven by picking up orbs), steps the player's attack DamageMultiplier up a
/// notch each level, and fires the upgrade presentation:
///   - an optional one-shot VFX scene (plug your own in via UpgradeVfx),
///   - a floating "+x% ATK" label above the player's head (placeholder until art lands).
///
/// Add this node only to the player actor. Enemies have no upgrade module, so their
/// DamageMultiplier stays at the default 1.
/// </summary>
public partial class AttackUpgradeModule : Node3D
{
    [ExportGroup("Attack Curve")]
    /// <summary>Fraction added to the attack multiplier per level (0.15 = +15% per level).</summary>
    [Export] public float DamagePerLevel = 0.15f;

    [ExportGroup("Feedback")]
    /// <summary>Optional. One-shot VFX spawned at the player on each upgrade. Plug yours in here.</summary>
    [Export] public PackedScene UpgradeVfx;
    /// <summary>Seconds before the spawned VFX is auto-freed. Set 0 if the VFX frees itself.</summary>
    [Export] public float VfxLifetime = 2f;
    /// <summary>Floating "+x% ATK" label scene (placeholder feedback).</summary>
    [Export] public PackedScene FloatingLabelScene;
    /// <summary>Local offset from the player where feedback spawns (roughly head height).</summary>
    [Export] public Vector3 SpawnOffset = new Vector3(0f, 2.2f, 0f);

    private ProgressionModule _progression;
    private StatusModule _status;

    public override void _Ready()
    {
        Node parent = GetParent();
        if (parent == null) return;

        _progression = parent.GetNodeOrNull<ProgressionModule>("ProgressionModule");
        _status = parent.GetNodeOrNull<StatusModule>("StatusModule");

        if (_progression != null)
            _progression.OnLevepUp += HandleLevelUp;
    }

    public override void _ExitTree()
    {
        if (_progression != null)
            _progression.OnLevepUp -= HandleLevelUp;
    }

    private void HandleLevelUp(int newLevel)
    {
        // Deterministic curve: level 1 = x1, each level beyond adds DamagePerLevel.
        if (_status != null)
            _status.DamageMultiplier = 1f + (newLevel - 1) * DamagePerLevel;

        PlayUpgradeFeedback();
    }

    private void PlayUpgradeFeedback()
    {
        Node scene = GetTree().CurrentScene;
        if (scene == null) return;

        Vector3 spawnPos = GlobalPosition + SpawnOffset;

        // --- Plug-in VFX seam ---
        if (UpgradeVfx != null)
        {
            Node3D vfx = UpgradeVfx.Instantiate<Node3D>();
            if (vfx != null)
            {
                scene.AddChild(vfx);
                vfx.GlobalPosition = spawnPos;

                if (VfxLifetime > 0f)
                {
                    SceneTreeTimer timer = GetTree().CreateTimer(VfxLifetime);
                    timer.Timeout += () => { if (IsInstanceValid(vfx)) vfx.QueueFree(); };
                }
            }
        }

        // --- Placeholder floating label ---
        if (FloatingLabelScene != null)
        {
            FloatingLabel label = FloatingLabelScene.Instantiate<FloatingLabel>();
            if (label != null)
            {
                scene.AddChild(label);
                label.GlobalPosition = spawnPos;
                label.Setup($"+{Mathf.RoundToInt(DamagePerLevel * 100f)}% ATK");
            }
        }

        // TODO (M7 audio): play an upgrade SFX here.
    }
}
