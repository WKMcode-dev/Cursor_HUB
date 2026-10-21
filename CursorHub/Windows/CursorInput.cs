using System.Runtime.InteropServices;

namespace CursorHub.Windows;

internal sealed class CursorInput
{
    [StructLayout(LayoutKind.Sequential)] private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern nint FindWindow(string? className, string title);
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] private static extern bool ScreenToClient(nint window, ref Point point);
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint window, out Rect rect);
    private readonly nint handle;
    public float PixelScale { get; private set; } = 1;
    public CursorInput(nint handle) => this.handle = handle;
    public System.Numerics.Vector2? Cursor(float width, float height)
    {
        if (handle == 0 || !GetCursorPos(out var p) || !ScreenToClient(handle, ref p)
            || !GetClientRect(handle, out var area) || area.Right <= 0 || area.Bottom <= 0) return null;
        PixelScale = width / area.Right;
        if (p.X < 0 || p.Y < 0 || p.X >= area.Right || p.Y >= area.Bottom) return null;
        return new(p.X * width / area.Right, p.Y * height / area.Bottom);
    }
}
