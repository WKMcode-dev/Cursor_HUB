using System.IO.Compression;
using System.Security.Cryptography;
namespace CursorHub;
internal static class AssetPaths
{
    private static readonly Lazy<string> assets=new(Resolve);
    public static string Directory=>assets.Value;
    private static string Resolve()
    {
        using var bundle=typeof(AssetPaths).Assembly.GetManifestResourceStream("CursorHub.AssetBundle.zip");
        if(bundle is null)return Path.Combine(AppContext.BaseDirectory,"Assets");
        using var memory=new MemoryStream();bundle.CopyTo(memory);var bytes=memory.ToArray();
        string root=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CursorHub","RuntimeAssets",Convert.ToHexString(SHA256.HashData(bytes))[..20]);
        string ready=Path.Combine(root,"ready");
        if(!File.Exists(ready))
        {
            System.IO.Directory.CreateDirectory(root);
            using var input=new MemoryStream(bytes);using var zip=new ZipArchive(input);
            zip.ExtractToDirectory(root,true);
            File.WriteAllText(ready,"1");
        }
        return Path.Combine(root,"Assets");
    }
}
