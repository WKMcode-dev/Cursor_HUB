using CursorHub.Interface;
using CursorHub.Windows;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;
using osuTK.Graphics;

namespace CursorHub;
public partial class CursorHubGame
{
    private AppPreferences preferences = AppPreferences.Load();
    private static bool darkTheme;
    private string? cursorImage, trailImage;
    private RasterAsset? cursorArt, trailArt;
    private float hotX=.5f, hotY=.5f;
    private Container hotspotDialog=null!;
    private void BuildCustomization(Container root)
    {
        sections[1].Add(MakeButton("IMPORTAR CURSOR PNG / SVG",0,224,235,()=>ImportImage(false)));
        sections[1].Add(MakeButton("EDITAR HOTSPOT",0,264,235,OpenHotspot));
        sections[1].Add(MakeButton("CURSOR CIRCULAR",0,304,235,()=>{cursorImage=null;cursorArt=null;hotX=hotY=.5f;notice="Cursor circular selecionado";}));
        sections[2].Add(MakeButton("IMPORTAR RASTRO PNG / SVG",0,220,235,()=>ImportImage(true)));
        sections[2].Add(MakeButton("RASTRO CIRCULAR",0,260,235,()=>{trailImage=null;trailArt=null;notice="Rastro circular selecionado";}));
        hotspotDialog=new Container { Position=new Vector2(270,86),Size=new Vector2(500,340),Alpha=0,Depth=-60 };root.Add(hotspotDialog);
        var page=sections[4];
        page.Add(Label("CONFIGURACOES",0,0,12,Muted));
        var theme=MakeButton("Tema: "+(preferences.DarkTheme?"escuro":"claro"),0,30,235,()=>{});
        theme.Action=ApplyTheme;page.Add(theme);
        var animation=MakeButton("Animacoes: "+(preferences.Animations?"ligadas":"desligadas"),0,78,235,()=>{});
        animation.Action=()=>{preferences=preferences with{Animations=!preferences.Animations};HubButton.Animations=preferences.Animations;animation.Text="Animacoes: "+(preferences.Animations?"ligadas":"desligadas");SavePreferences();};page.Add(animation);
        var history=MakeButton("Captura detalhada: "+(preferences.History?"ligada":"desligada"),0,126,235,()=>{});
        history.Action=()=>{preferences=preferences with{History=!preferences.History};history.Text="Captura detalhada: "+(preferences.History?"ligada":"desligada");SavePreferences();notice="Captura alterada. Reative Play para aplicar.";};page.Add(history);
        var budget=MakeButton("Particulas: "+preferences.Particles,0,174,235,()=>{});
        budget.Action=()=>{preferences=preferences with{Particles=preferences.Particles==256?512:preferences.Particles==512?1024:256};budget.Text="Particulas: "+preferences.Particles;SavePreferences();notice="Limite alterado. Reative Play para aplicar.";};page.Add(budget);
        page.Add(MakeButton("AJUSTAR HZ / ECONOMIA",0,222,235,()=>SelectSection(3)));
        page.Add(Label("Tema aplicado imediatamente.",0,273,10,Muted));
    }
    private void ApplyTheme()
    {
        int section=currentSection;
        preferences=preferences with {DarkTheme=!darkTheme}; SavePreferences();
        darkTheme=preferences.DarkTheme; HubButton.DarkTheme=darkTheme;
        Remove(panel,true);
        sizeBinding=new(size){MinValue=8,MaxValue=48};
        lifetimeBinding=new(lifetime){MinValue=60,MaxValue=600};
        rateBinding=new(selectedRate){MinValue=30,MaxValue=360};
        Add(panel=BuildInterface());
        panel.Scale=new Vector2(CursorHub.Core.InterfaceViewport.Fit(DrawWidth,DrawHeight).Scale);
        chrome?.SetTheme(darkTheme); SelectSection(section); UpdatePlayLabel();
        notice="Tema aplicado";nextStatus=0;
    }
    private void SavePreferences()
    {
        try{preferences.Save();notice="Preferencias salvas";}catch(Exception e){notice=e.Message;}nextStatus=0;
    }
    private void ImportImage(bool isTrail)
    {
        try
        {
            string? source=AssetPicker.Pick(CursorInput.FindWindow(null,windowTitle));if(source is null)return;
            string path=RasterAsset.Import(source);var art=RasterAsset.Load(path);_ = LoadSkin(path);
            if(isTrail){trailImage=path;trailArt=art;}else{cursorImage=path;cursorArt=art;hotX=hotY=.5f;OpenHotspot();}
            trail.Clear();notice="Imagem importada. Salve os ajustes ou um preset.";nextStatus=0;
        }
        catch(Exception e){notice="Importacao: "+e.Message;nextStatus=0;}
    }
    private void OpenHotspot()
    {
        if(cursorImage is null){notice="Importe uma imagem de cursor primeiro.";nextStatus=0;return;}
        hotspotDialog.Clear();hotspotDialog.Size=new Vector2(620,356);
        hotspotDialog.Add(new Box {RelativeSizeAxes=Axes.Both,Colour=darkTheme?new Color4(36,42,54,255):new Color4(237,242,247,255)});
        hotspotDialog.Add(Label("IMAGEM / TAMANHO / HOTSPOT",16,14,14));
        float x=hotX,y=hotY;int editSize=size,rotation=0;bool flipX=false,flipY=false;
        string original=cursorImage,transformed=original;
        var value=Label("",290,47,11,Muted);hotspotDialog.Add(value);
        var canvas=new HotspotCanvas(LoadSkin(original),x,y){Position=new Vector2(16,48)};
        void Readout(){value.Text=$"Clique: {Math.Round(x*(editSize-1))}, {Math.Round(y*(editSize-1))} px / {rotation} graus";}
        canvas.Changed=(a,b)=>{x=a;y=b;Readout();};Readout();hotspotDialog.Add(canvas);
        void Transform()
        {
            try {transformed=RasterAsset.Transform(original,rotation,flipX,flipY);canvas.SetTexture(LoadSkin(transformed));Readout();}
            catch(Exception e){notice="Transformacao: "+e.Message;nextStatus=0;}
        }
        var options=new Container {Position=new Vector2(290,78),Size=new Vector2(310,100)};hotspotDialog.Add(options);
        AddSlider(options,"Tamanho do ponteiro",0,editSize,8,48,v=>{editSize=v;Readout();},"px");
        hotspotDialog.Add(MakeButton("GIRAR -15",290,170,145,()=>{rotation=(rotation+345)%360;Transform();}));
        hotspotDialog.Add(MakeButton("GIRAR +15",445,170,155,()=>{rotation=(rotation+15)%360;Transform();}));
        hotspotDialog.Add(MakeButton("INVERTER X",290,212,145,()=>{flipX=!flipX;Transform();}));
        hotspotDialog.Add(MakeButton("INVERTER Y",445,212,155,()=>{flipY=!flipY;Transform();}));
        hotspotDialog.Add(MakeButton("CENTRALIZAR",290,254,310,()=>{x=y=.5f;canvas.Set(x,y);Readout();}));
        hotspotDialog.Add(Label("Clique ou arraste a mira",16,320,11,Muted));
        hotspotDialog.Add(MakeButton("APLICAR",290,302,145,()=>
        {
            cursorImage=transformed;cursorArt=RasterAsset.Load(transformed);hotX=x;hotY=y;sizeBinding.Value=editSize;
            hotspotDialog.Hide();trail.Clear();notice="Imagem e hotspot aplicados. Salve os ajustes.";nextStatus=0;
        }));
        hotspotDialog.Add(MakeButton("CANCELAR",445,302,155,()=>hotspotDialog.Hide()));hotspotDialog.Show();
    }
}
