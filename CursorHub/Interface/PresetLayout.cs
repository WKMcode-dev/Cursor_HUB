using CursorHub.Interface;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace CursorHub;

public partial class CursorHubGame
{
    private readonly PresetStore presets = new();
    private Container presetRows = null!, presetMenu = null!, presetEditor = null!;
    private int presetPage;
    private Guid? menuFor;

    private void InitializePresets(Container root)
    {
        sections[0].Add(MakeButton("+  ADICIONAR PRESET", 0, 0, 235, () => OpenPresetEditor(null)));
        sections[0].Add(presetRows = new Container { Position = new Vector2(0, 46), Size = new Vector2(235, 180) });
        sections[0].Add(MakeButton("<", 0, 228, 40, () => { presetPage = Math.Max(0, presetPage - 1); RefreshPresets(); }));
        sections[0].Add(MakeButton(">", 190, 228, 40, () => { presetPage = Math.Min(Math.Max(0, (presets.Items.Count - 1) / 3), presetPage + 1); RefreshPresets(); }));
        root.Add(presetMenu = new Container { Size = new Vector2(147, 122), Alpha = 0, Depth = -40 });
        root.Add(presetEditor = new Container { Position = new Vector2(440, 97), Size = new Vector2(334, 320), Alpha = 0, Depth = -50 });
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        presetRows.Clear();
        presetPage = Math.Min(presetPage, Math.Max(0, (presets.Items.Count - 1) / 3));
        if (presets.Items.Count == 0)
        {
            presetRows.Add(Label("Sua colecao comeca aqui.", 4, 25, 13, Muted));
            presetRows.Add(Label("Adicione seu primeiro preset.", 4, 48, 12, Muted));
        }
        int row = 0;
        foreach (var preset in presets.Items.Skip(presetPage * 3).Take(3))
        {
            int y = row++ * 57;
            var name = preset.Name.Length > 22 ? preset.Name[..21] + "…" : preset.Name;
            presetRows.Add(MakeButton(name, 0, y, 190, () => Demonstrate(preset)));
            presetRows.Add(MakeButton("...", 195, y, 40, () => OpenPresetMenu(preset, y)));
            presetRows.Add(Label($"{preset.Settings.Size} px / {preset.Settings.Lifetime} ms", 8, y + 37, 10, Muted));
        }
    }

    private void OpenPresetMenu(CursorPreset preset, float rowY)
    {
        if (menuFor == preset.Id && presetMenu.Alpha > 0) { presetMenu.Alpha = 0; return; }
        menuFor = preset.Id;
        presetMenu.Clear();
        presetMenu.Position = new Vector2(439, Math.Min(197 + rowY, 295));
        presetMenu.Add(new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(17, 19, 22, 255) });
        presetMenu.Add(MakeButton("Demonstrar", 4, 4, 139, () => { Demonstrate(preset); presetMenu.Alpha = 0; }));
        presetMenu.Add(MakeButton("Editar", 4, 44, 139, () => { presetMenu.Alpha = 0; OpenPresetEditor(preset); }));
        presetMenu.Add(MakeButton("Excluir", 4, 84, 139, () =>
        {
            try { presets.Remove(preset.Id); presetMenu.Alpha = 0; RefreshPresets(); notice = "Preset excluido"; }
            catch (Exception e) { notice = e.Message; }
        }));
        presetMenu.Alpha = 1;
    }

    private HubSettings CurrentSettings() => new(size, lifetime, selectedRate, colourIndex, economy);

    private void Demonstrate(CursorPreset preset)
    {
        var s = preset.Settings;
        sizeBinding.Value = Math.Clamp(s.Size, 8, 48);
        lifetimeBinding.Value = Math.Clamp(s.Lifetime, 60, 600);
        colourIndex = Math.Clamp(s.Colour, 0, palette.Length - 1);
        selectedRate = s.Rate == 120 ? 120 : 60;
        economy = s.Economy; UpdateEconomyLabel();
        rateButton.Text = $"Limite: {selectedRate} Hz";
        trail.Clear(); ApplyRate();
        notice = "Demonstrando: " + preset.Name;
        nextStatus = 0;
        // The running desktop preset is unchanged until the next Play.
    }

    private void OpenPresetEditor(CursorPreset? existing)
    {
        presetEditor.Clear();
        var s = existing?.Settings ?? CurrentSettings();
        int editSize = s.Size, editLifetime = s.Lifetime, editColour = Math.Clamp(s.Colour, 0, palette.Length - 1);
        presetEditor.Add(new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(26, 28, 32, 255) });
        presetEditor.Add(Label(existing is null ? "NOVO PRESET" : "EDITAR PRESET", 16, 14, 14, Orange));
        var name = new BasicTextBox { Position = new Vector2(16, 42), Size = new Vector2(302, 32), Text = existing?.Name ?? $"Preset {presets.Items.Count + 1}", PlaceholderText = "Nome do preset" };
        presetEditor.Add(name);
        var options = new Container { Position = new Vector2(16, 88), Size = new Vector2(300, 185) };
        AddSlider(options, "Tamanho", 0, Math.Clamp(editSize, 8, 48), 8, 48, value => editSize = value, "px");
        AddSlider(options, "Duracao", 83, Math.Clamp(editLifetime, 60, 600), 60, 600, value => editLifetime = value, "ms");
        for (int i = 0; i < palette.Length; i++)
        {
            int selected = i;
            var swatch = MakeButton("", 252, i * 38, 42, () => editColour = selected);
            swatch.BackgroundColour = palette[i];
            options.Add(swatch);
        }
        presetEditor.Add(options);
        presetEditor.Add(MakeButton("SALVAR", 16, 276, 142, () =>
        {
            try
            {
                presets.Put(new CursorPreset(existing?.Id ?? Guid.NewGuid(), name.Text, s with { Size = editSize, Lifetime = editLifetime, Colour = editColour }));
                presetEditor.Alpha = 0; RefreshPresets(); notice = "Preset salvo"; nextStatus = 0;
            }
            catch (Exception e) { notice = e.Message; nextStatus = 0; }
        }));
        presetEditor.Add(MakeButton("CANCELAR", 176, 276, 142, () => presetEditor.Alpha = 0));
        presetEditor.Alpha = 1;
    }
}
