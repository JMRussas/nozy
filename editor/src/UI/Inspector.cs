//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ.Editor;

internal static partial class Inspector
{
    private const int MaxProperties = 128;
    private const int MaxSections = 32;

    public struct AutoSection : IDisposable
    {
        readonly void IDisposable.Dispose() => EndSection();
    }

    private struct SectionState
    {
        public byte Collapsed;
    }

    private static partial class ElementId
    {
        public static partial WidgetId Root { get; }
        public static partial WidgetId Section { get; }
        public static partial WidgetId Property { get; }
    }

    private static WidgetId _nextPropertyId;
    private static WidgetId _nextSectionId;
    private static WidgetId _propertyId;
    private static bool _propertyEnabled;
    private static bool _propertyHovered;
    private static bool _sectionCollapsed;
    private static bool _sectionActive;
    private static string? _dropdownText = null!;
    private static Color32 _valueColor;

    private static WidgetId GetNextPropertyId() => _nextPropertyId++;

    public static bool IsSectionCollapsed => _sectionCollapsed;

    public static void UpdateUI()
    {
        if (!(Workspace.ActiveEditor?.ShowInspector ?? false))
            return;

        _nextPropertyId = ElementId.Property;
        _nextSectionId = ElementId.Section;

        using (UI.BeginColumn(ElementId.Root, EditorStyle.Inspector.Root))
            Workspace.ActiveEditor.InspectorUI();
    }

    public static AutoSection BeginSection(string name, Sprite? icon = null, Action? content = null, bool isActive = false)
    {
        var sectionId = _nextSectionId++;
        _sectionActive = isActive;

        // Outer section wrapper
        ElementTree.BeginColumn();
        if (isActive)
        {
            ElementTree.BeginBorder(1, EditorStyle.Palette.Primary);
            ElementTree.BeginPadding(EdgeInsets.All(1));
            ElementTree.BeginColumn();
        }

        // Header (self-contained tree)
        ElementTree.BeginTree();
        ref var state = ref ElementTree.BeginWidget<SectionState>(sectionId);
        var flags = ElementTree.GetWidgetFlags();
        var hovered = flags.HasFlag(WidgetFlags.Hovered);

        if (flags.HasFlag(WidgetFlags.Pressed))
            state.Collapsed = (byte)(state.Collapsed != 0 ? 0 : 1);

        var iconColor = isActive ? EditorStyle.Palette.Content : EditorStyle.Palette.HeaderText;
        var headerBg = hovered ? EditorStyle.Palette.Secondary : EditorStyle.Palette.Header;
        var chevron = state.Collapsed != 0
            ? EditorAssets.Sprites.IconFoldoutClosed
            : EditorAssets.Sprites.IconFoldoutOpen;

        ElementTree.BeginSize(Size.Default, EditorStyle.Inspector.SectionHeaderHeight);
        ElementTree.BeginFill(headerBg);
        ElementTree.BeginPadding(EdgeInsets.LeftRight(8));
        ElementTree.BeginAlign(new Align2(Align.Min, Align.Center));
        ElementTree.BeginRow(EditorStyle.Inspector.HeaderGap);

        ElementTree.Image(chevron, EditorStyle.Inspector.IconSize, ImageStretch.Uniform, iconColor, 1.0f, new Align2(Align.Center, Align.Center));

        if (icon != null)
            ElementTree.Image(icon, EditorStyle.Inspector.IconSize, ImageStretch.Uniform, iconColor, 1.0f, new Align2(Align.Center, Align.Center));

        ElementTree.Text(name, UI.DefaultFont, EditorStyle.Inspector.HeaderFontSize, iconColor, new Align2(Align.Min, Align.Center));

        ElementTree.Flex();

        content?.Invoke();

        ElementTree.EndTree();

        _sectionCollapsed = state.Collapsed != 0;

        // Body (only if not collapsed)
        if (!_sectionCollapsed)
        {
            ElementTree.BeginColumn(EditorStyle.Inspector.BodyGap);
            ElementTree.BeginFill(EditorStyle.Palette.Panel);
            ElementTree.BeginPadding(EdgeInsets.Symmetric(EditorStyle.Inspector.BodyPaddingV, EditorStyle.Inspector.BodyPaddingH));
        }

        return new AutoSection();
    }

    public static UI.AutoRow BeginRow()
    {
        return UI.BeginRow(EditorStyle.Inspector.Row);
    }

    public static void EndSection()
    {
        if (!_sectionCollapsed)
        {
            ElementTree.EndPadding();
            ElementTree.EndFill();
            ElementTree.EndColumn();
        }

        _sectionCollapsed = false;

        if (_sectionActive)
        {
            ElementTree.EndColumn();
            ElementTree.EndPadding();
            ElementTree.EndBorder();
        }

        ElementTree.EndColumn();
        _sectionActive = false;
    }

