using Godot;
using System;

public partial class SunRuntimeSetMember : Node
{
	[Export] public SunRuntimeSet RuntimeSet;

	private DirectionalLight3D _sun;

	public override void _Ready()
	{
		_sun = GetOwner() as DirectionalLight3D;

		if (_sun == null)
		{
			GD.PrintErr("SunRuntimeSetMember: Owner is not a DirectionalLight3D.");
			return;
		}

		if (RuntimeSet == null)
		{
			GD.PrintErr("SunRuntimeSetMember: No RuntimeSet assigned.");
			return;
		}

		RuntimeSet.Add(_sun);
	}

	public override void _ExitTree()
    {
        if (_sun != null && RuntimeSet != null)
		{
			RuntimeSet.Remove(_sun);
		}
    }
}
