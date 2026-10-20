using System.Text.Json;

namespace CursorHub.Interface;

internal sealed record CursorPreset(Guid Id, string Name, HubSettings Settings);

internal sealed class PresetStore
{
    public List<CursorPreset> Items { get; } = new();
    private readonly string file;
    public PresetStore(string? path = null)
    {
        file = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CursorHub", "presets.json");
        try
        {
            var saved = JsonSerializer.Deserialize<List<CursorPreset>>(File.ReadAllText(file));
            if (saved is not null)
                Items.AddRange(saved.Where(p => p is not null && p.Settings is not null && p.Id != Guid.Empty && !string.IsNullOrWhiteSpace(p.Name)).Take(128));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or JsonException) { }
    }
    public void Put(CursorPreset preset)
    {
        string name = preset.Name.Trim();
        if (name.Length == 0 || name.Length > 36) throw new ArgumentException("Use um nome de 1 a 36 caracteres.");
        int index = Items.FindIndex(p => p.Id == preset.Id);
        if (index < 0 && Items.Count >= 128) throw new InvalidOperationException("Limite de 128 presets atingido.");
        var previous = Items.ToArray();
        if (index >= 0) Items[index] = preset with { Name = name }; else Items.Add(preset with { Name = name });
        try { Save(); } catch { Items.Clear(); Items.AddRange(previous); throw; }
    }
    public void Remove(Guid id)
    {
        var previous = Items.ToArray(); Items.RemoveAll(p => p.Id == id);
        try { Save(); } catch { Items.Clear(); Items.AddRange(previous); throw; }
    }
    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file + ".tmp", JsonSerializer.Serialize(Items));
        File.Move(file + ".tmp", file, true);
    }
}
