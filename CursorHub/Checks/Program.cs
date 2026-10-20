using System.Numerics;
using CursorHub.Core;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}

var trail = new TrailBuffer(16);
trail.Advance(Vector2.Zero, 0, 180, 4);
trail.Advance(new Vector2(40, 0), 10, 180, 4);
Check(trail.Count == 11, "preenche movimentos rapidos");
for (int i = 1; i < trail.Count; i++)
    Check(Vector2.Distance(trail[i - 1].Position, trail[i].Position) <= 4.01, "espacamento limitado");
trail.Advance(new Vector2(40, 0), 300, 180, 4);
Check(trail.Count == 0, "expira sem movimento");
trail.Clear();
trail.Advance(Vector2.Zero, 0, 180, 1);
trail.Advance(new Vector2(1000, 0), 10, 180, 1);
Check(trail.Count == 16 && trail[15].Position.X == 1000, "limite fixo e ponta recente");
trail.Advance(new Vector2(5000, 0), 20, 180, 1);
Check(trail.Count == 1, "teleporte nao desenha travessia");
trail.Advance(null, 30, 180, 1);
trail.Advance(new Vector2(10, 10), 40, 180, 1);
Check(trail.Count == 2, "reentrada nao liga posicoes distantes");
trail.Clear();
Check(trail.Count == 0, "limpa estado");

var pixels = new uint[64 * 64];
CursorHub.Windows.LayeredSurface.Circle(pixels, 64, 64, 64, 32, 32, 8, 255, 100, 0, .5f);
Check(pixels[0] == 0, "fundo realmente transparente");
uint center = pixels[32 * 64 + 32];
Check((center >> 24) is >= 126 and <= 128, "alfa por pixel");
Check(((center >> 16) & 255) <= (center >> 24), "cor pre-multiplicada");

// Compare the optimized rasterizer to the original source-over formula,
// including fractional coordinates, clipped edges, and overlapping particles.
static void ReferenceCircle(Span<uint> buffer, int stride, int width, int height, float x, float y, float radius, byte r, byte g, byte b, float alpha)
{
    for (int py = 0; py < height; py++)
    for (int px = 0; px < width; px++)
    {
        float dx = px + .5f - x, dy = py + .5f - y;
        float coverage = Math.Clamp(radius + .5f - MathF.Sqrt(dx * dx + dy * dy), 0, 1);
        int a = (int)(Math.Clamp(alpha, 0, 1) * coverage * 255);
        if (a == 0) continue;
        int index = py * stride + px; uint dst = buffer[index]; int inv = 255 - a;
        uint blue = (uint)((b * a + (dst & 255) * inv) / 255);
        uint green = (uint)((g * a + ((dst >> 8) & 255) * inv) / 255);
        uint red = (uint)((r * a + ((dst >> 16) & 255) * inv) / 255);
        uint outAlpha = (uint)(a + ((dst >> 24) * inv) / 255);
        buffer[index] = blue | (green << 8) | (red << 16) | (outAlpha << 24);
    }
}
var expectedPixels = new uint[64 * 64];
var actualPixels = new uint[64 * 64];
var random = new Random(741);
for (int i = 0; i < 120; i++)
{
    float x = (float)random.NextDouble() * 100 - 20, y = (float)random.NextDouble() * 100 - 20;
    float radius = .2f + (float)random.NextDouble() * 24;
    float alpha = (float)random.NextDouble();
    ReferenceCircle(expectedPixels, 64, 64, 64, x, y, radius, 93, 211, 241, alpha);
    CursorHub.Windows.LayeredSurface.Circle(actualPixels, 64, 64, 64, x, y, radius, 93, 211, 241, alpha);
}
Check(expectedPixels.SequenceEqual(actualPixels), "rasterizador otimizado preserva cada pixel e sobreposicao");

string tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "CursorHub-check-" + Guid.NewGuid().ToString("N"));
try
{
    string file = Path.Combine(tempDirectory, "presets.json");
    var store = new CursorHub.Interface.PresetStore(file);
    var preset = new CursorHub.Interface.CursorPreset(Guid.NewGuid(), "Neon", new());
    store.Put(preset);
    Check(new CursorHub.Interface.PresetStore(file).Items.Single().Name == "Neon", "preset persiste");
    store.Put(preset with { Name = "Neon 2" });
    Check(store.Items.Count == 1 && new CursorHub.Interface.PresetStore(file).Items.Single().Name == "Neon 2", "edicao preserva identidade");
    store.Remove(preset.Id);
    Check(new CursorHub.Interface.PresetStore(file).Items.Count == 0, "exclusao persiste");
    File.WriteAllText(file, "{invalid");
    Check(new CursorHub.Interface.PresetStore(file).Items.Count == 0, "arquivo invalido nao derruba app");
}
finally
{
    if (Directory.Exists(tempDirectory))
        foreach (var file in Directory.GetFiles(tempDirectory)) File.Delete(file);
    if (Directory.Exists(tempDirectory)) Directory.Delete(tempDirectory);
}
