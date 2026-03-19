using Godot;
using System;

public partial class RuntimeSets : Node
{
	[Export] public ActorRuntimeSet Actors;

	public static RuntimeSets Instance {get; private set;}

	public override void _Ready()
	{
		Instance = this;
	}
}
