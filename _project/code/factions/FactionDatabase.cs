using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class FactionDatabase : Resource
{
    [Export] public FactionRelationship[] Relationships;

    private Dictionary<(Faction, Faction), FactionRelation> _lookup;

    public void Initialise()
    {
        _lookup = new Dictionary<(Faction, Faction), FactionRelation>();

        if (Relationships == null) return;

        foreach(FactionRelationship rel in Relationships)
        {
            _lookup[(rel.FactionA, rel.FactionB)] = rel.Relation;
            _lookup[(rel.FactionB, rel.FactionA)] = rel.Relation;
        }
    }

    public FactionRelation GetRelation(Faction a, Faction b)
    {
        if (a == b) return FactionRelation.Friendly;

        if (_lookup != null && _lookup.TryGetValue((a, b), out FactionRelation relation))
        {
            return relation;
        }

        return FactionRelation.Neutral;
    }
}
