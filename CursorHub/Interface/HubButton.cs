using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK.Graphics;
using osu.Framework.Graphics.Textures;

namespace CursorHub.Interface;

public partial class HubButton : BasicButton
{
    private Sprite? skin;
    public HubButton()
    {
        Masking = true;
        CornerRadius = 4;
        BorderThickness = 1;
        BorderColour = new Color4(191, 196, 206, 255);
        BackgroundColour = new Color4(250, 251, 253, 255);
        HoverColour = new Color4(92, 214, 227, 65);
        FlashColour = new Color4(221, 195, 230, 255);
    }
    public void SetCaptionLayout(float left, float size)
    {
        SpriteText.Anchor = Anchor.CentreLeft;
        SpriteText.Origin = Anchor.CentreLeft;
        SpriteText.X = left;
        SpriteText.Font = new FontUsage(size: size);
    }
    public Color4 TextColour { set => SpriteText.Colour = value; }
    public Texture Skin
    {
        set
        {
            Masking = false;
            CornerRadius = 0;
            BorderThickness = 0;
            Background.Alpha = 0;
            if (skin is null) Add(skin = new Sprite { RelativeSizeAxes = Axes.Both, Size = osuTK.Vector2.One, Depth = 1 });
            skin.Texture = value;
        }
    }
    protected override SpriteText CreateText() => new()
    {
        Anchor = Anchor.Centre, Origin = Anchor.Centre,
        Font = new FontUsage(size: 15), Colour = new Color4(30, 34, 43, 255)
    };
}

public partial class DragStrip : osu.Framework.Graphics.Containers.Container
{
    public Action? DragWindow;
    protected override bool OnMouseDown(MouseDownEvent e)
    {
        if (e.Button != osuTK.Input.MouseButton.Left) return false;
        DragWindow?.Invoke();
        return true;
    }
}
