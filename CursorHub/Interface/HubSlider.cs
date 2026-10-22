using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
namespace CursorHub.Interface;
// Track and thumb come from the supplied slide_adjust.svg. Geometry stays proportional.
public partial class HubSlider : Container
{
    private const float scale=222f/375, left=12.28f*scale, right=361.99f*scale;
    private readonly BindableInt current;
    private readonly Sprite thumb;
    private readonly Container fill;
    private readonly Action<ValueChangedEvent<int>> changed;
    public HubSlider(BindableInt current,Texture track,Texture handle,Color4 colour)
    {
        this.current=current;Size=new Vector2(222,22);
        Add(new Sprite {Texture=track,Size=new Vector2(222,25*scale),Y=3,Colour=colour});
        Add(fill=new Container {Position=new Vector2(left,3+12.28f*scale-2.5f*scale),Height=5*scale,Masking=true,CornerRadius=2.5f*scale,Child=new Box {RelativeSizeAxes=Axes.Both,Colour=colour}});
        Add(thumb=new Sprite {Texture=handle,Size=new Vector2(14*scale),Origin=Anchor.Centre,Y=3+12.28f*scale,Colour=colour});
        changed=_=>DrawValue();current.BindValueChanged(changed,true);
    }
    private void DrawValue()
    {
        float amount=(current.Value-current.MinValue)/(float)Math.Max(1,current.MaxValue-current.MinValue);
        thumb.X=left+(right-left)*amount;fill.Width=Math.Max(.01f,(right-left)*amount);
    }
    private void Pick(Vector2 point)
    {
        float amount=Math.Clamp((ToLocalSpace(point).X-left)/(right-left),0,1);
        current.Value=(int)MathF.Round(current.MinValue+(current.MaxValue-current.MinValue)*amount);
    }
    protected override bool OnMouseDown(MouseDownEvent e){if(e.Button!=osuTK.Input.MouseButton.Left)return false;Pick(e.ScreenSpaceMousePosition);return true;}
    protected override bool OnDragStart(DragStartEvent e)=>e.Button==osuTK.Input.MouseButton.Left;
    protected override void OnDrag(DragEvent e)=>Pick(e.ScreenSpaceMousePosition);
    protected override void Dispose(bool disposing){current.ValueChanged-=changed;base.Dispose(disposing);}
}
