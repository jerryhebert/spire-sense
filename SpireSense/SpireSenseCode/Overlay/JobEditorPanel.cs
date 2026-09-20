using Godot;
using MegaCrit.Sts2.Core.Models;
using SpireSense.SpireSenseCode.Game;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>
/// The reclassification panel shown beside a card on the inspect screen. One toggle per job, plus a
/// reset that removes your override and returns the card to the shipped classification.
/// </summary>
public partial class JobEditorPanel : PanelContainer
{
    private readonly Dictionary<Job, Button> _toggles = new();
    private Label _sourceLabel = null!;
    private Button _resetButton = null!;
    private string? _cardClassName;
    private bool _suppressCallbacks;
    private bool _dragging;
    private OverlaySettings _settings = null!;

    public override void _Ready()
    {
        Name = "SpireSenseJobEditor";
        _settings = OverlaySettings.Current;

        // Free-positioned rather than anchored: the card's own tooltips can sit on top of it, so it
        // has to be draggable, and a fixed anchor would fight the saved position.
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(320, 0);
        GuiInput += OnPanelGuiInput;

        var style = new StyleBoxFlat
        {
            BgColor = new Color(0.05f, 0.05f, 0.07f, 0.92f),
            BorderColor = new Color(0.85f, 0.75f, 0.45f, 0.9f),
            BorderWidthLeft = 2,
            BorderWidthTop = 2,
            BorderWidthRight = 2,
            BorderWidthBottom = 2,
            CornerRadiusTopLeft = 6,
            CornerRadiusTopRight = 6,
            CornerRadiusBottomLeft = 6,
            CornerRadiusBottomRight = 6,
            ContentMarginLeft = 14,
            ContentMarginRight = 14,
            ContentMarginTop = 12,
            ContentMarginBottom = 12,
        };
        AddThemeStyleboxOverride("panel", style);

        var column = new VBoxContainer { Name = "Column", MouseFilter = MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", 6);
        AddChild(column);

        var heading = new Label { Text = "Spire Sense — jobs  (drag to move)", MouseFilter = MouseFilterEnum.Ignore };
        heading.AddThemeFontSizeOverride("font_size", 20);
        heading.AddThemeColorOverride("font_color", new Color("e0c070"));
        column.AddChild(heading);

        foreach (var job in JobInfo.All)
        {
            var button = new Button
            {
                Text = JobInfo.DisplayName(job).Trim(),
                ToggleMode = true,
                Alignment = HorizontalAlignment.Left,
                FocusMode = FocusModeEnum.None,
            };
            button.AddThemeFontSizeOverride("font_size", 17);
            var captured = job;
            button.Toggled += pressed => OnJobToggled(captured, pressed);
            column.AddChild(button);
            _toggles[job] = button;
        }

        _sourceLabel = new Label { Text = "", MouseFilter = MouseFilterEnum.Ignore };
        _sourceLabel.AddThemeFontSizeOverride("font_size", 14);
        _sourceLabel.AddThemeColorOverride("font_color", new Color("9a9a9a"));
        _sourceLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        column.AddChild(_sourceLabel);

        _resetButton = new Button { Text = "Reset to default", FocusMode = FocusModeEnum.None };
        _resetButton.AddThemeFontSizeOverride("font_size", 15);
        _resetButton.Pressed += OnResetPressed;
        column.AddChild(_resetButton);

        column.AddChild(ScaleControls.Build(_settings, 14, ApplyScale));
        ApplyScale();
        RestorePosition();
    }

    private void ApplyScale()
    {
        Scale = Vector2.One * _settings.ClampedScale;
    }

    /// <summary>
    /// Uses the saved position, or parks the panel on the right of the screen the first time. The
    /// default is computed from the viewport rather than hard-coded so it lands sensibly at any
    /// window size.
    /// </summary>
    private void RestorePosition()
    {
        if (_settings.EditorX is { } x && _settings.EditorY is { } y)
        {
            Position = new Vector2(x, y);
            return;
        }

        var viewport = GetViewportRect().Size;
        Position = new Vector2(viewport.X * 0.66f, viewport.Y * 0.14f);
    }

    private void OnPanelGuiInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                _dragging = button.Pressed;
                if (!button.Pressed)
                {
                    _settings.EditorX = Position.X;
                    _settings.EditorY = Position.Y;
                    _settings.Save();
                }
                AcceptEvent();
                break;

            // Relative is in this control's own scaled space, so it has to be scaled back up or
            // the panel drifts behind the cursor at any scale other than 1.
            case InputEventMouseMotion motion when _dragging:
                Position += motion.Relative * Scale;
                AcceptEvent();
                break;
        }
    }

    /// <summary>Points the panel at a card. Pass null to hide it.</summary>
    public void ShowFor(CardModel? card)
    {
        if (card == null)
        {
            Visible = false;
            _cardClassName = null;
            return;
        }

        _cardClassName = card.GetType().Name;
        var result = CardClassifier.Classify(CardFactsReader.Read(card));

        if (result.Source == ClassificationSource.Ignored)
        {
            // Curses and statuses never count toward a job, so there is nothing to reclassify.
            Visible = false;
            return;
        }

        Visible = true;
        _suppressCallbacks = true;
        foreach (var (job, button) in _toggles)
        {
            button.ButtonPressed = result.Jobs.Contains(job);
        }
        _suppressCallbacks = false;

        _sourceLabel.Text = result.Source switch
        {
            ClassificationSource.Override => "Your classification.",
            ClassificationSource.Heuristic => "Guessed: this card is not in the tables.",
            _ => "Shipped classification.",
        };
        _resetButton.Disabled = result.Source != ClassificationSource.Override;
    }

    private void OnJobToggled(Job job, bool pressed)
    {
        if (_suppressCallbacks || _cardClassName == null)
        {
            return;
        }

        var jobs = new HashSet<Job>(_toggles.Where(t => t.Value.ButtonPressed).Select(t => t.Key));

        // AoE is a subset of frontloaded damage; keep the pair consistent in both directions.
        if (job == Job.FrontloadedAoe && pressed)
        {
            jobs.Add(Job.FrontloadedDamage);
        }
        else if (job == Job.FrontloadedDamage && !pressed)
        {
            jobs.Remove(Job.FrontloadedAoe);
        }

        JobOverrides.Set(_cardClassName, jobs);
        CardJobTip.InvalidateCache(_cardClassName);
        ModLog.Info($"Reclassified {_cardClassName} as [{string.Join(", ", jobs)}]");

        RefreshToggles(jobs);
        _sourceLabel.Text = "Your classification.";
        _resetButton.Disabled = false;
    }

    private void OnResetPressed()
    {
        if (_cardClassName == null)
        {
            return;
        }

        JobOverrides.Clear(_cardClassName);
        CardJobTip.InvalidateCache(_cardClassName);
        ModLog.Info($"Reset {_cardClassName} to the shipped classification");

        var jobs = JobDatabase.TryGet(_cardClassName, out var shipped) ? shipped : new HashSet<Job>();
        RefreshToggles(jobs);
        _sourceLabel.Text = JobDatabase.Has(_cardClassName)
            ? "Shipped classification."
            : "Guessed: this card is not in the tables.";
        _resetButton.Disabled = true;
    }

    private void RefreshToggles(IReadOnlySet<Job> jobs)
    {
        _suppressCallbacks = true;
        foreach (var (job, button) in _toggles)
        {
            button.ButtonPressed = jobs.Contains(job);
        }
        _suppressCallbacks = false;
    }
}
