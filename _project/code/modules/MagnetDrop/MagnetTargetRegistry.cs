using Godot;
using System;
using System.Collections.Generic;

public static class MagnetTargetRegistry
{
    private static readonly HashSet<MagnetTargetModule> _targets = new HashSet<MagnetTargetModule>();

    public static void Register(MagnetTargetModule target)
    {
        _targets.Add(target);
    }

    public static void Unregister(MagnetTargetModule target)
    {
        _targets.Remove(target);
    }

    public static IEnumerable<MagnetTargetModule> GetAll()
    {
        return _targets;
    }
}
