using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

public partial class DebugOverlayManager : CanvasLayer
{
    [Export] public PlayerMetricsManager PlayerMetricsManager;
    [Export] public Control MetricsPanel;
    [Export] public Label MetricsLabel;
    [Export] public Control ConsolePanel;
    [Export] public RichTextLabel ConsoleOutput;
    [Export] public LineEdit ConsoleInput;
    [Export] public string ToggleActionName = "toggle_debug_overlay";

    private readonly StringBuilder _stringBuilder = new StringBuilder();
    private readonly List<string> _commandHistory = new List<string>();
    private int _historyIndex = -1;

    public override void _Ready()
    {
        if (PlayerMetricsManager == null) GD.PrintErr("DebugOverlay: PlayerMetricsManager not assigned.");
        if (MetricsPanel == null) GD.PrintErr("DebugOverlay: MetricsPanel not assigned.");
        if (MetricsLabel == null) GD.PrintErr("DebugOverlay: MetricsLabel not assigned.");
        if (ConsolePanel == null) GD.PrintErr("DebugOverlay: ConsolePanel not assigned.");
        if (ConsoleOutput == null) GD.PrintErr("DebugOverlay: ConsoleOutput not assigned.");
        if (ConsoleInput == null) GD.PrintErr("DebugOverlay: ConsoleInput not assigned.");

        MetricsPanel.Visible = false;
        ConsolePanel.Visible = false;

        ConsoleInput.TextSubmitted += HandleConsoleInputSubmitted;
        ConsoleInput.GuiInput += HandleConsoleInputGuiInput;

        PrintToConsole("Type 'help' for commands.");
    }

    public override void _ExitTree()
    {
        if (ConsoleInput == null) return;
        ConsoleInput.TextSubmitted -= HandleConsoleInputSubmitted;
        ConsoleInput.GuiInput -= HandleConsoleInputGuiInput;
    }

    public override void _Input(InputEvent inputEvent)
    {
        if (!inputEvent.IsActionPressed(ToggleActionName)) return;

        AdvanceCycle();
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (!MetricsPanel.Visible) return;
        RefreshMetricsLabel();
    }

    private void AdvanceCycle()
    {
        bool metricsVisible = MetricsPanel.Visible;
        bool consoleVisible = ConsolePanel.Visible;

        if (!metricsVisible && !consoleVisible)
        {
            MetricsPanel.Visible = true;
            return;
        }

        if (metricsVisible && !consoleVisible)
        {
            ConsolePanel.Visible = true;
            ConsoleInput.GrabFocus();
            ConsoleInput.Clear();
            return;
        }

        MetricsPanel.Visible = false;
        ConsolePanel.Visible = false;
        ConsoleInput.ReleaseFocus();
    }

    private void RefreshMetricsLabel()
    {
        if (PlayerMetricsManager == null) return;
        if (MetricsLabel == null) return;

        _stringBuilder.Clear();
        _stringBuilder.AppendLine("=== Player Metrics ===");

        int playerIndex = 1;
        foreach (ActorCore player in PlayerMetricsManager.GetTrackedPlayers())
        {
            PlayerMetricsRecord record = PlayerMetricsManager.GetRecord(player);
            if (record == null) continue;

            _stringBuilder.Append("p").Append(playerIndex).Append(" (").Append(player.Name).AppendLine(")");
            _stringBuilder.Append("  Kills: ").AppendLine(record.KillCount.ToString());
            _stringBuilder.Append("  Deaths: ").AppendLine(record.DeathCount.ToString());
            _stringBuilder.Append("  Villagers saved: ").AppendLine(record.VillagersSaved.ToString());
            if (player.Progression != null)
                _stringBuilder.Append("  Attack Lv: ").AppendLine(player.Progression.CurrentLevel.ToString());
            _stringBuilder.Append("  Low HP: ").AppendLine(record.IsLowHealth.ToString());

            playerIndex++;
        }

        MetricsLabel.Text = _stringBuilder.ToString();
    }

    private void HandleConsoleInputSubmitted(string text)
    {
        string trimmed = text.Trim();
        ConsoleInput.Clear();

        if (string.IsNullOrEmpty(trimmed)) return;

        _commandHistory.Add(trimmed);
        _historyIndex = _commandHistory.Count;

        PrintToConsole("> " + trimmed);
        ProcessCommand(trimmed.ToLower());
    }

    private void HandleConsoleInputGuiInput(InputEvent inputEvent)
    {
        if (inputEvent is not InputEventKey keyEvent) return;
        if (!keyEvent.Pressed) return;

        if (keyEvent.Keycode == Key.Up)
        {
            NavigateHistory(-1);
            ConsoleInput.AcceptEvent();
            return;
        }

        if (keyEvent.Keycode == Key.Down)
        {
            NavigateHistory(1);
            ConsoleInput.AcceptEvent();
        }
    }

