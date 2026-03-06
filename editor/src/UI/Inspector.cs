//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using NoZ.Widgets;

namespace NoZ.Editor;

internal static partial class Inspector
{
    private const int MaxProperties = 128;

    public struct AutoSection : IDisposable { readonly void IDisposable.Dispose() => EndSection(); }

    [ElementId("Root")]
    [ElementId("Property", MaxProperties)]
    private static partial class ElementId { }

    private static int _nextPropertyId = 0;
    private static int _propertyId;
    private static bool _propertyEnabled;
    private static bool _propertyHovered;
    private static string? _dropdownText = null!;
    private static Color32 _valueColor;

    private static int GetNextPropertyId() => _nextPropertyId++;

    public static void UpdateUI()
    {
        if (!(Workspace.ActiveEditor?.ShowInspector ?? false))
            return;

        _nextPropertyId = ElementId.Property;

        using (UI.BeginColumn(ElementId.Root, EditorStyle.Inspector.Root))
            Workspace.ActiveEditor.InspectorUI();        
    }

    public static AutoSection BeginSection(string name, Action? content = null)
    {
        UI.BeginColumn(EditorStyle.Inspector.Section);
        using (UI.BeginRow(new ContainerStyle{ Height = EditorStyle.Control.Height}))
        {
            UI.Label(name, EditorStyle.Text.Primary);
            content?.Invoke();  
        }

        return new AutoSection();
    }

    public static UI.AutoRow BeginRow()
    {
        return UI.BeginRow(EditorStyle.Inspector.Row);
    }

    public static void EndSection()
    {
        UI.EndColumn();
        EditorUI.PanelSeparator();
    }

    public static bool Property(Action content, string? name = null, Sprite? icon = null, bool isEnabled = true, bool forceHovered=false) =>
        Property(GetNextPropertyId(), content, name, icon, isEnabled, forceHovered);

    private static bool Property(int id, Action content, string? name = null, Sprite? icon = null, bool isEnabled = true, bool forceHovered = false)
    {
        _propertyId = id;
        _propertyEnabled = isEnabled;
        _propertyHovered = forceHovered || UI.IsHovered(_propertyId);

        var pressed = false;
        using (UI.BeginRow(_propertyId, _propertyHovered ? EditorStyle.Control.RootHovered : EditorStyle.Control.Root))
        {
            if (icon != null)
                UI.Image(icon, EditorStyle.Icon.Secondary);

            if (name != null)
                UI.Label(name, EditorStyle.Text.Primary);

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
            UI.Label(_dropdownText!, EditorStyle.Text.Primary);

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
        using (UI.BeginContainer(new ContainerStyle { Width = Size.Percent(1), Height = Size.Fit }))
        {
            var hovered = UI.IsHovered(propertyId);

            value = multiLine
                ? Widget.TextArea(propertyId, value, hovered ? EditorStyle.Inspector.TextAreaHovered : EditorStyle.Inspector.TextArea, placeholder, handler)
                : Widget.TextBox(propertyId, value, hovered ? EditorStyle.Inspector.TextBoxHovered : EditorStyle.Inspector.TextBox, placeholder, handler);
        }

        return value;
    }

    public static bool Button(Sprite icon, bool enabled = true) =>
        EditorUI.Button(GetNextPropertyId(), icon, isEnabled: enabled);

    public static float SliderProperty(float value, float minValue=0.0f, float maxValue=1.0f, IChangeHandler? handler = null)
    {
        var propertyId = GetNextPropertyId();
        EditorUI.Slider(propertyId, ref value, minValue, maxValue);
        Widget.HandleChange(handler);
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
            UI.Label(hex, EditorStyle.Text.Secondary);

            UI.Flex();

            using (UI.BeginRow(new ContainerStyle { Width = Size.Fit }))
            {
                UI.Label(Strings.Number((int)((_valueColor.A / 255.0f) * 100)), EditorStyle.Text.Secondary);
                UI.Label("%", EditorStyle.Text.Secondary);
            }
        }

        _valueColor = color;

        var propertyId = GetNextPropertyId();
        if (Property(propertyId, Content, icon: icon, isEnabled: isEnabled, forceHovered: ColorPicker.IsOpen(propertyId)))
            ColorPicker.Open(propertyId, color);

        ColorPicker.Popup(propertyId, ref color);
        UI.SetLastElement(propertyId);
        Widget.HandleChange(handler);
        return color;
    }
}

