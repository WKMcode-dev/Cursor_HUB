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
    public static string? Pick(nint owner)
    {
        var of = new OpenFile { Size = Marshal.SizeOf<OpenFile>(), Owner = owner, File = new StringBuilder(32768), MaxFile = 32768, Filter = "Imagens PNG ou SVG\0*.png;*.svg\0\0", Title = "Importar cursor ou rastro", Flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008 };
        return GetOpenFileName(ref of) ? of.File.ToString() : null;
    }
}
