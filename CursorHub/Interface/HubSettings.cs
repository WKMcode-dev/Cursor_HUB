using System.Text.Json;

namespace CursorHub.Interface;

internal sealed record HubSettings(int Size = 18, int Lifetime = 180, int Rate = 60, int Colour = 0, bool Economy = true, string? CursorImage = null, string? TrailImage = null, float HotX = .5f, float HotY = .5f, int? TrailSize = null, int? TrailColour = null, uint? CursorRgb = null, uint? TrailRgb = null)
{
    public int ResolvedTrailSize => Math.Clamp(TrailSize ?? Size,8,48);
    public int ResolvedTrailColour => Math.Clamp(TrailColour ?? Colour,0,3);
    private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CursorHub", "settings.json");
    public static HubSettings Load()
    {
        try
        {
            var s = JsonSerializer.Deserialize<HubSettings>(File.ReadAllText(FilePath)) ?? new();
            return s with { CursorRgb=CursorHub.Core.ColourValue.Valid(s.CursorRgb),TrailRgb=CursorHub.Core.ColourValue.Valid(s.TrailRgb), TrailSize=s.ResolvedTrailSize, TrailColour=s.ResolvedTrailColour, Size = Math.Clamp(s.Size, 8, 48), Lifetime = Math.Clamp(s.Lifetime, 60, 600), Rate = Math.Clamp(s.Rate, 30, 360), Colour = Math.Clamp(s.Colour, 0, 3) };
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath + ".tmp", JsonSerializer.Serialize(this));
        File.Move(FilePath + ".tmp", FilePath, true);
    }
}
