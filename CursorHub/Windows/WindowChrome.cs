using CursorHub.Core;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CursorHub.Windows;

internal sealed class WindowChrome : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Point { public int X, Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    private sealed record DesignPoint(float X, float Y);
    [DllImport("gdi32.dll", SetLastError = true)]
    private static extern nint CreatePolygonRgn(Point[] points, int count, int mode);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(nint region);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int SetWindowRgn(nint window, nint region, bool redraw);
    [DllImport("user32.dll")]
    private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll")]
    private static extern int SetWindowLong(nint window, int index, int value);
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(nint window, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();
    [DllImport("user32.dll")]
    private static extern bool PostMessage(nint window, uint message, nint wParam, nint lParam);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(nint window, int command);
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint window, int attribute, ref int value, int size);

    [DllImport("user32.dll")] private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] private static extern bool GetClientRect(nint window, out Rect rect);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Rect rect);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("gdi32.dll")] private static extern nint CreateEllipticRgn(int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] private static extern nint CreateRoundRectRgn(int left, int top, int right, int bottom, int ellipseWidth, int ellipseHeight);
    [DllImport("gdi32.dll")] private static extern int CombineRgn(nint destination, nint first, nint second, int mode);
    [DllImport("gdi32.dll")] private static extern nint CreateRectRgn(int left, int top, int right, int bottom);
    [DllImport("gdi32.dll")] private static extern bool PtInRegion(nint region, int x, int y);
    [DllImport("user32.dll")] private static extern int GetWindowRgn(nint window, nint region);
    private bool minimized, maximized;
    private Rect normalBounds;
    private readonly nint handle;
    private ChromeMask mask;
    private int width, height;
    private int left = int.MinValue, top = int.MinValue;
    private LayeredSurface? fringe;
    private int disposed;
    internal int SoftEdgePixels { get; private set; }

    public WindowChrome(nint handle, bool dark = false)
    {
        if (handle == 0) throw new InvalidOperationException("Janela nativa ainda indisponivel.");
        this.handle = handle;
        mask = new ChromeMask(AssetPaths.Directory, dark);
        SetWindowLong(handle, -16, GetWindowLong(handle, -16) & ~0x00CF0000);
        RemoveSystemBorder();
        SetWindowPos(handle, 0, 0, 0, 0, 0, 0x0037); // FRAMECHANGED, no move/size/activate/z-order
    }

    public void SetTheme(bool dark)
    {
        mask = new ChromeMask(AssetPaths.Directory, dark);
        width = height = 0;
    }

    private void RemoveSystemBorder()
    {
        int disabled = 1, noBorder = unchecked((int)0xFFFFFFFE);
        DwmSetWindowAttribute(handle, 2, ref disabled, sizeof(int));
        DwmSetWindowAttribute(handle, 33, ref disabled, sizeof(int));
        // Unsupported attributes on older Windows versions are simply ignored by DWM.
        DwmSetWindowAttribute(handle, 34, ref noBorder, sizeof(int));
    }

    public void Sync(int ignoredWidth = 0, int ignoredHeight = 0)
    {
        LayeredSurface.Pump();
        if (IsIconic(handle)) { minimized = true; fringe?.Hide(); return; }
        if (minimized) { width = height = 0; minimized = false; RemoveSystemBorder(); }
        int style = GetWindowLong(handle, -16);
        if ((style & 0x00CF0000) != 0)
        {
            SetWindowLong(handle, -16, style & ~0x00CF0000);
            RemoveSystemBorder();
            SetWindowPos(handle, 0, 0, 0, 0, 0, 0x0037);
            width = height = 0;
        }
        if (!GetClientRect(handle, out var client) || client.Right < 100 || client.Bottom < 100) return;
        int newWidth = client.Right, newHeight = client.Bottom;
        if (!GetWindowRect(handle, out var window)) return;
        if (newWidth == width && newHeight == height)
        {
            if (window.Left != left || window.Top != top) fringe?.Move(window.Left, window.Top);
            left = window.Left; top = window.Top;
            return;
        }
        nint region = CreateRectRgn(0, 0, 0, 0);
        if (region == 0) throw new Win32Exception();
        // Scan the actual PNG alpha, rather than independently approximating circles and arcs.
        // Merge vertically adjacent runs to keep the native region compact.
        var pending = new Dictionary<(int Left, int Right), Rect>();
        void Commit(Rect rectangle)
        {
            nint run = CreateRectRgn(rectangle.Left, rectangle.Top, rectangle.Right, rectangle.Bottom);
            if (run == 0) throw new Win32Exception();
            try { if (CombineRgn(region, region, run, 2) == 0) throw new Win32Exception(); }
            finally { DeleteObject(run); }
        }
        try
        {
            for (int y = 0; y < newHeight; y++)
            {
                var next = new Dictionary<(int Left, int Right), Rect>();
                for (int x = 0; x < newWidth;)
                {
                    if (!mask.Includes(x, y, newWidth, newHeight)) { x++; continue; }
                    int start = x++;
                    while (x < newWidth && mask.Includes(x, y, newWidth, newHeight)) x++;
                    var key = (start, x);
                    if (pending.Remove(key, out var previous)) { previous.Bottom = y + 1; next[key] = previous; }
                    else next[key] = new Rect { Left = start, Top = y, Right = x, Bottom = y + 1 };
                }
                foreach (var rectangle in pending.Values) Commit(rectangle);
                pending = next;
            }
            foreach (var rectangle in pending.Values) Commit(rectangle);
        }
        catch { DeleteObject(region); throw; }
        if (SetWindowRgn(handle, region, true) == 0) { DeleteObject(region); throw new Win32Exception(); }
        // Ownership of a successful region passes to Windows.
        width = newWidth; height = newHeight;
        fringe?.Dispose();
        fringe = new LayeredSurface(handle, width, height, false);
        var pixels = fringe.Pixels;
        SoftEdgePixels = 0;
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            uint pixel = mask.Fringe(x, y, width, height);
            pixels[y * width + x] = pixel;
            if ((pixel >> 24) is > 0 and < 255) SoftEdgePixels++;
        }
        fringe.Present(window.Left, window.Top, width, height);
        left = window.Left; top = window.Top;
    }

    public void ClearRegion()
    {
        SetWindowRgn(handle, 0, true);
        width = height = 0;
    }
    public void Drag() { ReleaseCapture(); PostMessage(handle, 0x00A1, 2, 0); }
    public void Minimize() { minimized = true; ShowWindow(handle, 6); }
    public void Restore() { ShowWindow(handle, 9); width = height = 0; }
    internal void VerifyCutouts()
    {
        GetClientRect(handle, out var client);
        nint region = CreateRectRgn(0, 0, 0, 0);
        try
        {
            if (GetWindowRgn(handle, region) == 0) throw new InvalidOperationException("Regiao nativa ausente.");
            var fit = InterfaceViewport.Fit(client.Right, client.Bottom);
            bool Contains(int x, int y) => PtInRegion(region, (int)(fit.Left + x * fit.Scale), (int)(fit.Top + y * fit.Scale));
            if (!Contains(969, 44) || !Contains(1013, 44) || !Contains(1057, 44) || !Contains(1010, 465))
                throw new InvalidOperationException("Ilhas de controle cortadas.");
            if (Contains(920, 35) || Contains(940, 480)) throw new InvalidOperationException("Recortes indevidamente preenchidos.");
        }
        finally { DeleteObject(region); }
    }
    internal void VerifyNoBlackPixels(Image<Rgba32> image)
    {
        image.ProcessPixelRows(rows =>
        {
            for (int y = 0; y < image.Height; y++)
            {
                var row = rows.GetRowSpan(y);
                for (int x = 0; x < image.Width; x++)
                {
                    if (!mask.Includes(x, y, image.Width, image.Height)) continue;
                    var pixel = row[x];
                    if (pixel.R < 5 && pixel.G < 5 && pixel.B < 5)
                        throw new InvalidOperationException($"Fundo preto visivel em {x},{y}.");
                }
            }
        });
    }
    public void ToggleMaximize()
    {
        if (!maximized)
        {
            GetWindowRect(handle, out normalBounds);
            var monitor = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (!GetMonitorInfo(MonitorFromWindow(handle, 2), ref monitor)) return;
            var r = monitor.Work;
            SetWindowPos(handle, 0, r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top, 0x0014);
        }
        else SetWindowPos(handle, 0, normalBounds.Left, normalBounds.Top, normalBounds.Right - normalBounds.Left, normalBounds.Bottom - normalBounds.Top, 0x0014);
        maximized = !maximized;
        width = height = 0;
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        fringe?.Dispose(); fringe = null;
    }
}
