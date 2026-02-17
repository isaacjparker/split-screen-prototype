using Godot;
using System;

[Tool]
public partial class CombatDebug : Node3D
{
    [Export] public Node3D WeaponPivot;

	[ExportGroup("Pose Preview")]
    [Export] public bool ShowIdle { get => false; set { if (value) SetPose("Idle"); } }
    [Export] public bool ShowStart { get => false; set { if (value) SetPose("Start"); } }
    [Export] public bool ShowMid { get => false; set { if (value) SetPose("Mid"); } }
    [Export] public bool ShowEnd { get => false; set { if (value) SetPose("End"); } }

    private void SetPose(string poseName)
    { 
		if (WeaponPivot == null)
        {
            GD.PrintErr("CombatDebug: Assign TargetModule and WeaponPivot first!");
            return;
        }

		// Logic to snap the pivot to the locations defined in TargetModule
        switch (poseName)
        {
            case "Idle":
                WeaponPivot.Position = new Vector3(0.6f, 0.8f, -0.2f);
                WeaponPivot.RotationDegrees = Vector3.Zero;
                break;
            case "Start":
                WeaponPivot.Position = new Vector3(0.6f, 1.582f, 0.409f);
                WeaponPivot.RotationDegrees = new Vector3(32.3f, 113.6f, 13.1f);
                break;
            case "End":
                WeaponPivot.Position = new Vector3(-0.727f, 0.556f, -0.667f);
                WeaponPivot.RotationDegrees = new Vector3(-20.3f, 140.0f, -12.5f);
                break;
            case "Mid":
                // Previewing the midpoint logic used in the Tween
                Vector3 start = new Vector3(0.6f, 1.582f, 0.409f);
                Vector3 end = new Vector3(-0.727f, 0.556f, -0.667f);
                Vector3 mid = (start + end) / 2.0f;
                // Note: In tool mode, the actor's basis might be identity (facing -Z)
                mid += Vector3.Forward * 1.5f; 
                WeaponPivot.Position = mid;
                // For rotation mid, we just lerp halfway
                WeaponPivot.RotationDegrees = (new Vector3(32.3f, 113.6f, 13.1f) + new Vector3(-20.3f, 140.0f, -12.5f)) / 2;
                break;
        }
        
        GD.Print($"CombatDebug: Snapped to {poseName} pose.");
	}
}
