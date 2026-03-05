using Godot;
using System;

[Tool]
[GlobalClass]
public partial class SoundEffectConfig : Resource
{
    public enum SoundEffectType
    {
        Weapon,
        WeaponImpact
    }

    [Export(PropertyHint.Range, "1,10")] private int Limit = 5;
    [Export] public SoundEffectType Type {get; private set;}
    [Export] public AudioStream SoundEffect {get; private set;}
    [Export(PropertyHint.Range, "-40, 20")] public float Volume {get; private set;} = 0;
    [Export(PropertyHint.Range, "0.01, 4.0, .01")] private float PitchScale = 1.0f;
    [Export(PropertyHint.Range, "0, 1.0, .01")] private float PitchRandomness = 0f;

    private int _audioCount;

    public void ChangeAudioCount(int amount)
    {
        _audioCount = Mathf.Max(0, _audioCount + amount);
    }

    public bool HasReachedLimit()
    {
        return _audioCount >= Limit;
    }

    public float GetRandomisedPitchScale()
    {
        return Mathf.Max(0.01f, PitchScale + (float)GD.RandRange(-PitchRandomness, PitchRandomness));
    }

    public void OnAudioFinished()
    {
        ChangeAudioCount(-1);
    }
}
