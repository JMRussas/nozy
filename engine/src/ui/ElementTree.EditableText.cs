//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using System.Numerics;

namespace NoZ;

internal struct EditableTextElement
{
    public UnsafeSpan<char> Text;
    public float FontSize;
    public Color TextColor;
    public Color BgColor;
    public Color CursorColor;
    public Color BorderColor;
    public float BorderWidth;
    public float Height;
    public bool MultiLine;
    public ushort FontAssetIndex;
    public ushort PlaceholderAssetIndex;
    public UnsafeSpan<char> Placeholder;
}

internal struct TextBoxState
{
    public int CursorIndex;
    public int SelectionStart;
    public float ScrollOffset;
    public float BlinkTimer;
    public int TextHash;
    public int PrevTextHash;
    public UnsafeSpan<char> EditText;
    public byte Focused;
    public byte FocusEntered;
    public byte FocusExited;
    public byte WasCancelled;
}

public static unsafe partial class ElementTree
{
    public static bool EditableText(int widgetId, ref string value, Font font, float fontSize,
        Color textColor, Color bgColor, Color cursorColor = default,
        string placeholder = "", bool multiLine = false,
        float height = 0f, Color borderColor = default, float borderWidth = 1f)
    {
        if (cursorColor.IsTransparent) cursorColor = textColor;
        if (height <= 0) height = multiLine ? fontSize * 4 : fontSize * 1.8f;

        var changed = false;

        BeginWidget(widgetId);
        ref var state = ref GetState<TextBoxState>();

        var focused = HasFocus();
        state.FocusEntered = 0;
        state.FocusExited = 0;
        state.WasCancelled = 0;

        // Click to focus
        if (WasPressed() && !focused)
        {
            SetFocus();
            focused = true;
            state.CursorIndex = value.Length;
            state.SelectionStart = 0;
            state.BlinkTimer = 0;
            state.FocusEntered = 1;
        }

        // Handle keyboard input when focused
        if (focused)
        {
            state.BlinkTimer += Time.DeltaTime;

            if (state.Focused == 0)
            {
                state.EditText = Text(value);
                state.TextHash = value.GetHashCode();
                state.PrevTextHash = state.TextHash;
                state.Focused = 1;
                state.FocusEntered = 1;
            }
            else
            {
                state.EditText = Text(state.EditText.AsReadOnlySpan());
            }

            var scope = InputScope.All;
            var text = state.EditText.AsReadOnlySpan();

            if (Input.WasButtonPressed(InputCode.KeyEscape, scope))
            {
                Input.ConsumeButton(InputCode.KeyEscape);
                ClearFocus();
                state.Focused = 0;
                state.FocusExited = 1;
                state.WasCancelled = 1;
            }
            else if (Input.WasButtonPressed(InputCode.KeyEnter, scope))
            {
                Input.ConsumeButton(InputCode.KeyEnter);
                if (!multiLine)
                {
                    var newText = new string(state.EditText.AsReadOnlySpan());
                    if (newText != value)
                    {
                        value = newText;
                        changed = true;
                    }
                    ClearFocus();
                    state.Focused = 0;
                    state.FocusExited = 1;
                }
                else
                {
                    RemoveSelected(ref state);
                    state.EditText = UI.InsertText(state.EditText.AsReadOnlySpan(), state.CursorIndex, "\n");
                    state.CursorIndex++;
                    state.SelectionStart = state.CursorIndex;
                }
            }
            else if (Input.WasButtonPressed(InputCode.KeyTab, scope))
            {
                Input.ConsumeButton(InputCode.KeyTab);
                var newText = new string(state.EditText.AsReadOnlySpan());
                if (newText != value)
                {
                    value = newText;
                    changed = true;
                }
                ClearFocus();
                state.Focused = 0;
                state.FocusExited = 1;
            }
            else
            {
                if (Input.WasButtonPressed(InputCode.KeyLeft, true, scope))
                {
                    Input.ConsumeButton(InputCode.KeyLeft);
                    if (state.CursorIndex > 0) state.CursorIndex--;
                    if (!Input.IsShiftDown(scope)) state.SelectionStart = state.CursorIndex;
                    state.BlinkTimer = 0;
                }
                if (Input.WasButtonPressed(InputCode.KeyRight, true, scope))
                {
                    Input.ConsumeButton(InputCode.KeyRight);
                    if (state.CursorIndex < text.Length) state.CursorIndex++;
                    if (!Input.IsShiftDown(scope)) state.SelectionStart = state.CursorIndex;
                    state.BlinkTimer = 0;
                }
                if (Input.WasButtonPressed(InputCode.KeyHome, scope))
                {
                    Input.ConsumeButton(InputCode.KeyHome);
                    state.CursorIndex = 0;
                    if (!Input.IsShiftDown(scope)) state.SelectionStart = state.CursorIndex;
                }
                if (Input.WasButtonPressed(InputCode.KeyEnd, scope))
                {
                    Input.ConsumeButton(InputCode.KeyEnd);
                    state.CursorIndex = text.Length;
                    if (!Input.IsShiftDown(scope)) state.SelectionStart = state.CursorIndex;
                }

                if (Input.IsCtrlDown(scope) && Input.WasButtonPressed(InputCode.KeyA, scope))
                {
                    Input.ConsumeButton(InputCode.KeyA);
                    state.SelectionStart = 0;
                    state.CursorIndex = text.Length;
                }

                if (Input.IsCtrlDown(scope) && Input.WasButtonPressed(InputCode.KeyC, scope))
                {
                    Input.ConsumeButton(InputCode.KeyC);
                    if (state.CursorIndex != state.SelectionStart)
                    {
                        var start = Math.Min(state.CursorIndex, state.SelectionStart);
                        var length = Math.Abs(state.CursorIndex - state.SelectionStart);
                        Application.Platform.SetClipboardText(new string(text.Slice(start, length)));
                    }
                }

                if (Input.IsCtrlDown(scope) && Input.WasButtonPressed(InputCode.KeyV, scope))
                {
                    Input.ConsumeButton(InputCode.KeyV);
                    var clipboard = Application.Platform.GetClipboardText();
                    if (!string.IsNullOrEmpty(clipboard))
                    {
                        RemoveSelected(ref state);
                        state.EditText = UI.InsertText(state.EditText.AsReadOnlySpan(), state.CursorIndex, clipboard);
                        state.CursorIndex += clipboard.Length;
                        state.SelectionStart = state.CursorIndex;
                    }
                }

                if (Input.IsCtrlDown(scope) && Input.WasButtonPressed(InputCode.KeyX, scope))
                {
                    Input.ConsumeButton(InputCode.KeyX);
                    if (state.CursorIndex != state.SelectionStart)
                    {
                        var start = Math.Min(state.CursorIndex, state.SelectionStart);
                        var length = Math.Abs(state.CursorIndex - state.SelectionStart);
                        Application.Platform.SetClipboardText(new string(text.Slice(start, length)));
                        RemoveSelected(ref state);
                    }
                }

                if (Input.WasButtonPressed(InputCode.KeyBackspace, true, scope))
                {
                    Input.ConsumeButton(InputCode.KeyBackspace);
                    if (state.CursorIndex != state.SelectionStart)
                        RemoveSelected(ref state);
                    else if (state.CursorIndex > 0)
                    {
                        state.EditText = UI.RemoveText(state.EditText.AsReadOnlySpan(), state.CursorIndex - 1, 1);
                        state.CursorIndex--;
                        state.SelectionStart = state.CursorIndex;
                    }
                }

                if (Input.WasButtonPressed(InputCode.KeyDelete, true, scope))
                {
                    Input.ConsumeButton(InputCode.KeyDelete);
                    if (state.CursorIndex != state.SelectionStart)
                        RemoveSelected(ref state);
                    else if (state.CursorIndex < text.Length)
                    {
                        state.EditText = UI.RemoveText(state.EditText.AsReadOnlySpan(), state.CursorIndex, 1);
                    }
                }

                var input = Input.GetTextInput(scope);
                if (!string.IsNullOrEmpty(input))
                {
                    RemoveSelected(ref state);
                    state.EditText = UI.InsertText(state.EditText.AsReadOnlySpan(), state.CursorIndex, input);
                    state.CursorIndex += input.Length;
                    state.SelectionStart = state.CursorIndex;
                }

                for (var i = (int)InputCode.KeyA; i <= (int)InputCode.KeyRightSuper; i++)
                    Input.ConsumeButton((InputCode)i);
            }

            var len = state.EditText.Length;
            state.CursorIndex = Math.Clamp(state.CursorIndex, 0, len);
            state.SelectionStart = Math.Clamp(state.SelectionStart, 0, len);

            // Track per-frame text changes
            state.PrevTextHash = state.TextHash;
            state.TextHash = string.GetHashCode(state.EditText.AsReadOnlySpan());
        }
        else
        {
            if (state.Focused == 1)
                state.FocusExited = 1;
            state.Focused = 0;
        }

        // Visual
        if (!borderColor.IsTransparent && borderWidth > 0)
            BeginBorder(borderWidth, focused ? cursorColor : borderColor, BorderRadius.Circular(2));
        BeginSize(Size.Percent(1), new Size(height));
        BeginFill(bgColor);
        BeginPadding(new EdgeInsets(4, 6, 4, 6));
        {
            var displayText = focused ? state.EditText : Text(value);
            if (displayText.Length > 0)
            {
                Label(displayText, font, fontSize, textColor,
                    overflow: multiLine ? TextOverflow.Wrap : TextOverflow.Overflow);
            }
            else if (placeholder.Length > 0)
            {
                Label(Text(placeholder), font, fontSize, textColor.WithAlpha(0.4f));
            }
        }
        EndPadding();
        EndFill();
        EndSize();
        if (!borderColor.IsTransparent && borderWidth > 0)
            EndBorder();

        // Commit on focus loss
        if (state.Focused == 1 && !focused && state.FocusExited == 0)
        {
            var newText = new string(state.EditText.AsReadOnlySpan());
            if (newText != value)
            {
                value = newText;
                changed = true;
            }
            state.Focused = 0;
            state.FocusExited = 1;
        }

        EndWidget();
        return changed;
    }

