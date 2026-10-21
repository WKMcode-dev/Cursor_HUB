using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace CursorHub.Windows;

// A separate guard owns copies of the original cursor handles before any replacement.
// It restores them on Stop, normal exit, or unexpected death of the main process.
internal sealed unsafe class CursorController : IDisposable
{
    private static readonly uint[] Roles = { 32512, 32513, 32514, 32515, 32516, 32642, 32643, 32644, 32645, 32646, 32648, 32649, 32650, 32651 };
    [StructLayout(LayoutKind.Sequential)] private struct IconInfo { public int IsIcon; public uint HotX, HotY; public nint Mask, Colour; }
    [StructLayout(LayoutKind.Sequential)] private struct BitmapInfo
    {
        public uint Size; public int Width, Height; public ushort Planes, Bits;
        public uint Compression, ImageSize; public int X, Y; public uint Used, Important;
    }
    [DllImport("user32.dll")] private static extern nint LoadCursor(nint instance, nint name);
    [DllImport("user32.dll")] private static extern nint CopyImage(nint image, uint type, int x, int y, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetSystemCursor(nint cursor, uint role);
    [DllImport("user32.dll", SetLastError = true)] private static extern nint CreateIconIndirect(ref IconInfo info);
    [DllImport("user32.dll")] private static extern bool DestroyCursor(nint cursor);
    [DllImport("gdi32.dll")] private static extern nint CreateBitmap(int width, int height, uint planes, uint bits, nint data);
    [DllImport("gdi32.dll")] private static extern nint CreateDIBSection(nint dc, ref BitmapInfo info, uint use, out nint bits, nint section, uint offset);

    private Process? guard;

    private void EnsureGuard()
    {
        if (guard is { HasExited: false }) return;
        var start = new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = AppContext.BaseDirectory, RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true };
        if (string.Equals(Path.GetFileNameWithoutExtension(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
            start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
        start.ArgumentList.Add("--cursor-guard"); start.ArgumentList.Add(Environment.ProcessId.ToString()); start.ArgumentList.Add("pipe");
        guard = Process.Start(start) ?? throw new InvalidOperationException("Nao foi possivel iniciar a restauracao do cursor.");
        var ready = guard.StandardOutput.ReadLineAsync();
        if (!ready.Wait(3000) || ready.Result != "READY")
        {
            Restore();
            throw new InvalidOperationException("Protecao do cursor indisponivel. Play cancelado.");
        }
    }

    public void Apply(EffectOptions options)
    {
        EnsureGuard();
        const int extent = 64;
        var info = new BitmapInfo { Size = (uint)Marshal.SizeOf<BitmapInfo>(), Width = extent, Height = -extent, Planes = 1, Bits = 32 };
        nint bitmap = CreateDIBSection(0, ref info, 0, out var bits, 0, 0);
        byte* maskBits = stackalloc byte[extent * extent / 8];
        new Span<byte>(maskBits, extent * extent / 8).Fill(255);
        nint mask = CreateBitmap(extent, extent, 1, 1, (nint)maskBits);
        nint cursor = 0;
        try
        {
            if (bitmap == 0 || mask == 0 || bits == 0) throw new Win32Exception();
            var pixels = new Span<uint>((void*)bits, extent * extent); pixels.Clear();
            uint hx=32,hy=32;
            if(options.CursorArt is {} art)
            {
                LayeredSurface.Stamp(pixels,extent,extent,extent,options.Size/2f,options.Size/2f,options.Size,art,1);
                hx=(uint)Math.Clamp((int)Math.Round(options.HotX*(options.Size-1)),0,options.Size-1);
                hy=(uint)Math.Clamp((int)Math.Round(options.HotY*(options.Size-1)),0,options.Size-1);
            }
            else
            {
                LayeredSurface.Circle(pixels,extent,extent,extent,32,32,options.Size/2f,255,255,255,1);
                LayeredSurface.Circle(pixels,extent,extent,extent,32,32,Math.Max(1,options.Size/2f-1),options.R,options.G,options.B,1);
            }
            var icon = new IconInfo { HotX = hx, HotY = hy, Colour = bitmap, Mask = mask };
            cursor = CreateIconIndirect(ref icon);
            if (cursor == 0) throw new Win32Exception();
            foreach (uint role in Roles)
            {
                nint copy = CopyImage(cursor, 2, 0, 0, 0);
                if (copy == 0) throw new Win32Exception();
                if (!SetSystemCursor(copy, role)) { DestroyCursor(copy); throw new Win32Exception(); }
            }
        }
        catch { Restore(); throw; }
        finally
        {
            if (cursor != 0) DestroyCursor(cursor);
            if (bitmap != 0) LayeredSurface.DeleteObject(bitmap);
            if (mask != 0) LayeredSurface.DeleteObject(mask);
        }
    }

    public void Restore()
    {
        if (guard is { HasExited: false })
        {
            try { guard.StandardInput.WriteLine("STOP"); guard.StandardInput.Flush(); guard.StandardInput.Close(); }
            catch (IOException) { }
            guard.WaitForExit(3000);
        }
        guard?.Dispose(); guard = null;
    }
    public void Dispose() => Restore();

    public static void RunGuard(int parentId, string name)
    {
        var saved = new List<(uint Role, nint Handle)>();
        try
        {
            foreach (uint role in Roles)
            {
                nint original = CopyImage(LoadCursor(0, (nint)role), 2, 0, 0, 0);
                if (original == 0) throw new Win32Exception();
                saved.Add((role, original));
            }
            Console.WriteLine("READY"); Console.Out.Flush();
            // EOF also arrives if the main process is killed. No global named objects required.
            Console.ReadLine();
        }
        finally
        {
            foreach (var original in saved)
                if (!SetSystemCursor(original.Handle, original.Role)) DestroyCursor(original.Handle);
        }
    }
}
