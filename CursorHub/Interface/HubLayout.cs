using CursorHub.Interface;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace CursorHub;

public partial class CursorHubGame
{
    private static readonly Color4 Orange = new(255, 155, 32, 255);
    private static readonly Color4 Muted = new(142, 149, 158, 255);
    private readonly Container[] sections = new Container[4];
    private readonly HubButton[] navigation = new HubButton[4];
    private HubButton playButton = null!, economyButton = null!, rateButton = null!;
    private SpriteText sectionTitle = null!;
    private Container previewCard = null!;
    private readonly BindableInt sizeBinding = new(18) { MinValue = 8, MaxValue = 48 };
    private readonly BindableInt lifetimeBinding = new(180) { MinValue = 60, MaxValue = 600 };
    private string notice = "Configuracoes locais";

    private Texture LoadSkin(string file)
    {
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", file));
        return Texture.FromStream(Host.Renderer, stream) ?? throw new InvalidOperationException("Imagem invalida: " + file);
    }

    private Container BuildInterface()
    {
        var root = new DrawSizePreservingFillContainer
        {
            TargetDrawSize = new Vector2(800, 500), Strategy = DrawSizePreservationStrategy.Separate
        };
        using var stream = File.OpenRead(Path.Combine(AppContext.BaseDirectory, "Assets", "frame.png"));
        root.Add(new Sprite { Texture = Texture.FromStream(Host.Renderer, stream), Size = new Vector2(800, 500) });
        root.Add(new DragStrip { Position = new Vector2(12, 28), Size = new Vector2(548, 70), DragWindow = () => chrome?.Drag() });
        root.Add(Label("CURSOR", 28, 45, 24, Color4.White));
        root.Add(Label("HUB", 135, 45, 24, Orange));
        root.Add(Label("MAKE YOUR MOVE.", 29, 77, 10, Muted));
        root.Add(Label("PERSONALIZACAO", 198, 91, 10, Orange));
        root.Add(sectionTitle = Label("Seu cursor, do seu jeito.", 198, 108, 20));
        root.Add(new TrafficLightButton { Skin = LoadSkin("close.png"), Text = "x", Position = new Vector2(660, 35), Action = () => Exit() });
        root.Add(new TrafficLightButton { Skin = LoadSkin("minimize.png"), Text = "-", Position = new Vector2(704, 35), Action = () => chrome?.Minimize() });
        root.Add(new TrafficLightButton { Skin = LoadSkin("maximize.png"), Text = "+", Position = new Vector2(748, 35), Action = () => chrome?.ToggleMaximize() });
        root.Add(new Box { Position = new Vector2(174, 138), Size = new Vector2(1, 267), Colour = new Color4(61, 65, 70, 255) });

        string[] names = { "01   Presets", "02   Cursor", "03   Rastro", "04   Desempenho" };
        for (int i = 0; i < 4; i++)
        {
            int index = i;
            root.Add(navigation[i] = MakeButton(names[i], 20, 140 + i * 46, 140, () => SelectSection(index)));
            root.Add(sections[i] = new Container { Position = new Vector2(198, 151), Size = new Vector2(235, 240) });
        }
        root.Add(Label("DESKTOP ENGINE", 29, 345, 9, Muted));
        root.Add(Label("osu!framework", 29, 363, 13));
        root.Add(Label("v0.3.4 / Windows", 29, 385, 10, Muted));

        sections[1].Add(Label("APARENCIA", 0, 0, 11, Muted));
        AddSlider(sections[1], "Tamanho do cursor", 34, size, 8, 48, value => size = value, "px", sizeBinding);
        sections[1].Add(Label("Cor de destaque", 0, 121, 14));
        for (int i = 0; i < palette.Length; i++)
        {
            int index = i;
            var swatch = MakeButton("", i * 51, 150, 38, () => colourIndex = index);
            swatch.BackgroundColour = palette[i];
            swatch.Height = 30;
            sections[1].Add(swatch);
        }
        sections[1].Add(Label("Formato inicial: circular", 0, 210, 11, Muted));

        sections[2].Add(Label("MOVIMENTO", 0, 0, 11, Muted));
        AddSlider(sections[2], "Duracao do rastro", 34, lifetime, 60, 600, value => lifetime = value, "ms", lifetimeBinding);
        sections[2].Add(Label("Rastro continuo", 0, 133, 16));
        sections[2].Add(Label("Espacamento interpolado", 0, 159, 12, Muted));
        sections[2].Add(Label("Ate 256 particulas reutilizadas", 0, 181, 12, Muted));

        sections[3].Add(Label("ORCAMENTO DE RENDERIZACAO", 0, 0, 10, Muted));
        sections[3].Add(rateButton = MakeButton($"Limite: {selectedRate} Hz", 0, 34, 222, () =>
        {
            selectedRate = selectedRate == 60 ? 120 : 60;
            rateButton.Text = $"Limite: {selectedRate} Hz";
            ApplyRate();
        }));
        sections[3].Add(economyButton = MakeButton("", 0, 90, 222, () =>
        {
            economy = !economy;
            UpdateEconomyLabel(); ApplyRate();
        }));
        UpdateEconomyLabel();
        sections[3].Add(Label("Economia: 15 Hz quando parado.", 0, 151, 11, Muted));
        sections[3].Add(Label("Retomada pode levar ate 67 ms.", 0, 174, 11, Muted));
        sections[3].Add(Label("Desligue para priorizar resposta.", 0, 197, 11, Muted));

        root.Add(previewCard = new Container
        {
            Position = new Vector2(454, 145), Size = new Vector2(310, 261),
            Masking = true, CornerRadius = 8, BorderThickness = 1, BorderColour = new Color4(64, 68, 74, 255),
            Children = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(25, 27, 30, 255) },
                Label("PREVIA AO VIVO", 17, 15, 10, Muted),
                new Box { Position = new Vector2(155, 99), Size = new Vector2(1, 50), Colour = new Color4(45, 48, 52, 255) },
                new Box { Position = new Vector2(130, 124), Size = new Vector2(50, 1), Colour = new Color4(45, 48, 52, 255) },
                Label("Mova o mouse nesta area", 64, 228, 12, Muted)
            }
        });
        root.Add(status = Label("", 198, 412, 10, Muted));
        root.Add(MakeButton("SALVAR AJUSTES", 24, 445, 166, SaveSettings));
        playButton = MakeButton("PLAY", 650, 444, 136, TogglePlay);
        playButton.Height = 42;
        playButton.Skin = LoadSkin("play.png");
        playButton.TextColour = new Color4(30, 30, 30, 255);
        root.Add(playButton);
        InitializePresets(root);
        SelectSection(0);
        return root;
    }

    private void SelectSection(int index)
    {
        string[] titles = { "Sua colecao de presets.", "Seu cursor, do seu jeito.", "Deixe sua marca.", "Leve por escolha." };
        if (presetMenu is not null) presetMenu.Alpha = 0;
        sectionTitle.Text = titles[index];
        for (int i = 0; i < sections.Length; i++)
        {
            sections[i].Alpha = i == index ? 1 : 0;
            navigation[i].BackgroundColour = i == index ? new Color4(79, 59, 33, 255) : new Color4(34, 36, 39, 255);
            navigation[i].TextColour = i == index ? Orange : Muted;
        }
    }

    private static SpriteText Label(string text, float x, float y, float size = 14, Color4? colour = null) => new()
    {
        Text = text, Position = new Vector2(x, y), Font = new FontUsage(size: size), Colour = colour ?? new Color4(228, 230, 232, 255)
    };

    private static HubButton MakeButton(string text, float x, float y, float width, Action action) => new()
    {
        Text = text, Position = new Vector2(x, y), Width = width, Height = 34, Action = action
    };

    private static void AddSlider(Container parent, string title, float y, int value, int min, int max, Action<int> changed, string unit, BindableInt? binding = null)
    {
        parent.Add(Label(title, 0, y));
        var number = Label($"{value} {unit}", 0, y + 27, 23, Orange);
        parent.Add(number);
        var current = binding ?? new BindableInt(value) { MinValue = min, MaxValue = max };
        current.Value = value;
        current.BindValueChanged(e => { changed(e.NewValue); number.Text = $"{e.NewValue} {unit}"; });
        parent.Add(new BasicSliderBar<int>
        {
            Position = new Vector2(0, y + 66), Size = new Vector2(222, 8),
            Current = current, BackgroundColour = new Color4(58, 62, 67, 255), SelectionColour = Orange
        });
    }

    private void UpdateEconomyLabel() => economyButton.Text = $"Economia: {(economy ? "ligada" : "desligada")}";
    private void SaveSettings()
    {
        try { new HubSettings(size, lifetime, selectedRate, colourIndex, economy).Save(); notice = "Configuracoes salvas"; }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { notice = "Nao foi possivel salvar"; }
        nextStatus = 0;
    }
}
