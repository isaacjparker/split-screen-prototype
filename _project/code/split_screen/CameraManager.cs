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

    /*
        private void RebuildCameraLayout()
        {
            int activePlayerCount = _playerSlotToInstance.Count;

            if (activePlayerCount <= 0)
            {
                SetVisibleRig(playerCount: 0);
                return;
            }

            // 1 Player case
            if (activePlayerCount == 1)
            {
                SetVisibleRig(activePlayerCount);

                int remainingPlayerSlot = GetSortedPlayerSlots()[0];
                PlayerController remainingPlayer = _playerSlotToInstance[remainingPlayerSlot];

                _playerSlotToCameraSlot.Clear();
                _playerSlotToCameraSlot[remainingPlayerSlot] = 0;

                _1PRig.InitializeCamera(camArrayIndex: 0, remainingPlayer, remainingPlayerSlot);

                return;
            }

            // 2 Player case
            SetVisibleRig(playerCount: 2);

            List<int> sortedSlots = GetSortedPlayerSlots();
            int firstPlayerSlot = sortedSlots[0];
            int secondPlayerSlot = sortedSlots[1];

            PlayerController firstPlayer = _playerSlotToInstance[firstPlayerSlot];
            PlayerController secondPlayer = _playerSlotToInstance[secondPlayerSlot];

            _playerSlotToCameraSlot.Clear();
            _playerSlotToCameraSlot[firstPlayerSlot] = 0;
            _playerSlotToCameraSlot[secondPlayerSlot] = 1;

            _2PRig.InitializeCamera(camArrayIndex: 0, firstPlayer, firstPlayerSlot);
            _2PRig.InitializeCamera(camArrayIndex: 1, secondPlayer, secondPlayerSlot);

        }

        private List<int> GetSortedPlayerSlots()
        {
            List<int> slots = new List<int>(_playerSlotToInstance.Keys);
            slots.Sort(); // simple, deterministic ordering for the prototype
            return slots;
        }

        private void SetVisibleRig(int playerCount)
        {
            if (_1PRig != null) _1PRig.Visible = playerCount == 1;
            if (_2PRig != null) _2PRig.Visible = playerCount == 2;

            // Not used yet
            if (_3PRig != null) _3PRig.Visible = false;
            if (_4PRig != null) _4PRig.Visible = false;
        }
    */
}
