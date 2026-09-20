using Godot;
using SpireSense.SpireSenseCode.Jobs;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>
/// The diagonal hatching in a panel's bottom-right corner. Dragging it resizes the panel it is
/// given, which costs no layout space of its own because it is anchored over the content.
/// </summary>
public partial class ResizeGrip : Control
{
    private const float GripSize = 16f;

    private Control _target = null!;
    private OverlaySettings _settings = null!;
    private bool _dragging;

    /// <summary>Creates a grip that resizes <paramref name="target"/>.</summary>
    public static ResizeGrip For(Control target, OverlaySettings settings)
    {
        var grip = new ResizeGrip
        {
            Name = "ResizeGrip",
            _target = target,
            _settings = settings,
        };
        return grip;
    }

    public override void _Ready()
    {
        CustomMinimumSize = new Vector2(GripSize, GripSize);
        Size = new Vector2(GripSize, GripSize);

        // Pinned to the bottom-right corner of whatever it sits in.
        AnchorLeft = 1f;
        AnchorTop = 1f;
        AnchorRight = 1f;
        AnchorBottom = 1f;
        OffsetLeft = -GripSize;
        OffsetTop = -GripSize;
        OffsetRight = 0f;
        OffsetBottom = 0f;

        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.Fdiagsize;
        GuiInput += OnGripInput;
        UpdateTooltip();
    }

    private void UpdateTooltip() => TooltipText = $"Drag to resize  ({_settings.ScaleLabel})";

    public override void _Draw()
    {
        var colour = new Color(0.85f, 0.75f, 0.45f, 0.85f);
        var w = Size.X;
        var h = Size.Y;

        // Three steps of the conventional resize hatching.
        for (var i = 1; i <= 3; i++)
        {
            var inset = i * 4f;
            DrawLine(new Vector2(w - 1f, h - inset), new Vector2(w - inset, h - 1f), colour, 1.5f, antialiased: true);
        }
    }

    private void OnGripInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                _dragging = button.Pressed;
                if (!button.Pressed)
                {
                    _settings.Save();
                }
                AcceptEvent();
                break;

            case InputEventMouseMotion when _dragging:
                Resize();
                AcceptEvent();
                break;
        }
    }

    /// <summary>
    /// Scale is derived from where the cursor is relative to the panel's top-left, as a multiple of
    /// the panel's unscaled size. Because the grip sits at the scaled bottom-right, grabbing it
    /// yields exactly the current scale, so the drag starts without a jump.
    /// </summary>
    private void Resize()
    {
        var baseSize = _target.Size;
        var offset = _target.GetGlobalMousePosition() - _target.GlobalPosition;

        var wanted = PanelScale.FromDrag(offset.X, offset.Y, baseSize.X, baseSize.Y, _settings.ClampedScale);
        if (Math.Abs(wanted - _settings.ClampedScale) < 0.001f)
        {
            return;
        }

        _settings.Scale = wanted;
        _settings.NotifyScaleChanged();
        UpdateTooltip();
    }
}
