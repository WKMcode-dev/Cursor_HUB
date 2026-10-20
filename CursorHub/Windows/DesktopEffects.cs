using System.Diagnostics;
using System.Numerics;
using CursorHub.Core;
using System.Runtime.InteropServices;

namespace CursorHub.Windows;

internal sealed record EffectOptions(int Size, int Lifetime, int Rate, byte R, byte G, byte B, bool CustomizeCursor = true);

internal sealed class DesktopEffects : IDisposable
{
    [DllImport("user32.dll")] private static extern nint SetThreadDpiAwarenessContext(nint context);
    private readonly Thread thread;
    private readonly Func<LayeredSurface.Point>? probePosition;
    private readonly AutoResetEvent wake = new(false);
    private EffectOptions? requested;
    private volatile bool closing, active;
    private int disposed;
    private string? error;
    private string? details;
    private string? warning;
    private long frames;
    public bool Active => active;
    public string? Error => Volatile.Read(ref error);
    internal string? Details => Volatile.Read(ref details);
    public string? Warning => Volatile.Read(ref warning);
    internal long PresentedFrames => Interlocked.Read(ref frames);
    public DesktopEffects(Func<LayeredSurface.Point>? probePosition = null)
    {
        this.probePosition = probePosition;
        thread = new Thread(Run) { IsBackground = true, Name = "CursorHub desktop compositor" };
        thread.Start();
    }
    public void Play(EffectOptions options)
    {
        if (!thread.IsAlive) { Volatile.Write(ref error, error ?? "A camada global nao esta disponivel."); return; }
        Volatile.Write(ref error, null); Volatile.Write(ref warning, null); Volatile.Write(ref requested, options); wake.Set();
    }
    public void Stop() { Volatile.Write(ref requested, null); wake.Set(); }

    private void Run()
    {
        try
        {
            SetThreadDpiAwarenessContext(-4); // Physical screen coordinates, including mixed-DPI displays.
            using var surface = new LayeredSurface();
            using var cursors = new CursorController();
            var trail = new TrailBuffer(256);
            var clock = Stopwatch.StartNew();
            EffectOptions? applied = null;
            LayeredSurface.Point? last = null;
            bool visible = false, chordHeld = false;
            while (!closing)
            {
                LayeredSurface.Pump();
                var settings = Volatile.Read(ref requested);
                bool chord = LayeredSurface.GetAsyncKeyState(0x11) < 0 && LayeredSurface.GetAsyncKeyState(0x10) < 0 && LayeredSurface.GetAsyncKeyState(0x58) < 0;
                if (chord && !chordHeld) { Stop(); settings = null; }
                chordHeld = chord;
                if (settings is null)
                {
                    if (applied is not null) { cursors.Restore(); surface.Hide(); trail.Clear(); applied = null; last = null; visible = false; }
                    active = false;
                    wake.WaitOne(60);
                    continue;
                }
                try
                {
                    if (applied != settings)
                    {
                        if (settings.CustomizeCursor)
                        {
                            try { cursors.Apply(settings); }
                            catch (Exception exception)
                            {
                                // A denied cursor scheme change must not disable the desktop trail.
                                cursors.Restore();
                                Volatile.Write(ref warning, "Rastro ativo; Windows manteve o cursor original: " + exception.Message);
                            }
                        }
                        else cursors.Restore();
                        applied = settings;
                    }
                    active = true;
                    double frameStart = clock.Elapsed.TotalMilliseconds;
                    LayeredSurface.Point point;
                    if (probePosition is not null) point = probePosition();
                    else if (!LayeredSurface.GetCursorPos(out point)) { surface.Hide(); wake.WaitOne(30); continue; }
                    bool moved = last is null || point.X != last.Value.X || point.Y != last.Value.Y;
                    last = point;
                    trail.Advance(new Vector2(point.X, point.Y), frameStart, settings.Lifetime, Math.Max(2, settings.Size / 4f));
                    if (moved || trail.Count > 0)
                    {
                        int padding = settings.Size + 3;
                        int left = point.X - padding, top = point.Y - padding, right = point.X + padding, bottom = point.Y + padding;
                        // Ignore points too far from the tip rather than allocating desktop-sized bitmaps.
                        for (int i = 0; i < trail.Count; i++)
                        {
                            var p = trail[i].Position;
                            if (Math.Abs(p.X - point.X) > 450 || Math.Abs(p.Y - point.Y) > 450) continue;
                            left = Math.Min(left, (int)p.X - padding); right = Math.Max(right, (int)p.X + padding);
                            top = Math.Min(top, (int)p.Y - padding); bottom = Math.Max(bottom, (int)p.Y + padding);
                        }
                        int width = Math.Min(LayeredSurface.Extent, right - left + 1), height = Math.Min(LayeredSurface.Extent, bottom - top + 1);
                        var pixels = surface.Pixels;
                        for (int row = 0; row < height; row++) pixels.Slice(row * LayeredSurface.Extent, width).Clear();
                        for (int i = 0; i < trail.Count; i++)
                        {
                            var sample = trail[i];
                            float remaining = Math.Clamp(1 - (float)((frameStart - sample.Born) / settings.Lifetime), 0, 1);
                            LayeredSurface.Circle(pixels, LayeredSurface.Extent, width, height,
                                sample.Position.X - left, sample.Position.Y - top, settings.Size * (.125f + remaining * .325f), settings.R, settings.G, settings.B, remaining * .65f);
                        }
                        surface.Present(left, top, width, height); visible = true;
                        Interlocked.Increment(ref frames);
                    }
                    else if (visible) { surface.Hide(); visible = false; }
                    double elapsed = clock.Elapsed.TotalMilliseconds - frameStart;
                    // Poll at the selected rate even when stationary: native cursor remains responsive.
                    wake.WaitOne(Math.Max(1, (int)(1000.0 / settings.Rate - elapsed)));
                }
                catch (Exception exception)
                {
                    Volatile.Write(ref details, exception.ToString());
                    Volatile.Write(ref error, exception.Message);
                    Stop(); cursors.Restore(); surface.Hide(); applied = null; active = false;
                }
            }
        }
        catch (Exception exception) { Volatile.Write(ref details, exception.ToString()); Volatile.Write(ref error, exception.Message); active = false; }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        closing = true; wake.Set();
        if (thread.Join(4000)) wake.Dispose();
    }
}
