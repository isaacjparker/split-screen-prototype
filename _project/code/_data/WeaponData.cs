using Godot;
using System;

[GlobalClass]
public partial class WeaponData : Resource
{
    [Export] public String WeaponName;
    [Export] public AttackData[] AttackData;
}
