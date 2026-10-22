using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK.Graphics;
namespace CursorHub.Interface;
public partial class HubTextBox : BasicTextBox
{
    public HubTextBox()
    {
        BackgroundUnfocused=HubButton.DarkTheme?new Color4(48,58,75,255):new Color4(216,227,235,255);
        BackgroundFocused=new Color4(52,117,134,255);
    }
    protected override Drawable GetDrawableCharacter(char c)=>new SpriteText {Text=c.ToString(),Colour=HubButton.DarkTheme?Color4.White:new Color4(30,34,43,255),Font=new FontUsage(family:"KodeMono",size:FontSize)};
    protected override SpriteText CreatePlaceholder()=>new SpriteText {Font=new FontUsage(family:"KodeMono",size:14),Colour=new Color4(145,163,177,255)};
}
