using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace CursorHub.Windows;

// The native region and displayed PNGs share the same alpha source.
// Include only fully covered pixels: partially covered pixels would blend with the opaque swapchain.
internal sealed class ChromeMask
{
    private sealed record Layer(int X, int Y, int Width, int Height, int PixelWidth, int PixelHeight, uint[] Pixels);
    private readonly Layer[] layers;
    public ChromeMask(string directory)
    {
        Layer Load(string file, int x, int y, int width, int height)
        {
            using var image = Image.Load<Rgba32>(Path.Combine(directory, file));
            var pixels = new uint[image.Width * image.Height];
            image.ProcessPixelRows(rows =>
            {
                for (int py = 0; py < image.Height; py++)
                {
                    var row = rows.GetRowSpan(py);
                    for (int px = 0; px < image.Width; px++)
                    {
                        var p = row[px]; uint a = p.A;
                        pixels[py * image.Width + px] = p.B * a / 255 | ((p.G * a / 255) << 8) | ((p.R * a / 255) << 16) | (a << 24);
                    }
                }
            });
            return new Layer(x, y, width, height, image.Width, image.Height, pixels);
        }
        layers = new[]
        {
            Load("frame.png", 0, 0, 800, 500),
            Load("close.png", 660, 35, 18, 18), Load("minimize.png", 704, 35, 18, 18),
            Load("maximize.png", 748, 35, 18, 18), Load("play.png", 650, 444, 136, 42)
        };
    }

    public bool Includes(int px, int py, int width, int height)
    {
        double x = (px + .5) * 800 / width, y = (py + .5) * 500 / height;
        foreach (var layer in layers)
        {
            if (x < layer.X || y < layer.Y || x >= layer.X + layer.Width || y >= layer.Y + layer.Height) continue;
            double u = (x - layer.X) * layer.PixelWidth / layer.Width - .5;
            double v = (y - layer.Y) * layer.PixelHeight / layer.Height - .5;
            int x0 = (int)Math.Floor(u), y0 = (int)Math.Floor(v);
            double fx = u - x0, fy = v - y0;
            byte A(int sx, int sy) => (byte)(layer.Pixels[Math.Clamp(sy, 0, layer.PixelHeight - 1) * layer.PixelWidth + Math.Clamp(sx, 0, layer.PixelWidth - 1)] >> 24);
            double upper = A(x0, y0) * (1 - fx) + A(x0 + 1, y0) * fx;
            double lower = A(x0, y0 + 1) * (1 - fx) + A(x0 + 1, y0 + 1) * fx;
            if (upper * (1 - fy) + lower * fy >= 254.99) return true;
        }
        return false;
    }
    // Premultiplied RGBA interpolation keeps the edge colour correct over any desktop background.
    internal uint Fringe(int px, int py, int width, int height)
    {
        if (Includes(px, py, width, height)) return 0;
        double x = (px + .5) * 800 / width, y = (py + .5) * 500 / height;
        foreach (var layer in layers)
        {
            if (x < layer.X || y < layer.Y || x >= layer.X + layer.Width || y >= layer.Y + layer.Height) continue;
            double u = (x - layer.X) * layer.PixelWidth / layer.Width - .5;
            double v = (y - layer.Y) * layer.PixelHeight / layer.Height - .5;
            int x0 = (int)Math.Floor(u), y0 = (int)Math.Floor(v);
            double fx = u - x0, fy = v - y0;
            uint P(int sx, int sy) => layer.Pixels[Math.Clamp(sy, 0, layer.PixelHeight - 1) * layer.PixelWidth + Math.Clamp(sx, 0, layer.PixelWidth - 1)];
            uint p00 = P(x0, y0), p10 = P(x0 + 1, y0), p01 = P(x0, y0 + 1), p11 = P(x0 + 1, y0 + 1);
            uint output = 0;
            for (int shift = 0; shift <= 24; shift += 8)
            {
                double a = ((p00 >> shift) & 255) * (1 - fx) + ((p10 >> shift) & 255) * fx;
                double b = ((p01 >> shift) & 255) * (1 - fx) + ((p11 >> shift) & 255) * fx;
                output |= (uint)Math.Clamp((int)Math.Round(a * (1 - fy) + b * fy), 0, 255) << shift;
            }
            if ((output >> 24) != 0) return output;
        }
        return 0;
    }

}
