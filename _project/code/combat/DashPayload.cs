using Godot;


public struct DashPayload
{
	public readonly Vector3 TargetPosition;
    public readonly float Duration;
    public readonly float StopOffset;
    public readonly bool IsWhiff; // Helpful for animation logic if needed
	public Vector3 DashVelocity;
	public float DashFriction;

    public DashPayload(Vector3 target, float duration, float offset, bool isWhiff)
    {
        TargetPosition = target;
        Duration = duration;
        StopOffset = offset;
        IsWhiff = isWhiff;
    }
}
