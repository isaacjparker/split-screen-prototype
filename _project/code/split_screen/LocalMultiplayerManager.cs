using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Numerics;

public partial class LocalMultiplayerManager : Node
{
    // Inspector
    [Export] private PackedScene _playerScene = default;
    [Export] private Node3D[] _spawnPoints = Array.Empty<Node3D>();

    // Config
    private const int MaximumPlayerCount = 4;
    private const int KeyboardPlayerSlot = 0;

    // Runtime
    private readonly HashSet<int> _connectedDeviceIds = new();
    private readonly Dictionary<int, int> _deviceIdToPlayerSlot = new();
    private readonly Dictionary<int, PlayerBrain> _playerSlotToInstance = new Dictionary<int, PlayerBrain>();
    private Node _playersRootNode = default;

    // Actions
    public event Action<PlayerBrain, int> PlayerJoinedEvent;
    public event Action<int> PlayerLeftEvent;

    public override void _Ready()
    {

        // Track devices that were already connected before scene load
        foreach (int deviceId in Input.GetConnectedJoypads())
        {
            _connectedDeviceIds.Add(deviceId);
        }

		// Listen for controllers being connected/disconnected.
        Input.Singleton.Connect("joy_connection_changed", Callable.From<int, bool>(OnJoyConnectionChanged));

        CallDeferred(nameof(InitializeAfterSceneReady));
    }

    private void InitializeAfterSceneReady()
    {
        EnsurePlayersRootNodeExists();

        // Always spawn Player 0 (keyboard + device 0 action set).
        SpawnKeyboardPlayer();
    }

    public override void _PhysicsProcess(double delta)
    {
        HandleJoinLeaveInput();
    }

    private void EnsurePlayersRootNodeExists()
    {
        Node existingPlayersNode = GetTree().CurrentScene.GetNodeOrNull<Node>("Players");

        if (existingPlayersNode != null)
        {
            _playersRootNode = existingPlayersNode;
            return;
        }

        Node newPlayersNode = new Node();
        GetTree().CurrentScene.AddChild(newPlayersNode);
        newPlayersNode.Name = "Players";
        _playersRootNode = newPlayersNode;
    }

    private void HandleJoinLeaveInput()
    {
        // We only support 4 device-id action sets (0..3)
        // Start from 1 as device id 0 is auto joined with keyboard
        for (int deviceId = 1; deviceId < MaximumPlayerCount; deviceId++)
        {
			// Don't check input for devices that aren't plugged in.
            if (!_connectedDeviceIds.Contains(deviceId))
            {
                continue;
            }

            string startActionName = $"start_{deviceId}";

			// Skip if start wasn't pressed this frame.
            if (!Input.IsActionJustPressed(startActionName))
            {
                continue;
            }

			// If the device already has a slot, Start means Leave.
			// If it doesn't, Start means Join.
            if (_deviceIdToPlayerSlot.ContainsKey(deviceId))
            {
                LeavePlayer(deviceId);
            }
            else
            {
                JoinPlayer(deviceId);
            }
        }
    }

    private void OnJoyConnectionChanged(int deviceId, bool isConnected)
    {
        if (isConnected)
        {
            _connectedDeviceIds.Add(deviceId);
            return;
        }

        _connectedDeviceIds.Remove(deviceId);

        // If a joined device disconnects, treat it as leaving.
        if (_deviceIdToPlayerSlot.ContainsKey(deviceId))
        {
            LeavePlayer(deviceId);
        }
    }

    private void SpawnKeyboardPlayer()
    {
        if (_playerSlotToInstance.ContainsKey(KeyboardPlayerSlot))
        {
            return;
        }

        PlayerBrain playerInstance = CreatePlayerInstance(KeyboardPlayerSlot);

        playerInstance.PlayerSlot = KeyboardPlayerSlot;
        playerInstance.AssignInputDevice(0);

        _playerSlotToInstance[KeyboardPlayerSlot] = playerInstance;

        PlayerJoinedEvent?.Invoke(playerInstance, KeyboardPlayerSlot);
    }

    private void JoinPlayer(int deviceId)
    {
        int? availablePlayerSlot = FindLowestAvailablePlayerSlot();

        if (availablePlayerSlot == null)
        {
            GD.Print($"No free player slots available. Device {deviceId} cannot join.");
            return;
        }

        int playerSlot = availablePlayerSlot.Value;

        PlayerBrain playerInstance = CreatePlayerInstance(playerSlot);

        playerInstance.PlayerSlot = playerSlot;
        playerInstance.AssignInputDevice(deviceId);

        _playerSlotToInstance[playerSlot] = playerInstance;
        _deviceIdToPlayerSlot[deviceId] = playerSlot;

        GD.Print($"Device {deviceId} joined as PlayerSlot {playerSlot}.");

        PlayerJoinedEvent?.Invoke(playerInstance, playerSlot);
    }

    private void LeavePlayer(int deviceId)
    {
        if (!_deviceIdToPlayerSlot.TryGetValue(deviceId, out int playerSlot))
        {
            return;
        }

        // Notify first. Tell everyone the player is leaving.
        // Camera manager will RebuildCameraLayout() immediately when it receives this signal.
        GD.Print($"Device {deviceId} left PlayerSlot {playerSlot}.");
        PlayerLeftEvent?.Invoke(playerSlot);

        // Then remove the data from the manager
        _deviceIdToPlayerSlot.Remove(deviceId);

        if (_playerSlotToInstance.TryGetValue(playerSlot, out PlayerBrain playerInstance))
        {
            // Now it is safe to remove the physical node from the scene
            if (playerInstance != null && IsInstanceValid(playerInstance))
            {
                playerInstance.QueueFree();
            }

            _playerSlotToInstance.Remove(playerSlot);
        }
    }

    private int? FindLowestAvailablePlayerSlot()
    {
        // Slot 0 is reserved for keyboard in this prototype.
        for (int playerSlot = 1; playerSlot < MaximumPlayerCount; playerSlot++)
        {
            if (!_playerSlotToInstance.ContainsKey(playerSlot))
            {
                return playerSlot;
            }
        }

        return null;
    }

    private PlayerBrain CreatePlayerInstance(int playerSlot)
    {
        Node instance = _playerScene.Instantiate();

		if (instance is not PlayerBrain playerController)
        {
            instance.QueueFree();
            throw new InvalidOperationException("Player scene root must be a PlayerController.");
        }

        // Place player at spawn point before adding to tree.
        if (playerSlot >= 0 && playerSlot < _spawnPoints.Length && _spawnPoints[playerSlot] != null)
        {
            Node3D spawnPoint = _spawnPoints[playerSlot];
            playerController.GlobalTransform = spawnPoint.GlobalTransform;
        }
        else
        { 
            playerController.GlobalTransform = Transform3D.Identity;
            GD.Print($"Missing spawn point for PlayerSlot {playerSlot}. Spawning at origin");
        }

        _playersRootNode.AddChild(playerController);
        GD.Print("Player Instance Created.");
        return playerController;
    }
}
