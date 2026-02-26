using Godot;
using System;

public partial class CameraController : Camera3D
{
    // Config
    [Export] private Vector3 _camOffset;
    [Export] private Vector3 _lookAtOffset;
    [Export] private float _smoothingAccel = 5f;
    [Export] private float _catchUpAccel = 12f;
    [Export] private float _camShakeNoiseFreq = 2.0f;

    // Runtime
    private int _playerSlot;
    private ActorCore _actorCore;
    private FastNoiseLite _noise;

    private float _hitStopTimer = 0f;
    private float _dashTimer = 0f;
    private float _dashDragFactor = 1f;

    private Vector3 _currentFollowPos;
    private float _currentAccelRate;

    private float _shakeTimer = 0f;
    private float _shakeDuration = 0f;
    private float _shakeMagnitude = 0f;
    private float _noiseY = 0f;

    public void InitializeCamera(ActorCore actorCore, int playerSlot)
    {
        if (actorCore == null)
        {
            GD.PushWarning("CameraController: actorCore is null, failed to initialize camera.");
            return;
        }

        ClearTarget();

        _actorCore = actorCore;
        _playerSlot = playerSlot;

        if (_noise == null) _noise = new FastNoiseLite();
        _noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
        _noise.Frequency = _camShakeNoiseFreq;

        _actorCore.OnCameraShake += HandleCameraShake;
        _actorCore.OnHitStop += HandleHitStop;
        _actorCore.OnDash += HandleDash;

        _currentAccelRate = _smoothingAccel;

        _currentFollowPos = _actorCore.GlobalPosition + _camOffset;
        GlobalPosition = _currentFollowPos;

        LookAt(_actorCore.GlobalPosition + _lookAtOffset, Vector3.Up);
    }

    public override void _Process(double delta)
    { 
		// IsInstanceValid checks both for null AND if the object was freed/disposed
        if (!GodotObject.IsInstanceValid(_actorCore))
        {
            ClearTarget();
            return;
        }

        float fDelta = (float)delta;

        // If Hitstop, simply freeze everything
        if (_hitStopTimer > 0)
        {
            _hitStopTimer -= fDelta;
            return;
        }


        Vector3 lookAtTarget = _actorCore.GlobalPosition + _lookAtOffset;
        Vector3 targetPos = lookAtTarget + _camOffset;

        float targetAccel = _smoothingAccel;

        if (_dashTimer > 0)
        {
            // We are dashing - lag
            _dashTimer -= fDelta;
            targetAccel = _smoothingAccel * _dashDragFactor;
        }
        else
        {
            float dist = _currentFollowPos.DistanceTo(targetPos);

            if (dist > 1.0f)
            {
                float catchUpWeight = Mathf.Clamp((dist - 1.0f) / 4.0f, 0f, 1f);
                targetAccel = Mathf.Lerp(_smoothingAccel, _catchUpAccel, catchUpWeight);
            }
        }

        _currentAccelRate = Mathf.Lerp(_currentAccelRate, targetAccel, fDelta * 5f);

        // Apply movement
        float blend = Mathf.Clamp(_currentAccelRate * fDelta, 0f, 1f);
        _currentFollowPos = _currentFollowPos.Lerp(targetPos, blend);
        
        Vector3 shakeOffset = Vector3.Zero;

        if (_shakeTimer > 0)
        {
            _shakeTimer -= fDelta;

            // Shake strength
            float trauma = _shakeTimer / _shakeDuration;
            float currentStrength = _shakeMagnitude * (trauma * trauma);

            // Scroll noise texture
            _noiseY += fDelta * 50f;

            float x = _noise.GetNoise2D(_noiseY, 0) * currentStrength;
            float y = _noise.GetNoise2D(0, _noiseY) * currentStrength;

            shakeOffset = new Vector3(x, y, 0);
        }

        GlobalPosition = _currentFollowPos + shakeOffset;
    }

    public override void _ExitTree()
    {
        ClearTarget();
    }

    public void ClearTarget()
    {
        if (_actorCore != null)
        {
            _actorCore.OnCameraShake -= HandleCameraShake;
            _actorCore.OnHitStop -= HandleHitStop;
            _actorCore.OnDash -= HandleDash;
        }
        _actorCore = null;
    }

    private void HandleCameraShake(float magnitude, float duration)
    {
        if (magnitude >= _shakeMagnitude || _shakeTimer <= 0)
        {
            _shakeMagnitude = magnitude;
            _shakeDuration = duration;
            _shakeTimer = duration;
        }
    }

    private void HandleHitStop(float duration)
    {
        _hitStopTimer = duration;
    }

    private void HandleDash(float dragFactor, float duration)
    {
        _dashDragFactor = dragFactor;
        _dashTimer = duration;
    }
}
