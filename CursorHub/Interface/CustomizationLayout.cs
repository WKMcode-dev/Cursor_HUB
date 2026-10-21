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
        theme.Action=()=>{preferences=preferences with{DarkTheme=!preferences.DarkTheme};theme.Text="Tema: "+(preferences.DarkTheme?"escuro":"claro");SavePreferences();notice="Tema salvo. Reabra o app para aplicar.";nextStatus=0;};page.Add(theme);
        var animation=MakeButton("Animacoes: "+(preferences.Animations?"ligadas":"desligadas"),0,78,235,()=>{});
        animation.Action=()=>{preferences=preferences with{Animations=!preferences.Animations};HubButton.Animations=preferences.Animations;animation.Text="Animacoes: "+(preferences.Animations?"ligadas":"desligadas");SavePreferences();};page.Add(animation);
        var history=MakeButton("Captura detalhada: "+(preferences.History?"ligada":"desligada"),0,126,235,()=>{});
        history.Action=()=>{preferences=preferences with{History=!preferences.History};history.Text="Captura detalhada: "+(preferences.History?"ligada":"desligada");SavePreferences();notice="Captura alterada. Reative Play para aplicar.";};page.Add(history);
        var budget=MakeButton("Particulas: "+preferences.Particles,0,174,235,()=>{});
        budget.Action=()=>{preferences=preferences with{Particles=preferences.Particles==256?512:preferences.Particles==512?1024:256};budget.Text="Particulas: "+preferences.Particles;SavePreferences();notice="Limite alterado. Reative Play para aplicar.";};page.Add(budget);
        page.Add(MakeButton("AJUSTAR HZ / ECONOMIA",0,222,235,()=>SelectSection(3)));
        page.Add(Label("O tema e aplicado na proxima abertura.",0,273,10,Muted));
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
        hotspotDialog.Clear();
        hotspotDialog.Add(new Box {RelativeSizeAxes=Axes.Both,Colour=darkTheme?new Color4(36,42,54,255):new Color4(237,242,247,255)});
        hotspotDialog.Add(Label("HOTSPOT / PONTO DE CLIQUE",16,14,14));
        float x=hotX,y=hotY;
        var value=Label("",290,60,12,Muted);hotspotDialog.Add(value);
        var canvas=new HotspotCanvas(LoadSkin(cursorImage),x,y){Position=new Vector2(16,48)};
        void Readout(){value.Text=$"X: {Math.Round(x*255)} / Y: {Math.Round(y*255)}";}
        canvas.Changed=(a,b)=>{x=a;y=b;Readout();};Readout();hotspotDialog.Add(canvas);
        hotspotDialog.Add(Label("Clique ou arraste a mira",290,96,11,Muted));
        hotspotDialog.Add(MakeButton("CENTRALIZAR",290,140,190,()=>{x=y=.5f;canvas.Set(x,y);Readout();}));
        hotspotDialog.Add(MakeButton("APLICAR",290,196,190,()=>{hotX=x;hotY=y;hotspotDialog.Hide();notice="Hotspot aplicado. Salve os ajustes.";nextStatus=0;}));
        hotspotDialog.Add(MakeButton("CANCELAR",290,242,190,()=>hotspotDialog.Hide()));hotspotDialog.Show();
    }
}
