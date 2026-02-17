using Godot;
using System;

public partial class CameraController : Camera3D
{
    // Config
    [Export] private Vector3 _camOffset;
    [Export] private Vector3 _lookAtOffset;
    [Export] private float _smoothingAccel = 5f;
    [Export] private float _catchUpAccel = 12f;

    // Runtime
    private int _playerSlot;
    private Node3D _targetNode;
    private Vector3 _lookAtTarget;
    private bool _isFollowingTarget = true;
    private bool _pauseOccured;

    public void InitializeCamera(Node3D targetNode, int playerSlot)
    {
        if (targetNode == null)
        {
            GD.PushWarning("targetNode is null, failed to initialize camera.");
            return;
        }
    
    	_targetNode = targetNode;
        _playerSlot = playerSlot;

        GlobalPosition = _targetNode.GlobalPosition + _camOffset;
        _lookAtTarget = _targetNode.GlobalPosition + _lookAtOffset;
        LookAt(_lookAtTarget, Vector3.Up);
    }

    public override void _Process(double delta)
    { 
		// IsInstanceValid checks both for null AND if the object was freed/disposed
        if (!GodotObject.IsInstanceValid(_targetNode))
        {
            _targetNode = null; // Clean up the reference
            return;
        }

        _lookAtTarget = _targetNode.GlobalPosition + _lookAtOffset;
        Vector3 desiredPosition = _lookAtTarget + _camOffset;

        if (!_isFollowingTarget)
        {
            _pauseOccured = true;
            return;
        }

        float accel = _pauseOccured ? _catchUpAccel : _smoothingAccel;
        float time = Mathf.Clamp(accel * (float)delta, 0f, 1f);

        GlobalPosition = GlobalPosition.Lerp(desiredPosition, time);

        if (_pauseOccured && GlobalPosition.DistanceTo(desiredPosition) <= 0.01f)
        {
            _pauseOccured = false;
        }

    }

	// Called by PlayerController during dashes etc.
    public void SetCamFollow(bool value)
    {
        _isFollowingTarget = value;
    }

    public void ClearTarget()
    {
        _targetNode = null;
    }
}