    private void NavigateHistory(int direction)
    {
        if (_commandHistory.Count == 0) return;

        _historyIndex = Mathf.Clamp(_historyIndex + direction, 0, _commandHistory.Count);

        if (_historyIndex >= _commandHistory.Count)
        {
            ConsoleInput.Text = "";
            ConsoleInput.CaretColumn = 0;
            return;
        }

        ConsoleInput.Text = _commandHistory[_historyIndex];
        ConsoleInput.CaretColumn = ConsoleInput.Text.Length;
    }

    private enum MetricKind { Kills, Deaths }

    private void ApplyIntegerMetric(string commandName, string valueToken, List<ActorCore> targets, MetricKind kind)
    {
        if (!int.TryParse(valueToken, out int parsedValue))
        {
            PrintError("Value must be an integer: " + valueToken);
            return;
        }

        foreach (ActorCore player in targets)
        {
            ApplyIntegerMetricToPlayer(commandName, parsedValue, player, kind);
        }
    }

    private void ProcessCommand(string commandLine)
    {
        string[] tokens = commandLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length == 0) return;

        string commandName = tokens[0];

        if (commandName == "help")
        {
            PrintHelp();
            return;
        }

        if (commandName == "set" || commandName == "add")
        {
            HandleMetricCommand(commandName, tokens);
            return;
        }

        PrintError("Unknown command: " + commandName);
    }

    private void PrintHelp()
    {
        PrintToConsole("Available commands:");
        PrintToConsole("  help");
        PrintToConsole("  set <metric> <player> <value>");
        PrintToConsole("  add <metric> <player> <value>");
        PrintToConsole("Metrics: kills, deaths");
        PrintToConsole("Player: p1, p2, p3, p4, or all");
    }

    private void HandleMetricCommand(string commandName, string[] tokens)
    {
        if (tokens.Length != 4)
        {
            PrintError("Usage: " + commandName + " <metric> <player> <value>");
            return;
        }

        string metricName = tokens[1];
        string playerToken = tokens[2];
        string valueToken = tokens[3];

        List<ActorCore> targets = ResolvePlayerTargets(playerToken);
        if (targets == null) return;
        if (targets.Count == 0)
        {
            PrintError("No players matched: " + playerToken);
            return;
        }

        if (metricName == "kills")
        {
            ApplyIntegerMetric(commandName, valueToken, targets, MetricKind.Kills);
            return;
        }

        if (metricName == "deaths")
        {
            ApplyIntegerMetric(commandName, valueToken, targets, MetricKind.Deaths);
            return;
        }

        PrintError("Unknown metric: " + metricName);
    }

    private void ApplyIntegerMetricToPlayer(string commandName, int value, ActorCore player, MetricKind kind)
    {
        PlayerMetricsRecord record = PlayerMetricsManager.GetRecord(player);
        if (record == null) return;

        if (kind == MetricKind.Kills && commandName == "set")
        {
            PlayerMetricsManager.DebugSetKillCount(player, value);
            return;
        }

        if (kind == MetricKind.Kills && commandName == "add")
        {
            PlayerMetricsManager.DebugSetKillCount(player, record.KillCount + value);
            return;
        }

        if (kind == MetricKind.Deaths && commandName == "set")
        {
            PlayerMetricsManager.DebugSetDeathCount(player, value);
            return;
        }

        if (kind == MetricKind.Deaths && commandName == "add")
        {
            PlayerMetricsManager.DebugSetDeathCount(player, record.DeathCount + value);
        }
    }

    private List<ActorCore> ResolvePlayerTargets(string playerToken)
    {
        List<ActorCore> trackedPlayers = new List<ActorCore>();
        foreach (ActorCore player in PlayerMetricsManager.GetTrackedPlayers())
        {
            trackedPlayers.Add(player);
        }

        if (playerToken == "all")
        {
            return trackedPlayers;
        }

        if (!playerToken.StartsWith("p"))
        {
            PrintError("Player must be p1-p4 or 'all'.");
            return null;
        }

        string indexPart = playerToken.Substring(1);
        if (!int.TryParse(indexPart, out int playerIndex))
        {
            PrintError("Could not parse player index: " + playerToken);
            return null;
        }

        if (playerIndex < 1 || playerIndex > trackedPlayers.Count)
        {
            PrintError("Player index out of range: " + playerIndex);
            return null;
        }

        List<ActorCore> result = new List<ActorCore>();
        result.Add(trackedPlayers[playerIndex - 1]);
        return result;
    }

    private void PrintToConsole(string text)
    {
        if (ConsoleOutput == null) return;
        ConsoleOutput.AppendText(text + "\n");
    }

    private void PrintError(string text)
    {
        if (ConsoleOutput == null) return;
        ConsoleOutput.AppendText("[color=red]" + text + "[/color]\n");
    }
}
