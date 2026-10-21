using System.Runtime.InteropServices;

namespace CursorHub.Windows;

// High-resolution waits when supported, without busy-waiting or changing system timer resolution.
internal sealed class FramePacer : IDisposable
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint CreateWaitableTimerEx(nint attributes, string? name, uint flags, uint access);
    [DllImport("kernel32.dll")] private static extern bool SetWaitableTimer(nint timer, ref long due, int period, nint callback, nint context, bool resume);
    [DllImport("kernel32.dll")] private static extern uint WaitForMultipleObjects(uint count, nint[] handles, bool all, uint timeout);
    [DllImport("kernel32.dll")] private static extern bool CloseHandle(nint handle);
    private readonly nint timer = CreateWaitableTimerEx(0, null, 2, 0x001F0003);
    private readonly nint[] handles = new nint[2];
    public void Wait(double milliseconds, AutoResetEvent wake)
    {
        if (milliseconds <= 0) return;
        long due = -(long)Math.Ceiling(Math.Min(milliseconds, 1000) * 10000);
        if (timer == 0 || !SetWaitableTimer(timer, ref due, 0, 0, 0, false)) { wake.WaitOne(Math.Max(1, (int)Math.Ceiling(milliseconds))); return; }
        handles[0] = timer; handles[1] = wake.SafeWaitHandle.DangerousGetHandle();
        WaitForMultipleObjects(2, handles, false, 1100);
    }
    public void Dispose() { if (timer != 0) CloseHandle(timer); }
}
