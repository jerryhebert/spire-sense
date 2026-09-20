using Godot;
using SpireSense.SpireSenseCode.Game;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>
/// The on-screen panel. Lives directly under the scene-tree root so it survives every scene change,
/// polls the local deck a few times a second, and only rebuilds its text when the counts change.
/// </summary>
public partial class SpireSenseOverlay : CanvasLayer
{
    private const double PollIntervalSeconds = 0.25;

    private static SpireSenseOverlay? _instance;

    private OverlaySettings _settings = new();
    private PanelContainer _panel = null!;
    private RichTextLabel _label = null!;
    private Button _hotkeyButton = null!;
    private bool _capturingHotkey;
    private double _pollAccumulator;
    private DeckAnalysis? _lastAnalysis;
    private bool _dragging;
    private bool _positionDirty;
    private string? _lastErrorMessage;

    /// <summary>Adds the overlay to the scene tree. Safe to call from mod initialization.</summary>
    public static void Install()
    {
        if (_instance != null)
        {
            return;
        }

        if (Engine.GetMainLoop() is not SceneTree tree)
        {
            ModLog.Error("No SceneTree available; overlay not installed.");
            return;
        }

        // Mod initialization does not reliably run on the main thread, and constructing a Node off
        // the main thread can crash the engine. Deferring puts both the construction and the tree
        // insertion on the main thread during idle.
        Callable.From(() =>
        {
            try
            {
                _instance = new SpireSenseOverlay();
                tree.Root.AddChild(_instance);
            }
            catch (Exception ex)
            {
                ModLog.Error($"Overlay could not be added to the scene tree: {ex}");
            }
        }).CallDeferred();
    }

    public override void _Ready()
    {
        Name = "SpireSenseOverlay";
        Layer = 100;
        ProcessMode = ProcessModeEnum.Always;
        _settings = OverlaySettings.Current;

        _panel = new PanelContainer
        {
            Name = "Panel",
            MouseFilter = Control.MouseFilterEnum.Stop,
            Position = new Vector2(_settings.X, _settings.Y),
        };
        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f, 0.82f),
            BorderColor = new Color(0.85f, 0.75f, 0.45f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 12,
            ContentMarginRight = 12,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        _panel.AddThemeStyleboxOverride("panel", style);
        _panel.GuiInput += OnPanelGuiInput;

        _label = new RichTextLabel
        {
            Name = "Text",
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.Off,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(280, 0),
        };
        _label.AddThemeFontSizeOverride("normal_font_size", _settings.FontSize);
        _label.AddThemeFontSizeOverride("bold_font_size", _settings.FontSize);

        _hotkeyButton = new Button
        {
            Name = "Hotkey",
            FocusMode = Control.FocusModeEnum.None,
            MouseFilter = Control.MouseFilterEnum.Stop,
        };
        _hotkeyButton.AddThemeFontSizeOverride("font_size", Math.Max(12, _settings.FontSize - 5));
        _hotkeyButton.Pressed += BeginHotkeyCapture;
        UpdateHotkeyButton();

        var column = new VBoxContainer { Name = "Column", MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 6);
        column.AddChild(_label);
        column.AddChild(_hotkeyButton);
        column.AddChild(ScaleControls.Build(_settings, Math.Max(12, _settings.FontSize - 5), ApplyScale));
        _panel.AddChild(column);
        AddChild(_panel);
        ApplyScale();

        Visible = _settings.Visible;
        _panel.Visible = false; // Stays hidden until a run is in progress.
        ModLog.Info($"Overlay installed (visible={_settings.Visible}, toggle={_settings.ParsedToggleKey}).");
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is not InputEventKey key || !key.Pressed || key.Echo)
        {
            return;
        }

        if (_capturingHotkey)
        {
            CompleteHotkeyCapture(key.Keycode);
            GetViewport().SetInputAsHandled();
            return;
        }

        if (key.Keycode == _settings.ParsedToggleKey)
        {
            Visible = !Visible;
            _settings.Visible = Visible;
            _settings.Save();
            GetViewport().SetInputAsHandled();
        }
    }

    private void BeginHotkeyCapture()
    {
        _capturingHotkey = true;
        _hotkeyButton.Text = "Press any key…  (Esc cancels)";
    }

    private void CompleteHotkeyCapture(Key keycode)
    {
        _capturingHotkey = false;

        // Escape means cancel, so it cannot itself be bound. Everything else is fair game.
        if (keycode != Key.Escape)
        {
            _settings.ToggleKey = keycode.ToString();
            _settings.Save();
            ModLog.Info($"Overlay hotkey rebound to {_settings.ToggleKeyLabel}");
        }

        UpdateHotkeyButton();
    }

    private void UpdateHotkeyButton()
    {
        _hotkeyButton.Text = $"Hide with: {_settings.ToggleKeyLabel}  (click to change)";
    }

    private void ApplyScale()
    {
        // Scaling the panel scales its text, padding and borders together. PivotOffset stays at the
        // top-left so the panel grows away from the corner it is positioned by.
        _panel.Scale = Vector2.One * _settings.ClampedScale;
    }

    public override void _Process(double delta)
    {
        _pollAccumulator += delta;
        if (_pollAccumulator < PollIntervalSeconds)
        {
            return;
        }
        _pollAccumulator = 0;

        if (_positionDirty && !_dragging)
        {
            _positionDirty = false;
            _settings.X = _panel.Position.X;
            _settings.Y = _panel.Position.Y;
            _settings.Save();
        }

        try
        {
            var deck = RunAccess.LocalDeck;
            if (deck == null)
            {
                if (_panel.Visible)
                {
                    _panel.Visible = false;
                    _lastAnalysis = null;
                }
                return;
            }

            var analysis = DeckAnalysis.Analyze(deck.Select(CardFactsReader.Read));
            if (_lastAnalysis == null || !analysis.Equals(_lastAnalysis))
            {
                _lastAnalysis = analysis;
                _label.Text = OverlayText.Build(analysis, _settings.ShowCardNames);
            }
            _panel.Visible = true;
        }
        catch (Exception ex)
        {
            // Never let an overlay bug interrupt the game loop. This runs four times a second, so
            // only log when the message changes; otherwise a recurring fault would flood the log.
            if (_lastErrorMessage != ex.Message)
            {
                _lastErrorMessage = ex.Message;
                ModLog.Warn($"Overlay update failed: {ex.Message}");
            }
        }
    }

    private void OnPanelGuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                _dragging = button.Pressed;
                if (!button.Pressed)
                {
                    _positionDirty = true;
                }
                _panel.AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging:
                _panel.Position += motion.Relative * _panel.Scale;
                _panel.AcceptEvent();
                break;
        }
    }
}
