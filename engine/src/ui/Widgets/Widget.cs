//
//  NoZ - Copyright(c) 2026 NoZ Games, LLC
//

using System.Runtime.InteropServices;

namespace NoZ.Widgets;

public interface IChangeHandler
{
    void BeginChange();
    void NotifyChange();
    void CancelChange();
}

public static partial class Widget
{
    private const int StatePoolSize = 16 * 1024;

    private static NativeArray<byte>[] _statePools = [new(StatePoolSize, 0), new(StatePoolSize, 0)];
    private static int _currentStatePool;

    internal static void BeginFrame()
    {
        _currentStatePool ^= 1;
        _statePools[_currentStatePool].Clear();
    }

    /// <summary>
    /// Get persistent, frame-tracked widget state for the given element ID.
    /// State is automatically copied from the previous frame if it exists,
    /// or zero-initialized if fresh.
    /// </summary>
    public static unsafe ref T GetState<T>(int id) where T : unmanaged
    {
        ref var es = ref UI.GetElementState(id);
        var frame = UI.Frame;

        // Already allocated this frame — return existing ref
        if (es.WidgetStateFrame == frame)
            return ref *(T*)(_statePools[_currentStatePool].Ptr + es.WidgetStateOffset);

        // Bump-allocate in write pool (8-byte aligned)
        var size = (sizeof(T) + 7) & ~7;
        ref var writePool = ref _statePools[_currentStatePool];
        var offset = writePool.Length;
        writePool.AddRange(size);

        // Copy from previous frame's read pool, or zero-init
        var ptr = writePool.Ptr + offset;
        if (es.WidgetStateFrame == (ushort)(frame - 1))
        {
            var readPool = _statePools[_currentStatePool ^ 1];
            NativeMemory.Copy(readPool.Ptr + es.WidgetStateOffset, ptr, (nuint)sizeof(T));
        }
        else
        {
            NativeMemory.Clear(ptr, (nuint)sizeof(T));
        }

        es.WidgetStateOffset = offset;
        es.WidgetStateFrame = frame;
        return ref *(T*)ptr;
    }

    public static void HandleChange(IChangeHandler? handler)
    {
        if (handler == null) return;
        if (UI.HotEnter()) handler.BeginChange();
        if (UI.WasChanged()) handler.NotifyChange();
        if (UI.HotExit() && !UI.IsChanged()) handler.CancelChange();
    }
}
