using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;
namespace CursorHub.Interface;
public partial class HotspotCanvas : Container
{
    private readonly Box vertical, horizontal;
    private readonly Sprite image;
    public void SetTexture(Texture texture) => image.Texture=texture;
    public Action<float,float>? Changed;
    public HotspotCanvas(Texture texture, float x, float y)
    {
        Size = new Vector2(256); Masking = true;
        Add(new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(217,223,233,255) });
        for (int row=0;row<16;row++) for(int col=0;col<16;col++)
            if ((row+col)%2==0) Add(new Box { Position=new Vector2(col*16,row*16),Size=new Vector2(16),Colour=new Color4(245,247,251,255) });
        Add(image=new Sprite { Texture=texture,RelativeSizeAxes=Axes.Both,Size=Vector2.One });
        Add(vertical=new Box { Width=1, Height=256,Colour=new Color4(204,52,113,255) });
        Add(horizontal=new Box { Width=256,Height=1,Colour=new Color4(204,52,113,255) });
        Set(x,y);
    }
    public void Set(float x,float y) { vertical.X=Math.Clamp(x,0,1)*255;horizontal.Y=Math.Clamp(y,0,1)*255; }
    private void Pick(Vector2 screen)
    {
        var p=ToLocalSpace(screen);float x=Math.Clamp(p.X/255,0,1),y=Math.Clamp(p.Y/255,0,1);Set(x,y);Changed?.Invoke(x,y);
    }
    protected override bool OnMouseDown(MouseDownEvent e) { if(e.Button!=osuTK.Input.MouseButton.Left)return false;Pick(e.ScreenSpaceMousePosition);return true; }
    protected override bool OnDragStart(DragStartEvent e) => e.Button==osuTK.Input.MouseButton.Left;
    protected override void OnDrag(DragEvent e) => Pick(e.ScreenSpaceMousePosition);
}
