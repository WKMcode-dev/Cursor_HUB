using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;

namespace CursorHub.Interface;

// No BasicButton hover/flash layers: only the explicitly supplied image is drawn.
public partial class HubButton : ClickableContainer
{
    public static bool DarkTheme;
    public static bool Animations = true;
    private Sprite? skin;
    protected readonly Box Background;
    protected readonly SpriteText SpriteText;
    public string Text { get => SpriteText.Text.ToString(); set => SpriteText.Text = value; }
    public Color4 BackgroundColour { set => Background.Colour = value; }
    public Color4 HoverColour { get; set; }
    public Color4 FlashColour { get; set; }
    public HubButton()
    {
        Masking = true; CornerRadius = 4; BorderThickness = 1;
        BorderColour = new Color4(150, 163, 181, 255);
        Add(Background = new Box { RelativeSizeAxes = Axes.Both, Colour = DarkTheme ? new Color4(44, 50, 64, 255) : new Color4(250, 251, 253, 255) });
        Add(SpriteText = CreateText());
    }
    public void SetCaptionLayout(float left, float size)
    {
        SpriteText.Anchor = SpriteText.Origin = Anchor.CentreLeft;
        SpriteText.X = left; SpriteText.Font = new FontUsage(family: "KodeMono", size: size);
    }
    public Color4 SkinColour { set { if (skin is not null) skin.Colour = value; } }
    public Color4 TextColour { set => SpriteText.Colour = value; }
    public Texture Skin
    {
        set
        {
            Masking = false; CornerRadius = 0; BorderThickness = 0; Background.Hide();
            if (skin is null) Add(skin = new Sprite { RelativeSizeAxes = Axes.Both, Size = osuTK.Vector2.One, Depth = 1 });
            skin.Texture = value;
        }
    }
    protected override bool OnHover(HoverEvent e) { this.FadeTo(.85f, Animations?80:0); return true; }
    protected override void OnHoverLost(HoverLostEvent e) { this.FadeTo(1, Animations?80:0); }
    protected virtual SpriteText CreateText() => new()
    {
        Anchor = Anchor.Centre, Origin = Anchor.Centre,
        Font = new FontUsage(family: "KodeMono", size: 15), Colour = DarkTheme ? new Color4(230, 236, 246, 255) : new Color4(30, 34, 43, 255)
    };
}
public partial class DragStrip : Container
{
    public Action? DragWindow;
    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button != osuTK.Input.MouseButton.Left) return false;
        DragWindow?.Invoke(); return true;
    }
}
