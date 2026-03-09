//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ;

public struct DropDownStyle()
{
    public Size Width = Size.Fit;
    public Size Height = 30.0f;
    public Color Color = Color.Transparent;
    public Color ContentColor = Color.White;
    public float FontSize = 12.0f;
    public float IconSize = 16.0f;
    public float ArrowSize = 12.0f;
    public float Spacing = 6.0f;
    public float BorderRadius = 0;
    public float BorderWidth = 0;
    public Color BorderColor = Color.Transparent;
    public EdgeInsets Padding = EdgeInsets.Zero;
    public Font? Font = null;
    public Sprite? ArrowIcon = null;
    public PopupMenuStyle MenuStyle = new();
    public Func<DropDownStyle, WidgetFlags, DropDownStyle>? Resolve;

    public static readonly DropDownStyle Default = new();
}

public static partial class UI
{
    public static bool DropDown(
        WidgetId id,
        string text,
        PopupMenuItem[] items) =>
        DropDown(id, text, null, items, DropDownStyle.Default);

    public static bool DropDown(
        WidgetId id,
        string text,
        PopupMenuItem[] items,
        in DropDownStyle style) =>
        DropDown(id, text, null, items, style);

    public static bool DropDown(
        WidgetId id,
        string? text,
        Sprite? icon,
        PopupMenuItem[] items,
        in DropDownStyle style)
    {
        var isOpen = IsPopupMenuOpen(id);

        ElementTree.BeginTree();
        ElementTree.BeginWidget(id);

        var flags = ElementTree.GetWidgetFlags();
        if (isOpen)
            flags |= WidgetFlags.Checked;

        var s = style.Resolve != null ? style.Resolve(style, flags) : style;

        ElementTree.BeginSize(new Size2(s.Width, s.Height));

        if (s.BorderWidth > 0)
            ElementTree.BeginBorder(s.BorderWidth, s.BorderColor, s.BorderRadius);

        ElementTree.BeginFill(s.Color, s.BorderRadius);

        if (!s.Padding.IsZero)
            ElementTree.BeginPadding(s.Padding);

        ElementTree.BeginRow(s.Spacing);

        if (icon != null)
        {
            ElementTree.Image(
                icon,
                s.IconSize,
                ImageStretch.Uniform,
                s.ContentColor,
                1.0f,
                new Align2(Align.Min, Align.Center));
        }

        if (text != null)
        {
            var font = s.Font ?? _defaultFont!;
            ElementTree.Text(
                text,
                font,
                s.FontSize,
                s.ContentColor,
                new Align2(Align.Min, Align.Center),
                TextOverflow.Ellipsis);
        }

        ElementTree.Flex();

        if (s.ArrowIcon != null)
        {
            ElementTree.Image(
                s.ArrowIcon,
                s.ArrowSize,
                ImageStretch.Uniform,
                s.ContentColor,
                1.0f,
                new Align2(Align.Center, Align.Center));
        }

        ElementTree.EndTree();

        if (flags.HasFlag(WidgetFlags.Pressed))
        {
            if (isOpen)
            {
                ClosePopupMenu();
            }
            else
            {
                var anchorRect = GetElementWorldRect(id);
                var popupStyle = new PopupStyle
                {
                    AnchorX = Align.Min,
                    AnchorY = Align.Max,
                    PopupAlignX = Align.Min,
                    PopupAlignY = Align.Min,
                    Spacing = 2.0f,
                    ClampToScreen = true,
                    AnchorRect = anchorRect,
                    MinWidth = anchorRect.Width,
                };
                OpenPopupMenu(id, items, s.MenuStyle, popupStyle);
            }
        }

        return flags.HasFlag(WidgetFlags.Pressed);
    }
}
