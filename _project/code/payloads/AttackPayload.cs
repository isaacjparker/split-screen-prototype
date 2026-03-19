using Godot;
using System;

public struct AttackPayload
{
    public ActorCore SourceActor;
    public float BaseDamage;
    public float KnockbackPower;
    public float HitStopDuration;
    public float HitStopFactor;
    public Vector3 SourcePosition;
    public SoundEffectConfig AttackAudio;
    public SoundEffectConfig ImpactAudio;
}
