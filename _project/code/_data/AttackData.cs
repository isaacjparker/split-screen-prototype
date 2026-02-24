using Godot;
using System;

[GlobalClass]
public partial class AttackData : Resource
{
    [Export] public float BaseDamage;       // 5.0f
    [Export] public float KnockbackPower;   // 15.0f
    [Export] public float HitStopDuration;  // 0.2f
    [Export] public Texture2D SlashSprite;
    [Export] public float Windup;
    [Export] public float Active;
    [Export] public float Recovery;
    [Export] public Vector2 ComboWindow;
}
