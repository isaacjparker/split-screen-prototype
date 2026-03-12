using Godot;
using System;

public partial class EquipmentModule : Node
{
	private ActorCore _core;
	private StatusModule _status;

	public void Initialise(ActorCore core)
	{
		_core = core;
		_status = _core.Status;
	}

	public void EquipWeapon(WeaponData data)
	{
		if (_status.EquippedWeapon != null || _status.ActiveWeaponBehaviour != null)
		{
			UnequipWeapon();
		}

		_status.EquippedWeapon = data;
		_status.ActiveWeaponBehaviour = data.WeaponBehaviourScene.Instantiate() as BaseWeapon;
		this.AddChild(_status.ActiveWeaponBehaviour);
		_status.ActiveWeaponBehaviour.Equip(_core);
	}

	public void UnequipWeapon()
	{
		if (_status.ActiveWeaponBehaviour == null) return;
		
		_status.ActiveWeaponBehaviour.Unequip();
		_status.ActiveWeaponBehaviour.QueueFree();
		_status.ActiveWeaponBehaviour = null;

		_status.EquippedWeapon = null;
	}
}
