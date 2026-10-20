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
