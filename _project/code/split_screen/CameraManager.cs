using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class CameraManager : Node
{
    // Config
    [Export] private LocalMultiplayerManager _localMultiplayerManager;

    [Export] private CameraRig[] _cameraRigs = new CameraRig[4];

    // Runtime
    private Dictionary<int, ActorCore> _playerSlotToInstance = new();
    private Dictionary<int, int> _playerSlotToCameraSlot = new();

    public override void _Ready()
    {
        if (_localMultiplayerManager == null)
        {
            GD.PushWarning("LocalMultiplayerManager reference missing in CameraManager.");
            return;
        }

        _localMultiplayerManager.PlayerJoinedEvent += CreatePlayerCamera;
        _localMultiplayerManager.PlayerLeftEvent += RemovePlayerCamera;

        //SetVisibleRig(playerCount: 0);
        UpdateRigVisibility(0);
    }

    public override void _ExitTree()
    {
		if (_localMultiplayerManager == null)
            return;

        _localMultiplayerManager.PlayerJoinedEvent -= CreatePlayerCamera;
        _localMultiplayerManager.PlayerLeftEvent -= RemovePlayerCamera;
    }

    private void CreatePlayerCamera(ActorCore playerController, int playerSlot)
    {
        if (playerController == null)
        {
            GD.PushWarning("PlayerController is null for newly created player. Cannot Rebuild Camera Layout.");
            return;
        }

        _playerSlotToInstance[playerSlot] = playerController;

        RebuildCameraLayout();

    }

    private void RemovePlayerCamera(int playerSlot)
    { 
		_playerSlotToInstance.Remove(playerSlot);
        _playerSlotToCameraSlot.Remove(playerSlot);

        RebuildCameraLayout();
	}

    private void RebuildCameraLayout()
    {
        int count = _playerSlotToInstance.Count;
        UpdateRigVisibility(count);

        if (count == 0) return;

        // Get the specific rig for this player count (Index is count - 1)
        CameraRig activeRig = _cameraRigs[count - 1];
        if (!GodotObject.IsInstanceValid(activeRig)) return;

        // Clear camera slot mapping
        _playerSlotToCameraSlot.Clear();

        // Get sorted list of players to ensure consistent screen placement
        List<int> sortedSlots = _playerSlotToInstance.Keys.ToList();
        sortedSlots.Sort();

        for (int i = 0; i < sortedSlots.Count; i++)
        {
            int playerSlot = sortedSlots[i];
            ActorCore controller = _playerSlotToInstance[playerSlot];

            // CHECK: If the player is in the process of being removed / disposed, skip initialisation
            if (!GodotObject.IsInstanceValid(controller) || controller.IsQueuedForDeletion())
            {
                continue; 
            }

            // Update mapping
            _playerSlotToCameraSlot[playerSlot] = i;

            // Initalize the specific cameras in the active rig
            activeRig.InitializeCamera(i, controller, playerSlot);
        }
    }

    private void UpdateRigVisibility(int activeCount)
    {
        for (int i = 0; i < _cameraRigs.Length; i++)
        {
            if (_cameraRigs[i] == null) continue;

            bool shouldBeVisible = (i == activeCount - 1);

            // If the rig is being turned OFF, clear its cameras
            if (_cameraRigs[i].Visible && !shouldBeVisible)
            {
                _cameraRigs[i].DeactivateAllCameras();
            }

            // Rig index 0 is for 1 player, index 1 for 2 players, etc.
            _cameraRigs[i].Visible = shouldBeVisible;
            
        }
    }
}
