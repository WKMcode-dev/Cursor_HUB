using CursorHub.Core;
using CursorHub.Interface;
using CursorHub.Windows;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Textures;
using osuTK;
using osuTK.Graphics;
namespace CursorHub;
public partial class CursorHubGame
{
    private uint? cursorRgb,trailRgb;
    private Container cursorColours=null!,trailColours=null!;
    private bool choosingColour;
    private Color4 ResolveColour(int index,uint? rgb)=>ColourValue.Valid(rgb) is {} value?new Color4((byte)(value>>16),(byte)(value>>8),(byte)value,255):palette[Math.Clamp(index,0,3)];
    private static uint Rgb(Color4 colour)=>((uint)MathF.Round(colour.R*255)<<16)|((uint)MathF.Round(colour.G*255)<<8)|(uint)MathF.Round(colour.B*255);
    private Color4 TrailAccent=>ResolveColour(trailColourIndex,trailRgb);
    private void RefreshColourControls()
    {
        if(cursorColours is null||trailColours is null)return;
        void Fill(Container root,bool isTrail)
        {
            root.Clear();if((isTrail?trailImage:cursorImage) is not null){root.Hide();return;}root.Show();
            root.Add(Label(isTrail?"Cor do rastro":"Cor do cursor",0,0,12));
            for(int i=0;i<palette.Length;i++)
            {
                int index=i;var swatch=MakeButton("",i*47,24,38,()=>
                {
                    if(isTrail){trailColourIndex=index;trailRgb=null;}else{colourIndex=index;cursorRgb=null;}
                    RefreshColourControls();
                });swatch.Height=28;swatch.BackgroundColour=palette[i];root.Add(swatch);
            }
            var other=MakeButton("...",196,24,36,()=>ChooseColour(Rgb(isTrail?TrailAccent:Accent),value=>
            {
                if(isTrail)trailRgb=value;else cursorRgb=value;RefreshColourControls();
            }));other.Height=28;root.Add(other);
            if(!isTrail)root.Add(Label("...  SELETOR DE CORES",0,63,9,Muted));
        }
        Fill(cursorColours,false);Fill(trailColours,true);
    }
    private async void ChooseColour(uint initial,Action<uint> apply)
    {
        if(choosingColour)return;choosingColour=true;
        try
        {
            verificationColourThread=0;
            uint? colour=await ColourPicker.PickAsync(CursorInput.FindWindow(null,windowTitle),initial,
                VerificationDirectory is null ? null : id=>Volatile.Write(ref verificationColourThread,id));
            if(colour is {} value)await OnGameThread(()=>{apply(value);notice="Cor aplicada. Salve os ajustes ou o preset.";nextStatus=0;});
        }
        catch(Exception e){await OnGameThread(()=>{notice=e.Message;nextStatus=0;});}
        finally{await OnGameThread(()=>choosingColour=false);}
    }
    private Texture UserImage(string? path)
    {
        if(path is not null)try{if(File.Exists(path))return LoadSkin(path);}catch(Exception){notice="Imagem indisponivel; formato circular usado.";}
        return LoadSkin("particle.png");
    }
}

