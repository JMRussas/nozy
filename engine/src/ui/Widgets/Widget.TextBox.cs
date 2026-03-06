//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

namespace NoZ.Widgets;

public static partial class Widget
{
    public static bool TextBox(int id, ReadOnlySpan<char> text, TextBoxStyle style,
        ReadOnlySpan<char> placeholder = default, IChangeHandler? handler = null)
    {
        UI.TextBox(id, text, style, placeholder);
        var changed = UI.WasChanged();
        UI.SetLastElement(id);
        HandleChange(handler);
        return changed;
    }

    public static string TextBox(int id, string value, TextBoxStyle style,
        string? placeholder = null, IChangeHandler? handler = null)
    {
        if (TextBox(id, (ReadOnlySpan<char>)value, style, placeholder, handler))
            value = new string(UI.GetElementText(id));
        return value;
    }
}
