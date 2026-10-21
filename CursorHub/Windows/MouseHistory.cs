using System.Numerics;
using System.Runtime.InteropServices;

namespace CursorHub.Windows;

internal readonly record struct MotionPoint(Vector2 Position, double Time);

// Recover actual Windows pointer positions between rendered frames; never invent a curve.
internal sealed class MouseHistory
{
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; public uint Time; public nuint Extra; }
    [DllImport("user32.dll", SetLastError = true)]
    private static extern int GetMouseMovePointsEx(uint size, ref NativePoint point, [Out] NativePoint[] history, int count, uint resolution);
    private readonly NativePoint[] native = new NativePoint[64];
    private uint previousTick;
    private double previousTime;
    private bool initialized;
    public void Reset() => initialized = false;
    internal static int Decode(int coordinate) => coordinate > 32767 ? coordinate - 65536 : coordinate;
    public int Read(int x, int y, double now, Span<MotionPoint> output)
    {
        uint tick = unchecked((uint)Environment.TickCount);
        if (!initialized) { initialized = true; previousTick = tick; previousTime = now; return 0; }
        var query = new NativePoint { X = x & 0xffff, Y = y & 0xffff };
        int count = GetMouseMovePointsEx((uint)Marshal.SizeOf<NativePoint>(), ref query, native, native.Length, 1);
        int used = 0;
        for (int i = count - 1; i >= 0 && used < output.Length; i--)
        {
            var p = native[i];
            if (unchecked((int)(p.Time - previousTick)) <= 0 || unchecked((int)(tick - p.Time)) < 0) continue;
            double born = Math.Clamp(now - unchecked((uint)(tick - p.Time)), previousTime, now);
            output[used++] = new(new Vector2(Decode(p.X), Decode(p.Y)), born);
        }
        previousTick = tick; previousTime = now;
        return used;
    }
}
