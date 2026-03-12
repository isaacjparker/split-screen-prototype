using Godot;
using System;

[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public String WeaponName;
    [Export] public PackedScene WeaponBehaviourScene;
    [Export] public AttackData[] Attacks;
}
