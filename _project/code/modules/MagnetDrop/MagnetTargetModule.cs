using Godot;
using System;

public partial class MagnetTargetModule : Node3D
{
    [Export] public bool Active = true;

    public event Action<MagnetDropModule> OnAbsorb;

    public override void _EnterTree()
    {
        MagnetTargetRegistry.Register(this);
    }

    public override void _ExitTree()
    {
        MagnetTargetRegistry.Unregister(this);
    }

    public void HandleAbsorb(MagnetDropModule drop)
    {
        OnAbsorb?.Invoke(drop);
    }
}
