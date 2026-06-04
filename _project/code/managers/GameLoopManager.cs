using Godot;

/// <summary>
/// Top-level run orchestrator. Lives in boot_load_managers so it exists in every gameplay
/// scene. Drives the day/night loop's bookkeeping:
///   - owns the authoritative NightNumber and pushes the difficulty curve into WaveDirector,
///   - counts nights and ends the run after NightsToSurvive (unless FiniteNights is off),
///   - tracks villagers saved (total + per player) for the score,
///   - renders a small always-on HUD (day/night banner + score) and an end screen.
///
/// Turn FiniteNights OFF to loop the cycle forever with no win screen — handy when testing
/// a single phase/system without the run forcing an end after N nights.
/// </summary>
public partial class GameLoopManager : Node
{
    [ExportGroup("Run Length")]
    /// <summary>When false the day/night cycle loops forever (no win screen). Testing aid.</summary>
    [Export] public bool FiniteNights = true;
    /// <summary>Survive this many nights to win (ignored when FiniteNights is false).</summary>
    [Export] public int NightsToSurvive = 3;

    [ExportGroup("End Screen")]
    /// <summary>
    /// Seconds to wait after the final night's last wave enemy is killed before the win
    /// screen appears. A breather so the screen doesn't slam up the instant the maze clears.
    /// </summary>
    [Export] public float WinSettleDelay = 1.5f;
    /// <summary>
    /// Seconds the win screen ignores input after it appears, so a player mashing attack
    /// through the final wave doesn't instantly dismiss it.
    /// </summary>
    [Export] public float EndScreenInputDelay = 1.5f;

    [ExportGroup("Difficulty Curve (pushed into WaveDirector)")]
    [Export] public int BaseEnemiesPerPulse = 2;
    [Export] public int EnemiesPerPulseGrowth = 1;
    [Export] public int PerPlayerPulseBonus = 1;
    [Export] public float StaggerWindow = 12f;
    [Export] public float EnemyHealthGrowthPerNight = 0.25f;

    [ExportGroup("References")]
    [Export] public PlayerMetricsManager PlayerMetrics;

    public static GameLoopManager Instance { get; private set; }

    public int NightNumber { get; private set; }
    public int TotalVillagersSaved { get; private set; }
    public bool RunOver { get; private set; }

    private HomeRoom _homeRoom;
    private Label _statusLabel;
    private Label _endLabel;

    // End-of-run sequencing: the winning morning arrives, then we wait for the last wave to
    // be cleared, settle for a beat, show the screen, then lock input briefly.
    private bool _winPending;
    private float _settleTimer;
    private float _inputLockTimer;

    public override void _Ready()
    {
        Instance = this;
        // Keep reacting (HUD + restart input) while the tree is paused on the end screen.
        ProcessMode = ProcessModeEnum.Always;

        PushDifficultyToWaveDirector();
        NightNumber = 0;
        WaveDirector.NightNumber = 0;

        BuildHud();

        if (DayNightCycleManager.Instance != null)
        {
            DayNightCycleManager.Instance.DayStarted += OnDayStarted;
            DayNightCycleManager.Instance.NightStarted += OnNightStarted;
        }

        // HomeRoom lives in the gameplay scene and may not be ready yet — hook it after the
        // first frame, once every node's _Ready has run.
        CallDeferred(nameof(HookHomeRoom));

        RefreshHud();
    }

    public override void _ExitTree()
    {
        if (DayNightCycleManager.Instance != null)
        {
            DayNightCycleManager.Instance.DayStarted -= OnDayStarted;
            DayNightCycleManager.Instance.NightStarted -= OnNightStarted;
        }

        if (_homeRoom != null)
            _homeRoom.OnVillagerBanked -= OnVillagerBanked;

        if (Instance == this) Instance = null;
    }

    private void PushDifficultyToWaveDirector()
    {
        WaveDirector.BaseEnemiesPerPulse = BaseEnemiesPerPulse;
        WaveDirector.EnemiesPerPulseGrowth = EnemiesPerPulseGrowth;
        WaveDirector.PerPlayerPulseBonus = PerPlayerPulseBonus;
        WaveDirector.StaggerWindow = StaggerWindow;
        WaveDirector.EnemyHealthGrowthPerNight = EnemyHealthGrowthPerNight;
    }

    private void HookHomeRoom()
    {
        _homeRoom = HomeRoom.Instance;
        if (_homeRoom != null)
            _homeRoom.OnVillagerBanked += OnVillagerBanked;
    }

