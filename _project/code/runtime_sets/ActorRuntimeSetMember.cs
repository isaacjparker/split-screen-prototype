using Godot;

public partial class ActorRuntimeSetMember : Node
{
	[Export] public ActorRuntimeSet RuntimeSet;

	private ActorCore _actor;

	public override void _Ready()
	{
		_actor = GetOwner() as ActorCore;

		if (_actor == null)
		{
			GD.PrintErr("ActorRuntimeSetMember: Owner is not an ActorCore.");
			return;
		}

		if (RuntimeSet == null)
		{
			GD.PrintErr("ActorRuntimeSetMember: No RuntimeSet assigned.");
			return;
		}

		RuntimeSet.Add(_actor);
		_actor.OnDeath += RemoveFromRuntimeSet;
	}

    public override void _ExitTree()
    {
        if (_actor != null && RuntimeSet != null)
		{
			RuntimeSet.Remove(_actor);
		}
    }

	private void RemoveFromRuntimeSet(ActorCore actor)
	{
		RuntimeSet.Remove(actor);
	}
}
