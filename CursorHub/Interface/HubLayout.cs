using CursorHub.Core;
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
    private static readonly Color4 Orange = new(40, 125, 145, 255);
    private static Color4 Muted => darkTheme ? new Color4(162, 177, 198, 255) : new Color4(100, 105, 117, 255);
    private readonly Container[] sections = new Container[5];
    private readonly HubButton[] navigation = new HubButton[5];
    private HubButton playButton = null!, economyButton = null!;
    private BindableInt rateBinding = new(60) { MinValue = 30, MaxValue = 360 };
    private Container previewCard = null!;
    private Sprite previewPlaceholder = null!;
    private BindableInt sizeBinding = new(18) { MinValue = 8, MaxValue = 48 };
    private BindableInt trailSizeBinding = new(18) { MinValue=8,MaxValue=48 };
    private BindableInt lifetimeBinding = new(180) { MinValue = 60, MaxValue = 600 };
    private int currentSection;
    private string notice = "Configuracoes locais";

    private readonly Dictionary<string, Texture> skins = new();
    private Texture LoadSkin(string file)
    {
        if (darkTheme && file is "frame.png" or "nav.png" or "nav-selected.png" or "preset-card.png" or "preset-toolbar.png") file = "dark-" + file;
        if (skins.TryGetValue(file, out var cached)) return cached;
        using var stream = File.OpenRead(Path.Combine(AssetPaths.Directory, file));
        return skins[file] = Texture.FromStream(Host.Renderer, stream) ?? throw new InvalidOperationException("Imagem invalida: " + file);
    }

    private Container BuildInterface()
    {
        var root = new Container { Size = new Vector2(InterfaceViewport.Width, InterfaceViewport.Height), Anchor = Anchor.Centre, Origin = Anchor.Centre };
        root.Add(new Sprite { Texture = LoadSkin("frame.png"), Size = new Vector2(1100, 500) });
        root.Add(new DragStrip { Position = new Vector2(12, 28), Size = new Vector2(228, 60), DragWindow = () => chrome?.Drag() });
        root.Add(Label("CURSOR / HUB", 26, 43, 23));
        root.Add(Label("CHARACTER CHAIN   /   01", 27, 73, 9, Muted));
        root.Add(Label("DEMONSTRACAO AO VIVO", 800, 83, 10, Orange));

        root.Add(new TrafficLightButton { Skin = LoadSkin("close.png"), Text = "", Position = new Vector2(960, 35), Action = () => Exit() });
        root.Add(new TrafficLightButton { Skin = LoadSkin("minimize.png"), Text = "", Position = new Vector2(1004, 35), Action = () => chrome?.Minimize() });
        root.Add(new TrafficLightButton { Skin = LoadSkin("maximize.png"), Text = "", Position = new Vector2(1048, 35), Action = () => chrome?.ToggleMaximize() });
        root.Add(new Sprite { Texture = LoadSkin("divider.png"), Position = new Vector2(231, 36), Size = new Vector2(10, 432), Colour = darkTheme ? new Color4(177, 194, 212, 255) : Color4.White });
        root.Add(new Sprite { Texture = LoadSkin("chain.png"), Position = new Vector2(26, 323), Size = new Vector2(185, 26) });
        string[] names = { "PRESETS", "CURSOR", "RASTRO", "DESEMPENHO", "CONFIGURACOES" };
        for (int i = 0; i < 5; i++)
        {
            int index = i;
            root.Add(navigation[i] = MakeButton(names[i], 26, 109 + i * 42, 190, () => SelectSection(index)));
            navigation[i].Height = 40;
            navigation[i].SetCaptionLayout(56, 13);
            root.Add(Label($"{i + 1:00}", 41, 122 + i * 42, 12, Muted));
            root.Add(sections[i] = new Container { Position = new Vector2(264, 100), Size = new Vector2(235, 345) });
        }
        root.Add(Label("DESKTOP PERSONALIZATION", 27, 367, 9, Muted));
        root.Add(Label("osu!framework / v0.9.0", 27, 384, 11));
        root.Add(Label("CTRL + SHIFT + X  /  PARAR", 27, 410, 9, Muted));

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

        sections[2].Add(Label("RASTRO",0,0,11,Muted));
        AddSlider(sections[2],"Tamanho do rastro",24,trailSize,8,48,v=>trailSize=v,"px",trailSizeBinding);
        AddSlider(sections[2],"Duracao do rastro",108,lifetime,60,600,v=>lifetime=v,"ms",lifetimeBinding);
        sections[2].Add(Label("Cor do rastro",0,198,12));
        for(int i=0;i<palette.Length;i++){int c=i;var swatch=MakeButton("",i*51,222,38,()=>trailColourIndex=c);swatch.BackgroundColour=palette[i];swatch.Height=28;sections[2].Add(swatch);}

        sections[3].Add(Label("ORCAMENTO DE RENDERIZACAO", 0, 0, 10, Muted));
        AddSlider(sections[3], "Limite de desenho", 28, selectedRate, 30, 360, value =>
        {
            selectedRate = value; ApplyRate(); desktop?.SetRate(value);
        }, "Hz", rateBinding);
        int[] rates = { 60, 75, 144, 165 };
        for (int i = 0; i < rates.Length; i++)
        {
            int rate = rates[i];
            var shortcut = MakeButton(rate.ToString(), i * 56, 130, 50, () => rateBinding.Value = rate);
            shortcut.Height = 28;
            sections[3].Add(shortcut);
        }
        sections[3].Add(economyButton = MakeButton("", 0, 174, 222, () =>
        {
            economy = !economy; UpdateEconomyLabel(); ApplyRate();
        }));
        UpdateEconomyLabel();
        sections[3].Add(Label("Ajuste ao monitor e ao seu sistema.", 0, 218, 11, Muted));
        sections[3].Add(Label("Economia: 15 Hz com a previa parada.", 0, 239, 10, Muted));

        root.Add(previewCard = new Container
        {
            Position = new Vector2(800, 104), Size = new Vector2(266, 266),
            Masking = true, CornerRadius = 5, BorderThickness = 0, BorderColour = new Color4(190, 194, 203, 255),
            Children = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = darkTheme ? new Color4(26, 31, 41, 255) : Color4.White },
                
                new Sprite { Texture = LoadSkin("demonstration.png"), RelativeSizeAxes = Axes.Both, Size = Vector2.One },
                
                
            }
        });
        root.Add(Label("Mova o mouse nesta area", 800, 386, 11, Muted));
        root.Add(new Box { Position = new Vector2(780, 100), Size = new Vector2(1, 318), Colour = new Color4(207, 211, 219, 255) });
        previewCard.Add(previewPlaceholder = new Sprite { Texture = LoadSkin("cursor.png"), Anchor = Anchor.Centre, Origin = Anchor.Centre, Size = new Vector2(23, 29), Colour = Accent });
        root.Add(status = Label("", 264, 449, 8, Muted));
        root.Add(MakeButton("SALVAR AJUSTES", 26, 442, 190, SaveSettings));
        playButton = MakeButton("PLAY", 950, 444, 136, TogglePlay);
        playButton.Height = 42;
        playButton.Skin = LoadSkin("play.png");
        playButton.TextColour = new Color4(248, 249, 252, 255);
        root.Add(playButton);
        InitializePresets(root);
        BuildCustomization(root);
        SelectSection(0);
        return root;
    }

    private void SelectSection(int index)
    {

        currentSection = index;
        if (hotspotDialog is not null) hotspotDialog.Alpha = 0;
        if (presetMenu is not null) presetMenu.Alpha = 0;
        if (presetEditor is not null) presetEditor.Alpha = 0;
        trail.Clear();
        for (int i = 0; i < sections.Length; i++)
        {
            sections[i].Alpha = i == index ? 1 : 0;
            navigation[i].Skin = LoadSkin(i == index ? "nav-selected.png" : "nav.png");
            navigation[i].BackgroundColour = i == index ? new Color4(30, 34, 43, 255) : new Color4(247, 248, 250, 255);
            navigation[i].TextColour = i == index ? new Color4(245, 247, 251, 255) : Muted;
        }
    }

    private static SpriteText Label(string text, float x, float y, float size = 14, Color4? colour = null) => new()
    {
        Text = text, Position = new Vector2(x, y), Font = new FontUsage(family: "KodeMono", size: size), Colour = colour ?? (darkTheme ? new Color4(232, 238, 248, 255) : new Color4(30, 34, 43, 255))
    };

    private static HubButton MakeButton(string text, float x, float y, float width, Action action) => new()
    {
        Text = text, Position = new Vector2(x, y), Width = width, Height = 34, Action = action
    };

    private void AddSlider(Container parent, string title, float y, int value, int min, int max, Action<int> changed, string unit, BindableInt? binding = null)
    {
        parent.Add(Label(title, 0, y));
        var number = Label($"{value} {unit}", 0, y + 27, 23, Orange);
        parent.Add(number);
        var current = binding ?? new BindableInt(value) { MinValue = min, MaxValue = max };
        current.Value = value;
        current.BindValueChanged(e => { changed(e.NewValue); number.Text = $"{e.NewValue} {unit}"; });
        parent.Add(new HubSlider(current,LoadSkin("slider-track.png"),LoadSkin("slider-thumb.png"),Orange)
        {Position=new Vector2(0,y+62)});
    }

    private void UpdateEconomyLabel() => economyButton.Text = $"Economia: {(economy ? "ligada" : "desligada")}";
    private void SaveSettings()
    {
        try {
            var settings=CurrentSettings();settings.Save();
            var selected=presets.Items.FirstOrDefault(p=>p.Id==demonstratedPreset);
            if(selected is not null){presets.Put(selected with {Settings=settings});RefreshPresets();}
            notice = selected is null?"Configuracoes salvas":"Preset e miniatura atualizados";
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { notice = "Nao foi possivel salvar"; }
        nextStatus = 0;
    }
}
