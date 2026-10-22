using SkiaSharp;
using Svg.Skia;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;

namespace CursorHub.Windows;
internal sealed partial record RasterAsset
{
    public static string Import(string source)
    {
        var info = new FileInfo(source);
        if (info.Length > 8 * 1024 * 1024) throw new InvalidOperationException("Use uma imagem de ate 8 MB.");
        using var bitmap = new SKBitmap(256, 256, SKColorType.Bgra8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.Transparent);
        if (info.Extension.Equals(".svg", StringComparison.OrdinalIgnoreCase))
        {
            if (info.Length > 1024 * 1024) throw new InvalidOperationException("Use um SVG de ate 1 MB.");
            using var reader = XmlReader.Create(source, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024 });
            var document = XDocument.Load(reader);
            foreach (var element in document.Descendants())
            {
                if (element.Name.LocalName is "script" or "image" or "foreignObject" or "animate") throw new InvalidOperationException("Use SVG estatico com formas e caminhos.");
                foreach (var a in element.Attributes())
                    if (a.Name.LocalName.Equals("href", StringComparison.OrdinalIgnoreCase) && !a.Value.StartsWith('#') || a.Value.Contains("://") && a.Name.LocalName != "xmlns")
                        throw new InvalidOperationException("O SVG deve usar somente recursos internos.");
            }
            using var svg = new SKSvg(); svg.Load(source);
            var picture = svg.Picture ?? throw new InvalidOperationException("SVG invalido.");
            var bounds = picture.CullRect;
            if (bounds.Width <= 0 || bounds.Height <= 0) throw new InvalidOperationException("SVG sem dimensoes validas.");
            float scale = Math.Min(256 / bounds.Width, 256 / bounds.Height);
            canvas.Translate((256 - bounds.Width * scale) / 2, (256 - bounds.Height * scale) / 2);
            canvas.Scale(scale); canvas.Translate(-bounds.Left, -bounds.Top); canvas.DrawPicture(picture);
        }
        else if (info.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase))
        {
            using var codec = SKCodec.Create(source) ?? throw new InvalidOperationException("PNG invalido.");
            if (codec.Info.Width > 4096 || codec.Info.Height > 4096) throw new InvalidOperationException("Use uma imagem de ate 4096x4096.");
            using var image = SKBitmap.Decode(source) ?? throw new InvalidOperationException("PNG invalido.");
            float scale = Math.Min(256f / image.Width, 256f / image.Height);
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            canvas.DrawBitmap(image, SKRect.Create((256 - image.Width * scale) / 2, (256 - image.Height * scale) / 2, image.Width * scale, image.Height * scale), paint);
        }
        else throw new InvalidOperationException("Escolha PNG ou SVG.");
        canvas.Flush(); using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        var bytes = encoded.ToArray();
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CursorHub", "Images"); Directory.CreateDirectory(directory);
        string target = Path.Combine(directory, Convert.ToHexString(SHA256.HashData(bytes)) + ".png");
        if (!File.Exists(target)) File.WriteAllBytes(target, bytes);
        return target;
    }
    // Transform from the editor's original on every change, avoiding cumulative resampling.
    public static string Transform(string source, int degrees, bool flipX, bool flipY)
    {
        using var image=SKBitmap.Decode(source) ?? throw new InvalidOperationException("Imagem indisponivel.");
        using var bitmap=new SKBitmap(256,256,SKColorType.Bgra8888,SKAlphaType.Premul);
        using var canvas=new SKCanvas(bitmap);canvas.Clear(SKColors.Transparent);
        canvas.Translate(128,128);canvas.RotateDegrees(degrees);canvas.Scale(flipX?-1:1,flipY?-1:1);
        // Fit the rotated square so no image pixels are cut off.
        float radians=degrees*MathF.PI/180;
        float fit=1/(MathF.Abs(MathF.Cos(radians))+MathF.Abs(MathF.Sin(radians)));
        canvas.Scale(fit,fit);
        using var paint=new SKPaint {IsAntialias=true,FilterQuality=SKFilterQuality.High};
        canvas.DrawBitmap(image,SKRect.Create(-128,-128,256,256),paint);canvas.Flush();
        using var encoded=bitmap.Encode(SKEncodedImageFormat.Png,100);var bytes=encoded.ToArray();
        string directory=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"CursorHub","Images");Directory.CreateDirectory(directory);
        string target=Path.Combine(directory,Convert.ToHexString(SHA256.HashData(bytes))+".png");
        if(!File.Exists(target))File.WriteAllBytes(target,bytes);return target;
    }
    public static RasterAsset Load(string path)
    {
        using var image = SKBitmap.Decode(path) ?? throw new InvalidOperationException("Imagem nao encontrada.");
        using var bitmap = new SKBitmap(64, 64, SKColorType.Bgra8888, SKAlphaType.Premul);
        using (var c = new SKCanvas(bitmap)) { c.Clear(SKColors.Transparent); c.DrawBitmap(image, SKRect.Create(64,64)); }
        var raw = new int[4096]; Marshal.Copy(bitmap.GetPixels(), raw, 0, raw.Length);
        return new(64, 64, Array.ConvertAll(raw, x => unchecked((uint)x)));
    }
}