    public static bool Property(Action content, string? name = null, Sprite? icon = null, bool isEnabled = true, bool forceHovered=false) =>
        Property(GetNextPropertyId(), content, name, icon, isEnabled, forceHovered);

    private static bool Property(WidgetId id, Action content, string? name = null, Sprite? icon = null, bool isEnabled = true, bool forceHovered = false)
    {
        _propertyId = id;
        _propertyEnabled = isEnabled;
        _propertyHovered = forceHovered || UI.IsHovered(_propertyId);

        var pressed = false;
        var propertyStyle = new ContainerStyle
        {
            Height = EditorStyle.Control.Height,
            Spacing = EditorStyle.Inspector.BodyGap,
            Padding = EdgeInsets.LeftRight(8)
        };
        if (_propertyHovered)
        {
            propertyStyle.BorderWidth = 1;
            propertyStyle.BorderColor = EditorStyle.Palette.FocusRing;
            propertyStyle.BorderRadius = EditorStyle.Inspector.BorderRadius;
        }
        using (UI.BeginRow(_propertyId, propertyStyle))
        {
            if (icon != null)
                UI.Image(icon, EditorStyle.Icon.Secondary);

            if (name != null)
                UI.Text(name, EditorStyle.Text.Primary);

            content();

            pressed = UI.WasPressed();
        }

        return pressed;
    }

    public static void DropdownProperty(
        string text,
        Func<PopupMenuItem[]> getItems,
        string? name = null,
        Sprite? icon = null,
        bool enabled = true
    )
    {
        _dropdownText = text;

        static void DropdownContent()
        {
            UI.Text(_dropdownText!, EditorStyle.Text.Primary);

            if (_propertyHovered)
                UI.Flex();

            UI.Spacer(EditorStyle.Control.Spacing);
            UI.Image(EditorAssets.Sprites.IconFoldoutClosed, EditorStyle.Icon.SecondarySmall);
        }

        if (Property(DropdownContent, name: name, icon: icon, isEnabled: enabled))
        {
            var style = EditorStyle.Popup.Left with { AnchorRect = UI.GetElementWorldRect(_propertyId) };
            PopupMenu.Open(_propertyId, getItems(), style);
        }
    }

    public static bool ToggleProperty(Sprite icon, ref bool value, bool enabled=true)
    {
        if (EditorUI.ToggleButton(GetNextPropertyId(), icon, isChecked: value, isEnabled: enabled))
        {
            value = !value;
            return true;
        }

        return false;
    }

    public static string StringProperty(string value, bool multiLine = false, string? placeholder = null, IChangeHandler? handler = null)
    {
        var propertyId = GetNextPropertyId();

        using (BeginRow())
        using (UI.BeginFlex())
        {
            var prev = value;
            value = UI.TextInput(propertyId, value, EditorStyle.Inspector.TextArea, placeholder, handler);
            if (value != prev && handler != null)
            {
                handler.BeginChange();
                handler.NotifyChange();
            }
        }

        return value;
    }

    public static bool Button(Sprite icon, bool enabled = true) =>
        EditorUI.Button(GetNextPropertyId(), icon, isEnabled: enabled);

    public static float SliderProperty(float value, float minValue=0.0f, float maxValue=1.0f, IChangeHandler? handler = null)
    {
        var propertyId = GetNextPropertyId();
        EditorUI.Slider(propertyId, ref value, minValue, maxValue);
        UI.HandleChange(handler);
        return value;
    }

    public static Color32 ColorProperty(Color32 color, Sprite? icon = null, bool isEnabled = true, IChangeHandler? handler = null)
    {
        static void Content()
        {
            if (_valueColor.A == 0)
            {
                UI.Image(EditorAssets.Sprites.IconNofill, EditorStyle.Icon.Primary);
                return;
            }

            UI.Container(EditorStyle.Inspector.ColorButton with { Color = _valueColor.ToColor() });
            UI.Spacer(EditorStyle.Control.Spacing);

            Span<char> hex = stackalloc char[6];
            Strings.ColorHex(_valueColor, hex);
            UI.Text(hex, EditorStyle.Text.Secondary);

            UI.Flex();

            using (UI.BeginRow(new ContainerStyle { Width = Size.Fit }))
            {
                UI.Text(Strings.Number((int)((_valueColor.A / 255.0f) * 100)), EditorStyle.Text.Secondary);
                UI.Text("%", EditorStyle.Text.Secondary);
            }
        }

        _valueColor = color;

        var propertyId = GetNextPropertyId();
        if (Property(propertyId, Content, icon: icon, isEnabled: isEnabled, forceHovered: ColorPicker.IsOpen(propertyId)))
            ColorPicker.Open(propertyId, color);

        ColorPicker.Popup(propertyId, ref color);
        UI.SetLastElement(propertyId);
        UI.HandleChange(handler);
        return color;
    }
}

