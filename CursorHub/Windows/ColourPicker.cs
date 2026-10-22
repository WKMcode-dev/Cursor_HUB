using System.Runtime.InteropServices;
using CursorHub.Core;
namespace CursorHub.Windows;
internal static class ColourPicker
{
    [StructLayout(LayoutKind.Sequential)] private struct ChooseColour
    {
        public uint Size;public nint Owner,Instance;public uint Result;public nint Custom;
        public uint Flags;public nint Data,Hook,Template;
    }
    [DllImport("comdlg32.dll",EntryPoint="ChooseColorW",ExactSpelling=true)]
    [return:MarshalAs(UnmanagedType.Bool)]private static extern bool ChooseColor(ref ChooseColour colour);
    [DllImport("comdlg32.dll")]private static extern uint CommDlgExtendedError();
    [DllImport("kernel32.dll")]private static extern uint GetCurrentThreadId();
    private static readonly int[] custom=new int[16];
    public static Task<uint?> PickAsync(nint owner,uint initial,Action<uint>? ready=null)
    {
        var completion=new TaskCompletionSource<uint?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>
        {
            nint colours=0;
            try
            {
                colours=Marshal.AllocHGlobal(16*sizeof(int));Marshal.Copy(custom,0,colours,16);
                var value=new ChooseColour{Size=(uint)Marshal.SizeOf<ChooseColour>(),Owner=owner,Result=ColourValue.ToColorRef(initial),Custom=colours,Flags=3};
                ready?.Invoke(GetCurrentThreadId());
                if(ChooseColor(ref value)){Marshal.Copy(colours,custom,0,16);completion.SetResult(ColourValue.FromColorRef(value.Result));}
                else{uint error=CommDlgExtendedError();if(error!=0)throw new InvalidOperationException($"Seletor de cores: 0x{error:X}.");completion.SetResult(null);}
            }
            catch(Exception e){completion.SetException(e);}
            finally{Marshal.FreeHGlobal(colours);}
        }){IsBackground=true,Name="CursorHub colour picker (STA)"};
        thread.SetApartmentState(ApartmentState.STA);thread.Start();return completion.Task;
    }
}
