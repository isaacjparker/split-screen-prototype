using Godot;
using System;

/// <summary>
/// Base resource defining the contract for all attack VFX
/// Resources hold config data only - no runtime state
/// Runtime node references are owned by the caller and passed back in on subsequent calls.
/// <summary>
[Tool]
[GlobalClass]
public partial class AttackVFX : Resource
{
    /// <summary>
    /// Instantiates the visual node, attaches it to the actor, and returns it.
    /// The caller is responsible for storing the returned node in actor's runtime blackboard.
    /// The node should be invisble after equipping.
    /// <summary>
    public virtual Node3D Equip(ActorCore core) {return null;}

    /// <summary>
    /// Frees the visual node. The caller is responsible for clearing the
    /// reference from actor's runtime blackboard after this call.
    /// <summary>
    public virtual void Unequip(ActorCore core) {}

    /// <summary>
    /// Called at the start of the active window. Begins the visual effect
    /// <summary>
    public virtual void Apply(ActorCore core) {}

    /// <summary>
    /// Called at the end of the attack. For effects where the Tween does not
    /// handle full cleanup, this provides an explicit teardown hook.
    /// <summary>
    public virtual void Remove(ActorCore core) {}
}
