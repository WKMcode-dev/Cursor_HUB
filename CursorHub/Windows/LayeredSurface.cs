using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CursorHub.Windows;

// Small per-pixel-alpha surface. No screen capture, GPU readback or fullscreen opaque window.
internal sealed unsafe class LayeredSurface : IDisposable
{
    [StructLayout(LayoutKind.Sequential)] internal struct Point { public int X, Y; public Point(int x, int y) { X = x; Y = y; } }
    [StructLayout(LayoutKind.Sequential)] private struct Size { public int Width, Height; }
    [StructLayout(LayoutKind.Sequential, Pack = 1)] private struct Blend { public byte Operation, Flags, Alpha, Format; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, BitCount;
        public uint Compression, ImageSize; public int XPels, YPels; public uint Used, Important;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Message
    {
        public nint HWnd; public uint Msg; public nuint WParam; public nint LParam;
        public uint Time; public Point Point; public uint Private;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(uint exStyle, string className, string title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint window);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern nint CreateCompatibleDC(nint dc);
    [DllImport("gdi32.dll")] private static extern bool DeleteDC(nint dc);
    [DllImport("gdi32.dll")] private static extern nint SelectObject(nint dc, nint item);
    [DllImport("gdi32.dll")] internal static extern bool DeleteObject(nint item);
    [DllImport("gdi32.dll", SetLastError = true)] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint use, out nint bits, nint section, uint offset);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool UpdateLayeredWindow(nint window, nint dst, ref Point pos, ref Size size, nint src, ref Point source, uint key, ref Blend blend, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint window, nint after, int x, int y, int w, int h, uint flags);
    [DllImport("user32.dll")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")] internal static extern bool GetCursorPos(out Point point);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("user32.dll")] private static extern bool PeekMessage(out Message message, nint window, uint min, uint max, uint remove);
    [DllImport("user32.dll")] private static extern bool TranslateMessage(ref Message message);
    [DllImport("user32.dll")] private static extern nint DispatchMessage(ref Message message);

    private readonly nint window, dc, oldBitmap;
    private readonly nint bitmap;
    private readonly nint bits;
    private readonly int bufferWidth, bufferHeight;
    private readonly bool topmost;
    public const int Extent = 1024;
    public Span<uint> Pixels => new((void*)bits, bufferWidth * bufferHeight);

    public LayeredSurface(nint owner = 0, int bufferWidth = Extent, int bufferHeight = Extent, bool topmost = true)
    {
        this.bufferWidth = bufferWidth; this.bufferHeight = bufferHeight; this.topmost = topmost;
        // LAYERED | TRANSPARENT | TOOLWINDOW | NOACTIVATE; WS_POPUP.
        window = CreateWindowEx(0x080800A0u | (topmost ? 8u : 0u), "STATIC", "CursorHub alpha layer", 0x80000000, 0, 0, bufferWidth, bufferHeight, owner, 0, 0, 0);
        if (window == 0) throw new Win32Exception();
        nint screen = GetDC(0);
        try
        {
            dc = CreateCompatibleDC(screen);
            var info = new BitmapInfo { Size = (uint)Marshal.SizeOf<BitmapInfo>(), Width = bufferWidth, Height = -bufferHeight, Planes = 1, BitCount = 32 };
            bitmap = CreateDIBSection(screen, ref info, 0, out bits, 0, 0);
            if (dc == 0 || bitmap == 0 || bits == 0) throw new Win32Exception();
            oldBitmap = SelectObject(dc, bitmap);
        }
        catch { Dispose(); throw; }
        finally { ReleaseDC(0, screen); }
    }

    public void Present(int x, int y, int width, int height)
    {
        var pos = new Point(x, y); var source = new Point();
        var size = new Size { Width = width, Height = height };
        var blend = new Blend { Alpha = 255, Format = 1 };
        if (!UpdateLayeredWindow(window, 0, ref pos, ref size, dc, ref source, 0, ref blend, 2)) throw new Win32Exception();
        ShowWindow(window, 4); // Explicit SW_SHOWNOACTIVATE: presentation alone is not visibility.
        if (!SetWindowPos(window, topmost ? -1 : 0, 0, 0, 0, 0, topmost ? 0x0053u : 0x0057u))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows nao posicionou a camada de efeitos.");
        if (!IsWindowVisible(window) || topmost && (GetWindowLong(window, -20) & 8) == 0)
            throw new InvalidOperationException("A camada nao ficou visivel e acima das outras janelas.");
    }
    public void Move(int x, int y) => SetWindowPos(window, 0, x, y, 0, 0, 0x0055);
    public void Hide() => ShowWindow(window, 0);
    public static void Pump()
    {
        while (PeekMessage(out var message, 0, 0, 0, 1)) { TranslateMessage(ref message); DispatchMessage(ref message); }
    }

    // Source-over blend, premultiplied BGRA, fractional coverage for a smooth circle edge.
    internal static void Circle(Span<uint> pixels, int stride, int width, int height, float x, float y, float radius, byte r, byte g, byte b, float alpha)
    {
        int x0 = Math.Max(0, (int)MathF.Floor(x - radius - 1)), x1 = Math.Min(width - 1, (int)MathF.Ceiling(x + radius + 1));
        int y0 = Math.Max(0, (int)MathF.Floor(y - radius - 1)), y1 = Math.Min(height - 1, (int)MathF.Ceiling(y + radius + 1));
        for (int py = y0; py <= y1; py++)
        for (int px = x0; px <= x1; px++)
        {
            float dx = px + .5f - x, dy = py + .5f - y;
            float coverage = Math.Clamp(radius + .5f - MathF.Sqrt(dx * dx + dy * dy), 0, 1);
            int a = (int)(Math.Clamp(alpha, 0, 1) * coverage * 255);
            if (a == 0) continue;
            int index = py * stride + px; uint dst = pixels[index]; int inv = 255 - a;
            uint blue = (uint)((b * a + (dst & 255) * inv) / 255);
            uint green = (uint)((g * a + ((dst >> 8) & 255) * inv) / 255);
            uint red = (uint)((r * a + ((dst >> 16) & 255) * inv) / 255);
            uint outAlpha = (uint)(a + ((dst >> 24) * inv) / 255);
            pixels[index] = blue | (green << 8) | (red << 16) | (outAlpha << 24);
        }
    }

    public void Dispose()
    {
        if (dc != 0 && oldBitmap != 0) SelectObject(dc, oldBitmap);
        if (bitmap != 0) DeleteObject(bitmap);
        if (dc != 0) DeleteDC(dc);
        if (window != 0) DestroyWindow(window);
    }
}
