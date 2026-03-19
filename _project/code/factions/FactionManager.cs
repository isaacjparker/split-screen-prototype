using Godot;

public partial class FactionManager : Node
{
	[Export] public FactionDatabase FactionDatabase;

	public static FactionManager Instance {get; private set;}

	public override void _Ready()
	{
		Instance = this;
		FactionDatabase?.Initialise();
	}

	public static FactionRelation GetRelation(Faction a, Faction b)
	{
		return Instance.FactionDatabase.GetRelation(a, b);
	}

	public static bool IsHostile(Faction a, Faction b)
	{
		return Instance.FactionDatabase.GetRelation(a, b) == FactionRelation.Hostile;
	}

	public static bool IsFriendly(Faction a, Faction b)
	{
		return Instance.FactionDatabase.GetRelation(a, b) == FactionRelation.Friendly;
	}

	public static bool IsNeutral(Faction a, Faction b)
	{
		return Instance.FactionDatabase.GetRelation(a, b) == FactionRelation.Neutral;
	}
}
