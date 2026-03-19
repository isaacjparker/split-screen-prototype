using Godot;

[GlobalClass]
public partial class FactionRelationship : Resource
{
    [Export] public Faction FactionA;
    [Export] public Faction FactionB;
    [Export] public FactionRelation Relation = FactionRelation.Neutral;
}