    private void OnVillagerBanked(ActorCore villager, ActorCore rescuer)
    {
        // The threshold-area path fires with a null rescuer (and can repeat); the real,
        // deduped banking comes from BankVillager with the rescuing player attached.
        if (rescuer == null) return;

        TotalVillagersSaved++;
        PlayerMetrics?.RegisterVillagerSaved(rescuer);
        RefreshHud();
    }

    private void OnNightStarted()
    {
        NightNumber++;
        WaveDirector.NightNumber = NightNumber;
        RefreshHud();
    }

    private void OnDayStarted()
    {
        // A morning arrives after each survived night. Once we've outlasted the target the
        // run is won — but don't slam up the screen yet: the night's wave enemies that were
        // still marching when dawn broke stay alive and must be cleared first (handled in
        // _Process). Day also stops new pulses, so the horde can only shrink from here.
        if (FiniteNights && !RunOver && !_winPending && NightNumber >= NightsToSurvive)
        {
            _winPending = true;
            _settleTimer = WinSettleDelay;
        }

        RefreshHud();
    }

    public override void _Process(double delta)
    {
        if (RunOver)
        {
            // Screen is up; tick down the input lockout (runs while the tree is paused
            // because our ProcessMode is Always).
            if (_inputLockTimer > 0f) _inputLockTimer -= (float)delta;
            return;
        }

        if (!_winPending) return;

        // Hold the win until every wave enemy from the final night is dead.
        if (CountLivingWaveEnemies() > 0)
        {
            _settleTimer = WinSettleDelay;
            if (_statusLabel != null)
                _statusLabel.Text = "DAWN — clear the last of the horde!";
            return;
        }

        // Maze is clear: settle for a beat, then reveal the screen.
        _settleTimer -= (float)delta;
        if (_settleTimer <= 0f) EndRun();
    }

    private void EndRun()
    {
        RunOver = true;
        _winPending = false;
        _inputLockTimer = EndScreenInputDelay;

        if (_endLabel != null)
        {
            _endLabel.Text =
                $"YOU SURVIVED {NightsToSurvive} NIGHTS!\n" +
                $"Villagers saved: {TotalVillagersSaved}\n\n" +
                "Press any button to play again";
            _endLabel.Visible = true;
        }

        GetTree().Paused = true;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!RunOver) return;
        if (_inputLockTimer > 0f) return; // ignore the mash that was clearing the final wave

        bool pressed = (@event is InputEventKey k && k.Pressed) ||
                       (@event is InputEventJoypadButton j && j.Pressed);
        if (!pressed) return;

        GetTree().Paused = false;
        GetTree().ReloadCurrentScene();
    }

    /// <summary>
    /// Living wave enemies across the maze. Wave enemies carry AgentSM.WaveMode; guards (which
    /// respawn each morning) don't, so a fresh morning's guards never block the win.
    /// </summary>
    private static int CountLivingWaveEnemies()
    {
        if (RuntimeSets.Instance == null || RuntimeSets.Instance.Actors == null) return 0;

        int count = 0;
        foreach (ActorCore a in RuntimeSets.Instance.Actors.GetAll())
        {
            if (!GodotObject.IsInstanceValid(a)) continue;
            if (a.Status == null || !a.Status.IsAlive) continue;
            if (a.StateMachine is AgentSM sm && sm.WaveMode) count++;
        }
        return count;
    }

    // --- HUD (built programmatically so it needs no scene wiring) ---
    private void BuildHud()
    {
        CanvasLayer hud = new CanvasLayer { Name = "GameLoopHud" };
        AddChild(hud);

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _statusLabel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        _statusLabel.OffsetTop = 8f;
        _statusLabel.AddThemeFontSizeOverride("font_size", 22);
        hud.AddChild(_statusLabel);

        _endLabel = new Label
        {
            Name = "EndLabel",
            Visible = false,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        _endLabel.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _endLabel.AddThemeFontSizeOverride("font_size", 40);
        hud.AddChild(_endLabel);
    }

    private void RefreshHud()
    {
        if (_statusLabel == null) return;

        bool isNight = DayNightCycleManager.Instance != null &&
                       DayNightCycleManager.Instance.CurrentPhase == DayNightCycleManager.Phase.Night;

        string phaseText = isNight
            ? (FiniteNights ? $"NIGHT {NightNumber} / {NightsToSurvive}" : $"NIGHT {NightNumber}")
            : $"DAY {NightNumber + 1}";

        if (!FiniteNights) phaseText += "  (endless)";

        _statusLabel.Text = $"{phaseText}     Villagers saved: {TotalVillagersSaved}";
    }
}