    private static void RemoveSelected(ref TextBoxState state)
    {
        if (state.CursorIndex == state.SelectionStart) return;
        var start = Math.Min(state.CursorIndex, state.SelectionStart);
        var length = Math.Abs(state.CursorIndex - state.SelectionStart);
        state.EditText = UI.RemoveText(state.EditText.AsReadOnlySpan(), start, length);
        state.CursorIndex = start;
        state.SelectionStart = start;
    }

    public static ReadOnlySpan<char> GetEditableText(int widgetId)
    {
        if (!HasFocusOn(widgetId)) return default;
        ref var state = ref GetStateByWidgetId<TextBoxState>(widgetId);
        if (state.Focused == 0) return default;
        return state.EditText.AsReadOnlySpan();
    }

    public static void SetEditableText(int widgetId, ReadOnlySpan<char> value, bool selectAll = false)
    {
        ref var state = ref GetStateByWidgetId<TextBoxState>(widgetId);
        state.EditText = Text(value);
        state.TextHash = string.GetHashCode(value);
        state.CursorIndex = value.Length;
        state.SelectionStart = selectAll ? 0 : value.Length;
        _focusId = widgetId;
        state.Focused = 1;
    }

    // Stub for layout — EditableText is currently a compound widget, not a true leaf element.
    // These will be used when we make it a proper leaf element.
    private static float FitEditableTextAxis(ref BaseElement e, int axis) => 0;
    private static float LayoutEditableTextAxis(ref BaseElement e, int axis, float available) => 0;
    private static void DrawEditableText(ref BaseElement e) { }
}
