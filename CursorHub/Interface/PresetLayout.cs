using CursorHub.Interface;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using CursorHub.Windows;
using osuTK.Graphics;

namespace CursorHub;

public partial class CursorHubGame
{
    private readonly PresetStore presets = new();
    private Container presetRows = null!, presetMenu = null!, presetEditor = null!;
    private int presetPage;
    private Guid? menuFor;

    private const int PresetsPerPage = 24;
    private Guid? demonstratedPreset;
    private SpriteText presetCounter = null!;

    private void InitializePresets(Container root)
    {
        sections[0].Position = new Vector2(264, 100);
        sections[0].Size = new Vector2(512, 344);
        sections[0].Add(presetRows = new Container { Size = new Vector2(512, 318) });
        var previous = MakeButton("<", 0, 324, 28, () => { presetPage = Math.Max(0, presetPage - 1); RefreshPresets(); });
        var next = MakeButton(">", 34, 324, 28, () => { presetPage = Math.Min(Math.Max(0, (presets.Items.Count) / PresetsPerPage), presetPage + 1); RefreshPresets(); });
        previous.Height = next.Height = 20;
        sections[0].Add(previous); sections[0].Add(next);
        sections[0].Add(presetCounter = Label("", 74, 329, 9, Muted));
        root.Add(presetMenu = new Container { Size = new Vector2(23.0625f, 72), Alpha = 0, Depth = -40 });
        root.Add(presetEditor = new Container { Position = new Vector2(420, 98), Size = new Vector2(334, 320), Alpha = 0, Depth = -50 });
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        presetMenu.Alpha = 0;
        presetRows.Clear();
        int pages = Math.Max(1, (presets.Items.Count + 1 + PresetsPerPage - 1) / PresetsPerPage);
        presetPage = Math.Clamp(presetPage, 0, pages - 1);
        presetCounter.Text = $"{presets.Items.Count} PRESETS   /   {presetPage + 1:00} - {pages:00}";
        var page = presets.Items.Skip(presetPage * PresetsPerPage).Take(PresetsPerPage).ToArray();
        for (int cell = 0; cell < PresetsPerPage; cell++)
        {
            float x = (cell % 6) * 85, y = (cell / 6) * 82;
            if (cell < page.Length)
            {
                var preset = page[cell];
                var card = new Container { Position = new Vector2(x, y), Size = new Vector2(72, 72) };
                var button = MakeButton("", 0, 0, 72, () => { presetMenu.Alpha = 0; Demonstrate(preset); RefreshPresets(); });
                button.Height = 72;
                button.Skin = LoadSkin("preset-card.png");
                button.SkinColour = preset.Id == demonstratedPreset ? new Color4(221, 241, 243, 255) : Color4.White;
                card.Add(button);
                card.Add(new osu.Framework.Graphics.Sprites.Sprite
                {
                    Texture = LoadSkin("cursor.png"), Position = new Vector2(20, 16), Size = new Vector2(24, 30),
                    Colour = palette[Math.Clamp(preset.Settings.Colour, 0, palette.Length - 1)]
                });
                var name = preset.Name.Length > 9 ? preset.Name[..8] + ".." : preset.Name;
                card.Add(Label(name, 6, 57, 8));
                int selectedCell = cell;
                var menu = MakeButton("", 55, 44, 16, () => OpenPresetMenu(preset, selectedCell));
                menu.Height = 26;
                menu.BorderThickness = 0;
                menu.Skin = LoadSkin("preset-menu.png");
                card.Add(menu);
                presetRows.Add(card);
            }
            else if (cell == page.Length && presets.Items.Count < 128)
            {
                var add = MakeButton("+", x, y, 72, () => OpenPresetEditor(null));
                add.Height = 72;
                presetRows.Add(add);
                presetRows.Add(Label("NOVO", x + 23, y + 57, 8, Muted));
            }
            else
            {
                // Empty slots are static geometry, never fictitious saved presets.
                var empty = new Container { Position = new Vector2(x, y), Size = new Vector2(72, 72), Masking = true, CornerRadius = 4, BorderThickness = 1, BorderColour = new Color4(228, 229, 232, 255) };
                empty.Add(new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(244, 245, 248, 255) });
                empty.Add(Label("+", 31, 21, 13, new Color4(196, 201, 210, 255)));
                presetRows.Add(empty);
            }
        }
    }

    private void OpenPresetMenu(CursorPreset preset, float rowY)
    {
        if (menuFor == preset.Id && presetMenu.Alpha > 0) { presetMenu.Alpha = 0; return; }
        menuFor = preset.Id;
        presetMenu.Clear();
        int cell = (int)rowY;
        float tileX = 264 + (cell % 6) * 85;
        float menuX = tileX + 74;
        if (menuX + presetMenu.Width > 776) menuX = tileX - presetMenu.Width - 2;
        presetMenu.Position = new Vector2(menuX, 100 + (cell / 6) * 82);
        presetMenu.Add(new Sprite { Texture = LoadSkin("preset-toolbar.png"), RelativeSizeAxes = Axes.Both, Size = Vector2.One });
        void Item(string skin, int y, Action action)
        {
            var item = MakeButton("", 0, y, 23, action);
            item.Height = 24;
            item.Skin = LoadSkin(skin);
            presetMenu.Add(item);
        }
        Item("preset-trash.png", 0, () =>
        {
            try { presets.Remove(preset.Id); if (demonstratedPreset == preset.Id) demonstratedPreset = null; presetMenu.Alpha = 0; RefreshPresets(); notice = "Preset excluido"; nextStatus = 0; }
            catch (Exception e) { notice = e.Message; nextStatus = 0; }
        });
        Item("preset-edit.png", 24, () => { presetMenu.Alpha = 0; OpenPresetEditor(preset); });
        Item("preset-use.png", 48, () => { Demonstrate(preset); presetMenu.Alpha = 0; RefreshPresets(); });
        presetMenu.Alpha = 1;
    }

    private HubSettings CurrentSettings() => new(size, lifetime, selectedRate, colourIndex, economy, cursorImage, trailImage, hotX, hotY);

    private void Demonstrate(CursorPreset preset)
    {
        demonstratedPreset = preset.Id;
        var s = preset.Settings;
        cursorImage=s.CursorImage;trailImage=s.TrailImage;hotX=Math.Clamp(s.HotX,0,1);hotY=Math.Clamp(s.HotY,0,1);
        try{cursorArt=cursorImage is null?null:RasterAsset.Load(cursorImage);trailArt=trailImage is null?null:RasterAsset.Load(trailImage);}catch{cursorImage=trailImage=null;cursorArt=trailArt=null;}
        sizeBinding.Value = Math.Clamp(s.Size, 8, 48);
        lifetimeBinding.Value = Math.Clamp(s.Lifetime, 60, 600);
        colourIndex = Math.Clamp(s.Colour, 0, palette.Length - 1);
        selectedRate = Math.Clamp(s.Rate, 30, 360);
        rateBinding.Value = selectedRate;
        economy = s.Economy; UpdateEconomyLabel();
        trail.Clear(); ApplyRate();
        notice = "Demonstrando: " + preset.Name;
        nextStatus = 0;
        // The running desktop preset is unchanged until the next Play.
    }

    private void OpenPresetEditor(CursorPreset? existing)
    {
        presetMenu.Alpha = 0;
        presetEditor.Clear();
        var s = existing?.Settings ?? CurrentSettings();
        int editSize = s.Size, editLifetime = s.Lifetime, editColour = Math.Clamp(s.Colour, 0, palette.Length - 1);
        presetEditor.Add(new Box { RelativeSizeAxes = Axes.Both, Colour = darkTheme?new Color4(36,42,54,255):new Color4(238, 241, 246, 255) });
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
                presetPage = existing is null ? Math.Max(0, (presets.Items.Count - 1) / PresetsPerPage) : presetPage;
                presetEditor.Alpha = 0; RefreshPresets(); notice = "Preset salvo"; nextStatus = 0;
            }
            catch (Exception e) { notice = e.Message; nextStatus = 0; }
        }));
        presetEditor.Add(MakeButton("CANCELAR", 176, 276, 142, () => presetEditor.Alpha = 0));
        presetEditor.Alpha = 1;
    }
}
