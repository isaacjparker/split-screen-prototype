using Godot;
using System;
using System.Collections.Generic;

public enum CameraRigType
{ 
	OnePlayer,
	TwoPlayer,
	ThreePlayer,
	FourPlayer
}

public partial class CameraRig : GridContainer
{
    [Export] private CameraController[] CamArray = new CameraController[4];

    [Export] private CameraRigType _thisRigType;

	// Called From CameraManager
    public void InitializeCamera(int camArrayIndex, ActorCore actorCore, int playerSlot)
    {
        if (actorCore == null)
        {
            GD.PushWarning($"CameraRig.InitializeCamera: targetNode for Camera {camArrayIndex} is null.");
            return;
        }

		if (CamArray[camArrayIndex] == null)
        {
            GD.PushWarning($"CameraRig.InitializeCamera: Camera {camArrayIndex} reference missing.");
            return;
        }

		CamArray[camArrayIndex].InitializeCamera(actorCore, playerSlot);

    }

    public void DeactivateAllCameras()
    {
        foreach (CameraController controller in CamArray)
        {
            // Only call if the array slot isn't null and the node isn't disposed
            if (IsInstanceValid(controller))
            {
                controller.ClearTarget();
            }
        }
    }

    public void ClearCameraTarget(int camArrayIndex)
    {
        CamArray[camArrayIndex].ClearTarget();
    }
}
