//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using System.Diagnostics;

namespace NoZ.Editor;

internal static partial class Inspector
{
    private const int MaxProperties = 128;

    public struct AutoSection : IDisposable { readonly void IDisposable.Dispose() => EndSection(); }

    [ElementId("Property", MaxProperties)]
    private static partial class ElementId { }

    private static int _nextPropertyId = 0;
    private static int _propertyId;
    private static bool _propertyEnabled;
    private static PopupMenuItem[]? _dropdownItems = null!;
    private static string? _dropdownLabel;
    private static Sprite? _dropdownIcon;
    private static Color? _dropdownIconColor;

    public static void UpdateUI()
    {
        if (!(Workspace.ActiveEditor?.ShowInspector ?? false))
            return;

        _nextPropertyId = ElementId.Property;

        using (UI.BeginColumn(EditorStyle.Inspector.Root))
            Workspace.ActiveEditor.InspectorUI();        
    }

    public static AutoSection BeginSection(string name)
    {
        UI.BeginContainer(EditorStyle.Inspector.Section);

        return new AutoSection();
    }

    public static void EndSection()
    {
        UI.EndContainer();        
    }

    private static void Property(string name, Action<int> content)
    {
        _propertyId = _nextPropertyId++;
        using (UI.BeginRow(EditorStyle.Inspector.Property))
        {
            using (UI.BeginContainer(new ContainerStyle { Width = 80 }))
                UI.Label(name, _propertyEnabled ? EditorStyle.Control.Text : EditorStyle.Control.DisabledText);
            content(_propertyId);
        }
    }

    public static void Property(string name, Action<int> content, bool isEnabled = true)
    {
        _propertyEnabled = isEnabled;
        Property(name, content);
    }

    public static void DropdownProperty(
        string name,
        string label,
        PopupMenuItem[] items,
        Sprite? icon = null,
        Color? iconColor = null)
    {
        Debug.Assert(_dropdownItems != null);

        _dropdownItems = items;
        _dropdownLabel = label;
        _dropdownIcon = icon;
        _dropdownIconColor = iconColor;
        Property(name, DropdownContent, true);
    }

    private static void DropdownContent(int id)
    {
        var label = _dropdownLabel!;
        var icon = _dropdownIcon;
        var iconColor = _dropdownIconColor;

        void ControlContent()
        {
            using (UI.BeginRow(new ContainerStyle
            {
                Width = 180,
                Padding = EdgeInsets.LeftRight(12)
            }))
            {
                if (icon != null)
                {
                    UI.Image(icon, EditorStyle.Control.Icon with { Color = iconColor ?? Color.White });
                    UI.Spacer(EditorStyle.Control.Spacing);
                }
                UI.Label(label, EditorStyle.Control.Text);
                UI.Flex();
                UI.Image(EditorAssets.Sprites.IconFoldoutClosed, EditorStyle.Control.Icon);
            }
        }

        if (EditorUI.Control(id, ControlContent, selected: PopupMenu.IsOpen(id)))
        {
            var style = EditorStyle.Popup.Left with { AnchorRect = UI.GetElementWorldRect(id) };
            PopupMenu.Open(id, _dropdownItems!, style);
        }
    }
}

