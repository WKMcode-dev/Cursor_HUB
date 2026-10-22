using System.ComponentModel;
using System.Runtime.InteropServices;
using CursorHub.Core;
namespace CursorHub.Windows;
// Sends input only to this application's known window, using canonical interface coordinates.
internal static class UiInputProbe
{
    [StructLayout(LayoutKind.Sequential)]private struct Rect{public int Left,Top,Right,Bottom;}
    [StructLayout(LayoutKind.Sequential)]private struct Point{public int X,Y;}
    [StructLayout(LayoutKind.Explicit,Size=40)]private struct Input
    {
        [FieldOffset(0)]public uint Type;[FieldOffset(8)]public ushort Key;
        [FieldOffset(10)]public ushort Scan;[FieldOffset(12)]public uint Flags;
    }
    [DllImport("user32.dll")]private static extern bool GetClientRect(nint window,out Rect rect);
    [DllImport("user32.dll")]private static extern bool ClientToScreen(nint window,ref Point point);
    [DllImport("user32.dll")]private static extern bool SetForegroundWindow(nint window);
    [DllImport("user32.dll")]private static extern bool SetCursorPos(int x,int y);
    [DllImport("user32.dll")]private static extern void mouse_event(uint flags,uint dx,uint dy,uint data,nuint extra);
    [DllImport("user32.dll",SetLastError=true)]private static extern uint SendInput(uint count,Input[] input,int size);
    public static void Move(nint window,float x,float y)
    {
        if(window==0||!GetClientRect(window,out var rect))throw new InvalidOperationException("Janela de teste indisponivel.");
        var fit=InterfaceViewport.Fit(rect.Right,rect.Bottom);
        var point=new Point{X=(int)MathF.Round(fit.Left+x*fit.Scale),Y=(int)MathF.Round(fit.Top+y*fit.Scale)};
        if(!ClientToScreen(window,ref point))throw new InvalidOperationException("Coordenadas de teste invalidas.");
        SetForegroundWindow(window);SetCursorPos(point.X,point.Y);
    }
    public static void Press()=>mouse_event(2,0,0,0,0);
    public static void Release()=>mouse_event(4,0,0,0,0);
    public static void ReplaceText(string text)
    {
        var input=new List<Input>{new(){Type=1,Key=0x11},new(){Type=1,Key=0x41},new(){Type=1,Key=0x41,Flags=2},new(){Type=1,Key=0x11,Flags=2}};
        foreach(char c in text){input.Add(new Input{Type=1,Scan=c,Flags=4});input.Add(new Input{Type=1,Scan=c,Flags=6});}
        if(SendInput((uint)input.Count,input.ToArray(),Marshal.SizeOf<Input>())!=input.Count)throw new Win32Exception();
    }
}
