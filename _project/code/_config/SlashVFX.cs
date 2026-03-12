using Godot;
using System;
using System.Dynamic;
using System.Numerics;

[Tool]
[GlobalClass]
public partial class SlashVFX : AttackVFX
{
    [Export] public PackedScene SlashTemplate;
    [Export] public Texture2D SlashTexture;
    [Export] public bool FlipH = false;
    [Export] public float WipeArcDegrees {get; private set;} = 260.0f;
    [Export] public float WipeOutDuration {get; private set;} = 0.2f;
}
