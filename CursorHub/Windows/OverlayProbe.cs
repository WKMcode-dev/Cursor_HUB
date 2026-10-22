using System.Runtime.InteropServices;
using System.Text.Json;

namespace CursorHub.Windows;

// Integration probe uses its own plain windows. It never reads another app's content.
internal static class OverlayProbe
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint CreateWindowEx(uint ex, string cls, string title, uint style, int x, int y, int w, int h, nint parent, nint menu, nint instance, nint parameter);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint window);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint window, int command);
    [DllImport("user32.dll")] private static extern nint GetDC(nint window);
    [DllImport("user32.dll")] private static extern int ReleaseDC(nint window, nint dc);
    [DllImport("gdi32.dll")] private static extern uint GetPixel(nint dc, int x, int y);
    [DllImport("gdi32.dll")] private static extern nint CreateSolidBrush(uint colour);
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern int FillRect(nint dc, ref Rect area, nint brush);
    [DllImport("dwmapi.dll")] private static extern int DwmFlush();
    [DllImport("user32.dll")] private static extern nint SetThreadDpiAwarenessContext(nint context);

    public static void Run(string directory)
    {
        Directory.CreateDirectory(directory);
        var results = new List<string>();
        nint first = 0, second = 0;
        try
        {
            SetThreadDpiAwarenessContext(-4);
            nint CreateTarget()
            {
                nint target = CreateWindowEx(0x08000080, "STATIC", "CursorHub overlay probe", 0x80000000, 400, 300, 240, 180, 0, 0, 0, 0);
                if (target == 0) throw new Exception("Nao foi possivel criar a janela de verificacao.");
                ShowWindow(target, 4);
                var area = new Rect { Right = 240, Bottom = 180 };
                nint dc = GetDC(target), brush = CreateSolidBrush(0x00FFFFFF);
                FillRect(dc, ref area, brush); LayeredSurface.DeleteObject(brush); ReleaseDC(target, dc);
                return target;
            }
            void CheckComposed(string label)
            {
                LayeredSurface.Pump(); DwmFlush(); Thread.Sleep(100);
                nint dc = GetDC(0);
                uint actual = GetPixel(dc, 520, 390); ReleaseDC(0, dc);
                results.Add($"{label}: pixel RGB #{actual & 255:X2}{(actual >> 8) & 255:X2}{(actual >> 16) & 255:X2}");
                if ((actual & 0xFFFFFF) != 0x00B400FF) throw new Exception("A camada nao apareceu na composicao final acima da janela de verificacao.");
            }
            first = CreateTarget();
            using var overlay = new LayeredSurface(bufferWidth: 64, bufferHeight: 64);
            overlay.Pixels.Clear();
            LayeredSurface.Circle(overlay.Pixels, 64, 64, 64, 32, 32, 14, 255, 0, 180, 1);
            overlay.Present(488, 358, 64, 64);
            CheckComposed("Acima de janela comum");
            second = CreateTarget();
            overlay.Present(488, 358, 64, 64);
            CheckComposed("Acima de uma segunda janela aberta depois");
            results.Add("PASS composicao final sobre janelas independentes");
            overlay.Hide();
            using var effects = new DesktopEffects(() => new LayeredSurface.Point(560, 420));
            effects.Play(new EffectOptions(36, 600, 60, 255, 0, 180, false));
            var timeout = System.Diagnostics.Stopwatch.StartNew();
            while (effects.PresentedFrames == 0 && timeout.ElapsedMilliseconds < 2000) { LayeredSurface.Pump(); Thread.Sleep(5); }
            if (effects.PresentedFrames == 0) throw new Exception("Worker nao apresentou rastro: " + effects.Details);
            DwmFlush();
            nint screen = GetDC(0); uint trail = GetPixel(screen, 560, 420); ReleaseDC(0, screen);
            results.Add($"Worker do rastro acima de outra janela: #{trail & 255:X2}{(trail >> 8) & 255:X2}{(trail >> 16) & 255:X2}");
            if ((trail & 255) < 200 || ((trail >> 8) & 255) > 230 || ((trail >> 16) & 255) < 80)
                throw new Exception("Rastro nao foi encontrado na composicao final.");
            for(int i=0;i<20;i++){effects.Stop();effects.Play(new EffectOptions(36,600,60,255,0,180,false));}
            timeout.Restart();
            while(effects.Playback.Snapshot.Phase!=Core.PlaybackPhase.Playing && timeout.ElapsedMilliseconds<2000){LayeredSurface.Pump();Thread.Sleep(5);}
            if(effects.Playback.Snapshot.Phase!=Core.PlaybackPhase.Playing)throw new Exception("Play apos cliques rapidos nao confirmou.");
            effects.Stop();timeout.Restart();
            while(effects.Playback.Snapshot.Phase!=Core.PlaybackPhase.Stopped && timeout.ElapsedMilliseconds<2000){LayeredSurface.Pump();Thread.Sleep(5);}
            if(effects.Playback.Snapshot.Phase!=Core.PlaybackPhase.Stopped || effects.Active)throw new Exception("Stop final nao confirmou.");
            results.Add("PASS 20 ciclos rapidos de Play/Stop no compositor real");
            using var grown=new LayeredSurface();grown.EnsureCapacity(1400,1100);grown.Pixels.Clear();
            LayeredSurface.Circle(grown.Pixels,grown.Stride,1400,1100,1380,1080,8,255,0,180,1);
            if((grown.Pixels[1080*grown.Stride+1380]>>24)==0)throw new Exception("Ponta do rastro cortada apos 1024 px.");
            results.Add("PASS superficie expandida para rastro maior que 1024 px");
            results.Add("PASS worker real do rastro acima de outra janela");
        }
        catch (Exception error) { results.Add("FAIL " + error); }
        finally
        {
            if (second != 0) DestroyWindow(second);
            if (first != 0) DestroyWindow(first);
            File.WriteAllText(Path.Combine(directory, "overlay-results.json"), JsonSerializer.Serialize(results));
        }
    }
}
