using Godot;

namespace SpireSense.SpireSenseCode.Overlay;

/// <summary>
/// Holds a panel's content with a <see cref="ResizeGrip"/> floating over its bottom-right corner.
///
/// A PanelContainer stretches every child to fill it, so a grip added directly beside the content
/// would be stretched too, and adding it to the content column would cost a whole row. This sits
/// between them: it reports the content's minimum size as its own, so the panel still sizes to its
/// content, while the grip is anchored on top and takes no layout space.
/// </summary>
public partial class GripLayer : Control
{
    private Control _content = null!;

    public static GripLayer Wrap(Control content, ResizeGrip grip)
    {
        var layer = new GripLayer { Name = "GripLayer", _content = content, MouseFilter = MouseFilterEnum.Ignore };

        content.SetAnchorsPreset(LayoutPreset.FullRect);
        layer.AddChild(content);
        layer.AddChild(grip);

        // The panel must grow and shrink as the content does, which only happens if this wrapper
        // re-reports its minimum size when the content's changes.
        content.MinimumSizeChanged += layer.UpdateMinimumSize;
        return layer;
    }

    public override Vector2 _GetMinimumSize() =>
        _content == null ? Vector2.Zero : _content.GetCombinedMinimumSize();
}
