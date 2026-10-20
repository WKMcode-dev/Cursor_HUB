using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK.Graphics;

namespace CursorHub.Interface;

public partial class TrafficLightButton : HubButton
{
    public TrafficLightButton()
    {
        Width = Height = 18;
        CornerRadius = 0;
        HoverColour = new Color4(255, 255, 255, 30);
        FlashColour = new Color4(255, 255, 255, 90);
    }

    protected override SpriteText CreateText() => new()
    {
        Anchor = Anchor.Centre, Origin = Anchor.Centre,
        Font = new FontUsage(size: 12), Colour = new Color4(40, 35, 30, 255), Alpha = 0
    };

    protected override bool OnHover(HoverEvent e)
    {
        base.OnHover(e);
        SpriteText.Alpha = 1;
        return true;
    }
    protected override void OnHoverLost(HoverLostEvent e)
    {
        base.OnHoverLost(e);
        SpriteText.Alpha = 0;
    }
}
