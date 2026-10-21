using System.Text.Json;
namespace CursorHub.Interface;
internal sealed record AppPreferences(bool DarkTheme = false, bool Animations = true, bool History = true, int Particles = 512)
{
    private static string FileName => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CursorHub", "preferences.json");
    public static AppPreferences Load()
    {
        try { var p = JsonSerializer.Deserialize<AppPreferences>(File.ReadAllText(FileName)) ?? new(); return p with { Particles = Math.Clamp(p.Particles, 256, 1024) }; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FileName)!);
        File.WriteAllText(FileName + ".tmp", JsonSerializer.Serialize(this)); File.Move(FileName + ".tmp", FileName, true);
    }
}
