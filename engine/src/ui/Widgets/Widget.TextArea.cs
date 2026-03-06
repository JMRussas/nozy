//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ.Widgets;

public static partial class Widget
{
    public static bool TextArea(int id, ReadOnlySpan<char> text, TextAreaStyle style,
        ReadOnlySpan<char> placeholder = default, IChangeHandler? handler = null)
    {
        UI.TextArea(id, text, style, placeholder);
        var changed = UI.WasChanged();
        UI.SetLastElement(id);
        HandleChange(handler);
        return changed;
    }

    public static string TextArea(int id, string value, TextAreaStyle style,
        string? placeholder = null, IChangeHandler? handler = null)
    {
        if (TextArea(id, (ReadOnlySpan<char>)value, style, placeholder, handler))
            value = new string(UI.GetElementText(id));
        return value;
    }
}
