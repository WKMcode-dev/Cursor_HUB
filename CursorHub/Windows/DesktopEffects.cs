using System.Diagnostics;
using System.Numerics;
using CursorHub.Core;
using System.Runtime.InteropServices;

namespace CursorHub.Windows;

internal sealed record EffectOptions(int Size, int Lifetime, int Rate, byte R, byte G, byte B, bool CustomizeCursor = true, RasterAsset? CursorArt = null, RasterAsset? TrailArt = null, float HotX = .5f, float HotY = .5f, bool History = true, int Particles = 512);

internal sealed class DesktopEffects : IDisposable
{
    [DllImport("user32.dll")] private static extern nint SetThreadDpiAwarenessContext(nint context);
    internal readonly PlaybackState Playback = new();
    private readonly object requestGate = new();
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
        if (!thread.IsAlive) { lock(requestGate){var revision=Playback.Request(true);Playback.Fail(revision);} Volatile.Write(ref error, error ?? "A camada global nao esta disponivel."); return; }
        lock(requestGate){Playback.Request(true);Volatile.Write(ref error,null);Volatile.Write(ref warning,null);Volatile.Write(ref requested,options);}wake.Set();
    }
    public void SetRate(int rate)
    {
        var current = Volatile.Read(ref requested);
        if (current is not null) Interlocked.CompareExchange(ref requested, current with { Rate = Math.Clamp(rate, 30, 360) }, current);
        wake.Set();
    }
    public void Stop() { lock(requestGate){Playback.Request(false);Volatile.Write(ref requested,null);} wake.Set(); }

    private void Run()
    {
        try
        {
            SetThreadDpiAwarenessContext(-4); // Physical screen coordinates, including mixed-DPI displays.
            using var surface = new LayeredSurface();
            using var cursors = new CursorController();
            using var pacer = new FramePacer();
            var trail = new TrailBuffer(512);
            var history = new MouseHistory();
            var moves = new MotionPoint[64];
            var clock = Stopwatch.StartNew();
            EffectOptions? applied = null;
            LayeredSurface.Point? last = null;
            bool visible = false, chordHeld = false;
            while (!closing)
            {
                LayeredSurface.Pump();
                EffectOptions? settings;long revision;
                lock(requestGate){settings=Volatile.Read(ref requested);revision=Playback.Snapshot.Revision;}
                bool chord = LayeredSurface.GetAsyncKeyState(0x11) < 0 && LayeredSurface.GetAsyncKeyState(0x10) < 0 && LayeredSurface.GetAsyncKeyState(0x58) < 0;
                if (chord && !chordHeld) { Stop(); settings = null; }
                chordHeld = chord;
                if (settings is null)
                {
                    if (applied is not null) { cursors.Restore(); surface.Hide(); trail.Clear(); history.Reset(); applied = null; last = null; visible = false; }
                    active = false;Playback.Complete(revision,false);
                    wake.WaitOne(60);
                    continue;
                }
                try
                {
                    if (applied != settings)
                    {
                        if(trail.Capacity!=settings.Particles) { trail=new TrailBuffer(Math.Clamp(settings.Particles,256,1024)); history.Reset(); }
                        bool changedCursor = applied is null || applied.Size != settings.Size || applied.R != settings.R || applied.G != settings.G || applied.B != settings.B || applied.CustomizeCursor != settings.CustomizeCursor || applied.CursorArt != settings.CursorArt || applied.HotX != settings.HotX || applied.HotY != settings.HotY;
                        if (changedCursor && settings.CustomizeCursor)
                        {
                            try { cursors.Apply(settings); }
                            catch (Exception exception)
                            {
                                // A denied cursor scheme change must not disable the desktop trail.
                                cursors.Restore();
                                Volatile.Write(ref warning, "Rastro ativo; Windows manteve o cursor original: " + exception.Message);
                            }
                        }
                        else if (changedCursor) cursors.Restore();
                        applied = settings;
                    }
                    active = true;Playback.Complete(revision,true);
                    double frameStart = clock.Elapsed.TotalMilliseconds;
                    LayeredSurface.Point point;
                    if (probePosition is not null) point = probePosition();
                    else if (!LayeredSurface.GetCursorPos(out point)) { surface.Hide(); wake.WaitOne(30); continue; }
                    bool moved = last is null || point.X != last.Value.X || point.Y != last.Value.Y;
                    last = point;
                    float spacing = Math.Max(1, settings.Size / 8f);
                    if (probePosition is null && settings.History)
                    {
                        int count = history.Read(point.X, point.Y, frameStart, moves);
                        for (int i = 0; i < count; i++) trail.Advance(moves[i].Position, moves[i].Time, settings.Lifetime, spacing);
                    }
                    trail.Advance(new Vector2(point.X, point.Y), frameStart, settings.Lifetime, spacing);
                    if (moved || trail.Count > 0)
                    {
                        int padding = settings.Size + 3;
                        int left = point.X - padding, top = point.Y - padding, right = point.X + padding, bottom = point.Y + padding;
                        // Cover the complete retained trajectory; grow only when needed.
                        for (int i = 0; i < trail.Count; i++)
                        {
                            var p = trail[i].Position;
                            
                            left = Math.Min(left, (int)p.X - padding); right = Math.Max(right, (int)p.X + padding);
                            top = Math.Min(top, (int)p.Y - padding); bottom = Math.Max(bottom, (int)p.Y + padding);
                        }
                        int width = right-left+1, height=bottom-top+1;
                        surface.EnsureCapacity(width,height);
                        var pixels = surface.Pixels;
                        for (int row = 0; row < height; row++) pixels.Slice(row * surface.Stride, width).Clear();
                        for (int i = 0; i < trail.Count; i++)
                        {
                            var sample = trail[i];
                            float remaining = Math.Clamp(1 - (float)((frameStart - sample.Born) / settings.Lifetime), 0, 1);
                            if(settings.TrailArt is {} art) LayeredSurface.Stamp(pixels, surface.Stride,width,height,sample.Position.X-left,sample.Position.Y-top,settings.Size*(.25f+remaining*.65f),art,remaining*.65f*Math.Min(1,(i+1)/24f));
                            else LayeredSurface.Circle(pixels, surface.Stride, width, height,
                                sample.Position.X - left, sample.Position.Y - top, settings.Size * (.125f + remaining * .325f), settings.R, settings.G, settings.B, remaining * .65f * Math.Min(1,(i+1)/24f));
                        }
                        surface.Present(left, top, width, height); visible = true;
                        Interlocked.Increment(ref frames);
                    }
                    else if (visible) { surface.Hide(); visible = false; }
                    double elapsed = clock.Elapsed.TotalMilliseconds - frameStart;
                    // Poll at the selected rate even when stationary: native cursor remains responsive.
                    pacer.Wait(1000.0 / Math.Clamp(settings.Rate, 30, 360) - elapsed, wake);
                }
                catch (Exception exception)
                {
                    Volatile.Write(ref details, exception.ToString());
                    Volatile.Write(ref error, exception.Message);
                    lock(requestGate){if(Playback.Snapshot.Revision==revision){Volatile.Write(ref requested,null);Playback.Fail(revision);}}
                    cursors.Restore(); surface.Hide(); applied = null; active = false;
                }
            }
        }
        catch (Exception exception) { Volatile.Write(ref details, exception.ToString()); Volatile.Write(ref error, exception.Message); active = false;Playback.Fail(Playback.Snapshot.Revision); }
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        closing = true; wake.Set();
        if (thread.Join(4000)) wake.Dispose();
    }
}
