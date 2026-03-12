using Godot;
using System;

[Tool]
[GlobalClass]
public partial class AttackData : Resource
{
    [Export] public float BaseDamage;       // 5.0f
    [Export] public float KnockbackPower;   // 15.0f
    [Export] public float HitStopDuration;  // 0.2f
    [Export(PropertyHint.Range, "0,100")]
    public float HitStopFactor = 100.0f;
    [Export] public float CamShakeMagnitude = 0.5f;
    [Export] public AttackVFX VFX;
    [Export] public SoundEffectConfig AttackAudio;
    [Export] public SoundEffectConfig ImpactAudio;
    private float _windup;
    [Export] public float Windup { get => _windup; set { _windup = value; ClampCombo(); } }

    private float _active;
    [Export] public float Active { get => _active; set { _active = value; ClampCombo(); } }

    private float _recovery;
    [Export] public float Recovery { get => _recovery; set { _recovery = value; ClampCombo(); } }

    private Vector2 _comboWindow;
    [Export]
    public Vector2 ComboWindow 
    { 
        get => _comboWindow; 
        set 
        {
            _comboWindow = value;
            ClampCombo();
        } 
    }

    private void ClampCombo()
    {
        float total = _windup + _active + _recovery;
        
        if (_comboWindow.Y > total)
        {
            _comboWindow.Y = total;
            NotifyPropertyListChanged(); 
        }
    }
}
