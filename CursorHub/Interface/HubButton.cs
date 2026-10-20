using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK.Graphics;
using osu.Framework.Graphics.Textures;

namespace CursorHub.Interface;

public partial class HubButton : BasicButton
{
    public HubButton()
    {
        Masking = true;
        CornerRadius = 5;
        BackgroundColour = new Color4(46, 49, 53, 255);
        HoverColour = new Color4(255, 155, 32, 40);
        FlashColour = new Color4(110, 75, 32, 255);
    }
    public Color4 TextColour { set => SpriteText.Colour = value; }
    public Texture Skin
    {
        set
        {
            Masking = false;
            CornerRadius = 0;
            Background.Alpha = 0;
            Add(new Sprite { Texture = value, RelativeSizeAxes = Axes.Both, Size = osuTK.Vector2.One, Depth = 1 });
        }
    }
    protected override SpriteText CreateText() => new()
    {
        Anchor = Anchor.Centre, Origin = Anchor.Centre,
        Font = new FontUsage(size: 15), Colour = new Color4(227, 229, 232, 255)
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
