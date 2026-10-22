using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
namespace CursorHub.Windows;
// Only observes and cancels dialogs created by this probe's own picker thread.
internal static class PickerProbe
{
    private delegate bool EnumWindow(nint window,nint parameter);
    [DllImport("user32.dll")] private static extern bool EnumThreadWindows(uint thread,EnumWindow callback,nint parameter);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)] private static extern int GetClassName(nint window,StringBuilder text,int length);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool PostMessage(nint window,uint message,nint wParam,nint lParam);
    public static void Run(string directory)
    {
        Directory.CreateDirectory(directory);var results=new List<string>();
        try
        {
            foreach(bool fromWorker in new[]{false,true})
            {
                uint pickerThread=0;
                Task<string?> Open()=>AssetPicker.PickAsync(0,id=>Volatile.Write(ref pickerThread,id));
                var pending=fromWorker?Task.Run(Open):Open();
                var watch=Stopwatch.StartNew();nint dialog=0;
                while(dialog==0 && watch.ElapsedMilliseconds<10000 && !pending.IsCompleted)
                {
                    uint id=Volatile.Read(ref pickerThread);
                    if(id!=0)EnumThreadWindows(id,(window,_)=>
                    {
                        var cls=new StringBuilder(64);GetClassName(window,cls,64);
                        if(cls.ToString()=="#32770" && IsWindowVisible(window)){dialog=window;return false;}
                        return true;
                    },0);
                    if(dialog==0)Thread.Sleep(25);
                }
                if(dialog==0){if(pending.IsFaulted)pending.GetAwaiter().GetResult();throw new Exception("Seletor nao ficou visivel.");}
                PostMessage(dialog,0x0010,0,0);
                if(pending.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult() is not null)throw new Exception("Cancelar retornou arquivo.");
                results.Add("PASS seletor visivel e cancelamento limpo a partir de "+(fromWorker?"thread MTA":"thread STA"));
            }
            foreach(bool cancel in new[]{true,false})
            {
                uint pickerThread=0;
                var pending=ColourPicker.PickAsync(0,0x12ABEF,id=>Volatile.Write(ref pickerThread,id));
                var watch=Stopwatch.StartNew();nint dialog=0;
                while(dialog==0&&watch.ElapsedMilliseconds<10000&&!pending.IsCompleted)
                {
                    uint id=Volatile.Read(ref pickerThread);
                    if(id!=0)EnumThreadWindows(id,(window,_)=>{var cls=new StringBuilder(64);GetClassName(window,cls,64);if(cls.ToString()=="#32770"&&IsWindowVisible(window)){dialog=window;return false;}return true;},0);
                    if(dialog==0)Thread.Sleep(25);
                }
                if(dialog==0){if(pending.IsFaulted)pending.GetAwaiter().GetResult();throw new Exception("Seletor de cores nao ficou visivel.");}
                PostMessage(dialog,0x0111,cancel?2:1,0);
                var result=pending.WaitAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                if(cancel?result is not null:result!=0x12ABEF)throw new Exception("Seletor alterou ou perdeu o RGB.");
                results.Add("PASS seletor de cores visivel: "+(cancel?"cancelamento":"confirmacao RGB exata"));
            }

        }
        catch(Exception e){results.Add("FAIL "+e);}
        File.WriteAllText(Path.Combine(directory,"picker-results.json"),JsonSerializer.Serialize(results));
    }
}
