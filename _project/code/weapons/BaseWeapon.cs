using Godot;
using System;

public partial class BaseWeapon : Node
{
	public virtual void Equip(ActorCore core){}
	public virtual void Unequip(){}
}
