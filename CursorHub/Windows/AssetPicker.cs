using System.Runtime.InteropServices;
namespace CursorHub.Windows;
internal static class AssetPicker
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct OpenFile
    {
        public int Size; public nint Owner, Instance; public nint Filter; public nint Custom; public int MaxCustom, FilterIndex;
        public nint File; public int MaxFile; public nint FileTitle; public int MaxFileTitle; public nint Initial, Title;
        public uint Flags; public ushort Offset, Extension; public nint DefaultExtension; public nint Data, Hook; public nint Template; public nint Reserved; public uint Reserved2, FlagsEx;
    }
    [DllImport("comdlg32.dll", EntryPoint="GetOpenFileNameW", ExactSpelling=true)]
    [return:MarshalAs(UnmanagedType.Bool)] private static extern bool GetOpenFileName(ref OpenFile file);
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
        const int capacity=32768;
        nint buffer=0,filter=0,title=0;
        try
        {
            // OPENFILENAME is a native pointer structure; StringBuilder cannot be a struct field.
            buffer=Marshal.AllocHGlobal(capacity*sizeof(char));Marshal.WriteInt16(buffer,0);
            filter=Marshal.StringToHGlobalUni("Imagens PNG ou SVG\0*.png;*.svg\0\0");
            title=Marshal.StringToHGlobalUni("Importar cursor ou rastro");
            var of=new OpenFile {Size=Marshal.SizeOf<OpenFile>(),Owner=owner,File=buffer,MaxFile=capacity,Filter=filter,FilterIndex=1,Title=title,Flags=0x00080000|0x00001000|0x00000800|0x00000008};
            if(GetOpenFileName(ref of))return Marshal.PtrToStringUni(buffer);
            uint error=CommDlgExtendedError();
            if(error!=0)throw new InvalidOperationException($"Windows nao abriu o seletor de imagens (0x{error:X}).");
            return null;
        }
        finally{Marshal.FreeHGlobal(buffer);Marshal.FreeHGlobal(filter);Marshal.FreeHGlobal(title);}
    }
}
