using Godot;
using System;
using System.Collections.Generic;

public partial class PlayerMetricsManager : Node
{
    [Export] public ActorRuntimeSet PlayerRuntimeSet;

    private readonly Dictionary<ActorCore, PlayerMetricsRecord> _records = new Dictionary<ActorCore, PlayerMetricsRecord>();

    public event Action<ActorCore, int> OnKillCountChanged;
    public event Action<ActorCore, int> OnDeathCountChanged;
    public event Action<ActorCore> OnLowHealthEntered;
    public event Action<ActorCore> OnLowHealthExited;

    public override void _Ready()
    {
        if (PlayerRuntimeSet == null)
        {
            GD.PrintErr("PlayerMetricsManager: PlayerRuntimeSet not assigned.");
            return;
        }

        PlayerRuntimeSet.ItemAdded += HandlePlayerAdded;
        PlayerRuntimeSet.ItemRemoved += HandlePlayerRemoved;

        foreach (ActorCore existingPlayer in PlayerRuntimeSet.GetAll())
        {
            HandlePlayerAdded(existingPlayer);
        }
    }

    public override void _ExitTree()
    {
        if (PlayerRuntimeSet == null) return;

        PlayerRuntimeSet.ItemAdded -= HandlePlayerAdded;
        PlayerRuntimeSet.ItemRemoved -= HandlePlayerRemoved;

        foreach (ActorCore player in _records.Keys)
        {
            UnsubscribeFromPlayer(player);
        }
    }

    public IEnumerable<ActorCore> GetTrackedPlayers()
    {
        return _records.Keys;
    }

    public PlayerMetricsRecord GetRecord(ActorCore player)
    {
        _records.TryGetValue(player, out PlayerMetricsRecord record);
        return record;
    }

    public void DebugSetKillCount(ActorCore player, int value)
    {
        PlayerMetricsRecord record = GetRecord(player);
        if (record == null) return;

        record.KillCount = value;
        OnKillCountChanged?.Invoke(player, record.KillCount);
    }

    public void DebugSetDeathCount(ActorCore player, int value)
    {
        PlayerMetricsRecord record = GetRecord(player);
        if (record == null) return;

        record.DeathCount = value;
        OnDeathCountChanged?.Invoke(player, record.DeathCount);
    }

    private void HandlePlayerAdded(ActorCore player)
    {
        if (player == null) return;

        if (_records.ContainsKey(player)) return;

        _records[player] = new PlayerMetricsRecord();
        SubscribeToPlayer(player);
    }

    private void HandlePlayerRemoved(ActorCore player)
    {
        if (player == null) return;

        if (!_records.ContainsKey(player)) return;

        UnsubscribeFromPlayer(player);
        // Retain record in case of respawn / re-add
    }

    private void SubscribeToPlayer(ActorCore player)
    {
        player.OnKill += HandleActorKill;
        player.OnDeath += HandleActorDeath;
        player.OnLowHealthEntered += HandleActorLowHealthEntered;
        player.OnLowHealthExited += HandleActorLowHealthExited;
    }

    private void UnsubscribeFromPlayer(ActorCore player)
    {
        player.OnKill -= HandleActorKill;
        player.OnDeath -= HandleActorDeath;
        player.OnLowHealthEntered -= HandleActorLowHealthEntered;
        player.OnLowHealthExited -= HandleActorLowHealthExited;
    }

    private void HandleActorKill(ActorCore killer, ActorCore victim)
    {
        if (!_records.TryGetValue(killer, out PlayerMetricsRecord record)) return;

        record.KillCount += 1;
        OnKillCountChanged?.Invoke(killer, record.KillCount);
    }

    private void HandleActorDeath(ActorCore player)
    {
        if (!_records.TryGetValue(player, out PlayerMetricsRecord record)) return;

        record.DeathCount += 1;
        OnDeathCountChanged?.Invoke(player, record.DeathCount);
    }

    private void HandleActorLowHealthEntered(ActorCore player)
    {
        if (!_records.TryGetValue(player, out PlayerMetricsRecord record)) return;

        record.IsLowHealth = true;
        OnLowHealthEntered?.Invoke(player);
    }

    private void HandleActorLowHealthExited(ActorCore player)
    {
        if (!_records.TryGetValue(player, out PlayerMetricsRecord record)) return;

        record.IsLowHealth = false;
        OnLowHealthExited?.Invoke(player);
    }
}
