using System.Runtime.InteropServices;
using System.Text;
namespace CursorHub.Windows;
internal static class AssetPicker
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct OpenFile
    {
        public int Size; public nint Owner, Instance; public string Filter; public nint Custom; public int MaxCustom, FilterIndex;
        public StringBuilder File; public int MaxFile; public nint FileTitle; public int MaxFileTitle; public string? Initial, Title;
        public uint Flags; public ushort Offset, Extension; public string? DefaultExtension; public nint Data, Hook; public string? Template; public nint Reserved; public uint Reserved2, FlagsEx;
    }
    [DllImport("comdlg32.dll", CharSet = CharSet.Unicode)] private static extern bool GetOpenFileName(ref OpenFile file);
    [DllImport("comdlg32.dll")] private static extern uint CommDlgExtendedError();
    [DllImport("ole32.dll")] private static extern int OleInitialize(nint reserved);
    [DllImport("ole32.dll")] private static extern void OleUninitialize();
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();

    public static Task<string?> PickAsync(nint owner, Action<uint>? ready = null)
    {
        var completion=new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread=new Thread(()=>
        {
            bool initialized=false;
            try
            {
                Marshal.ThrowExceptionForHR(OleInitialize(0));initialized=true;
                ready?.Invoke(GetCurrentThreadId());
                completion.SetResult(Pick(owner));
            }
            catch(Exception e){completion.SetException(e);}
            finally{if(initialized)OleUninitialize();}
        }){IsBackground=true,Name="CursorHub file picker (STA)"};
        thread.SetApartmentState(ApartmentState.STA);thread.Start();
        return completion.Task;
    }
    private static string? Pick(nint owner)
    {
        var of = new OpenFile { Size = Marshal.SizeOf<OpenFile>(), Owner = owner, File = new StringBuilder(32768), MaxFile = 32768, Filter = "Imagens PNG ou SVG\0*.png;*.svg\0\0", Title = "Importar cursor ou rastro", Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008 };
        if(GetOpenFileName(ref of))return of.File.ToString();
        uint error=CommDlgExtendedError();
        if(error!=0)throw new InvalidOperationException($"Windows nao abriu o seletor de imagens (0x{error:X}).");
        return null; // Cancel is distinct from a native dialog failure.
    }
}
