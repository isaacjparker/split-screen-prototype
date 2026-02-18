using Godot;
using System;

public partial class HitBox : Area3D
{
    public AttackPayload Payload { get; private set; }

    public void SetPayload(AttackPayload payload)
    {
        Payload = payload;
    }
}
