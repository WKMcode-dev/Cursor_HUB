using CursorHub.Interface;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using CursorHub.Windows;
using osuTK.Graphics;

namespace CursorHub;

public partial class CursorHubGame
{
    private PresetStore presets = new();
    private Container presetRows = null!, presetMenu = null!, presetEditor = null!;
    private int presetPage;
    private Guid? menuFor;

    private const int PresetsPerPage = 24;
    private Guid? demonstratedPreset;
    private SpriteText presetCounter = null!;

    private void InitializePresets(Container root)
    {
        sections[0].Position = new Vector2(264, 100);
        sections[0].Size = new Vector2(512, 344);
        sections[0].Add(presetRows = new Container { Size = new Vector2(512, 318) });
        var previous = MakeButton("", 0, 324, 28, () => { presetPage = Math.Max(0, presetPage - 1); RefreshPresets(); });
        var next = MakeButton("", 34, 324, 28, () => { presetPage = Math.Min(Math.Max(0, (presets.Items.Count) / PresetsPerPage), presetPage + 1); RefreshPresets(); });
        previous.Height = next.Height = 20;
        previous.Add(new Sprite {Texture=LoadSkin("page-previous.png"),RelativeSizeAxes=Axes.Both,Size=Vector2.One,Colour=Muted});
        next.Add(new Sprite {Texture=LoadSkin("page-next.png"),RelativeSizeAxes=Axes.Both,Size=Vector2.One,Colour=Muted});
        sections[0].Add(previous); sections[0].Add(next);
        sections[0].Add(presetCounter = Label("", 74, 329, 9, Muted));
        root.Add(presetMenu = new Container { Size = new Vector2(23.0625f, 72), Alpha = 0, Depth = -40 });
        root.Add(presetEditor = new Container { Position = new Vector2(264, 84), Size = new Vector2(512, 360), Alpha = 0, Depth = -50 });
        RefreshPresets();
    }

    private void RefreshPresets()
    {
        presetMenu.Alpha = 0;
        presetRows.Clear();
        int pages = CursorHub.Core.PresetPagination.PageCount(presets.Items.Count);
        presetPage = Math.Clamp(presetPage, 0, pages - 1);
        presetCounter.Text = $"{presets.Items.Count} PRESETS   /   {presetPage + 1:00} - {pages:00}";
        var page = presets.Items.Skip(presetPage * PresetsPerPage).Take(PresetsPerPage).ToArray();
        for (int cell = 0; cell < page.Length+(CursorHub.Core.PresetPagination.HasAdd(presets.Items.Count,presetPage)?1:0); cell++)
        {
            float x = (cell % 6) * 85, y = (cell / 6) * 82;
            if (cell < page.Length)
            {
                var preset = page[cell];
                var card = new Container { Position = new Vector2(x, y), Size = new Vector2(72, 72) };
                var button = MakeButton("", 0, 0, 72, () => { presetMenu.Alpha = 0; Demonstrate(preset); RefreshPresets(); });
                button.Height = 72;
                button.Skin = LoadSkin("preset-card.png");
                button.SkinColour = preset.Id == demonstratedPreset ? new Color4(221, 241, 243, 255) : Color4.White;
                card.Add(button);
                // The selected card previews current edits; other cards remain saved snapshots.
                var cfg=preset.Id==demonstratedPreset?CurrentSettings():preset.Settings;
                var colour=ResolveColour(cfg.Colour,cfg.CursorRgb);
                float diameter=8+(Math.Clamp(cfg.Size,8,48)-8)*.45f;
                float trailDiameter=8+(cfg.ResolvedTrailSize-8)*.45f;
                var trailColour=ResolveColour(cfg.ResolvedTrailColour,cfg.TrailRgb);
                for(int t=0;t<4;t++)
                {
                    float amount=(t+1)/5f;
                    card.Add(new Sprite {Name="trail-thumbnail",Texture=UserImage(cfg.TrailImage),Position=new Vector2(14+t*7,36-t*4),Origin=Anchor.Centre,Size=new Vector2(trailDiameter*(.25f+.65f*amount)),Colour=cfg.TrailImage is null?trailColour:Color4.White,Alpha=amount*.65f});
                }
                card.Add(new Sprite {Name="cursor-thumbnail",Texture=UserImage(cfg.CursorImage),Position=new Vector2(43,19),Origin=Anchor.Centre,Size=new Vector2(diameter),Colour=cfg.CursorImage is null?colour:Color4.White});
                var name = preset.Name.Length > 9 ? preset.Name[..8] + ".." : preset.Name;
                card.Add(Label(name, 6, 57, 8));
                int selectedCell = cell;
                var menu = MakeButton("", 55, 44, 16, () => OpenPresetMenu(preset, selectedCell));
                menu.Height = 26;
                menu.BorderThickness = 0;
                menu.Skin = LoadSkin("preset-menu.png");
                card.Add(menu);
                presetRows.Add(card);
            }
            else
            {
                var empty=new Container {Position=new Vector2(x,y),Size=new Vector2(72)};
                var add=MakeButton("",0,0,72,()=>OpenPresetEditor(null));add.Height=72;add.Skin=LoadSkin("preset-card.png");
                add.SkinColour=cell==page.Length?Color4.White:new Color4(220,228,236,210);empty.Add(add);
                var colour=Muted;
                empty.Add(new Box {Anchor=Anchor.Centre,Origin=Anchor.Centre,Size=new Vector2(12,1.5f),Colour=colour});
                empty.Add(new Box {Anchor=Anchor.Centre,Origin=Anchor.Centre,Size=new Vector2(1.5f,12),Colour=colour});
                if(cell==page.Length && presets.Items.Count<128)empty.Add(Label("NOVO",23,57,8,Muted));
                presetRows.Add(empty);
            }
        }
    }

    private void OpenPresetMenu(CursorPreset preset, float rowY)
    {
        if (menuFor == preset.Id && presetMenu.Alpha > 0) { presetMenu.Alpha = 0; return; }
        menuFor = preset.Id;
        presetMenu.Clear();
        int cell = (int)rowY;
        float tileX = 264 + (cell % 6) * 85;
        float menuX = tileX + 74;
        if (menuX + presetMenu.Width > 776) menuX = tileX - presetMenu.Width - 2;
        presetMenu.Position = new Vector2(menuX, 100 + (cell / 6) * 82);
        presetMenu.Add(new Sprite { Texture = LoadSkin("preset-toolbar.png"), RelativeSizeAxes = Axes.Both, Size = Vector2.One });
        void Item(string skin, int y, Action action)
        {
            var item = MakeButton("", 0, y, 23, action);
            item.Height = 24;
            item.Skin = LoadSkin(skin);
            presetMenu.Add(item);
        }
        Item("preset-trash.png", 0, () =>
        {
            try { presets.Remove(preset.Id); if (demonstratedPreset == preset.Id) demonstratedPreset = null; presetMenu.Alpha = 0; RefreshPresets(); notice = "Preset excluido"; nextStatus = 0; }
            catch (Exception e) { notice = e.Message; nextStatus = 0; }
        });
        Item("preset-edit.png", 24, () => { presetMenu.Alpha = 0; OpenPresetEditor(preset); });
        Item("preset-use.png", 48, () => { Demonstrate(preset); presetMenu.Alpha = 0; RefreshPresets(); });
        presetMenu.Alpha = 1;
    }

    private HubSettings CurrentSettings() => new(size, lifetime, selectedRate, colourIndex, economy, cursorImage, trailImage, hotX, hotY, trailSize, trailColourIndex,cursorRgb,trailRgb);

    private void Demonstrate(CursorPreset preset)
    {
        demonstratedPreset = preset.Id;
        ApplyWorkspaceSettings(preset.Settings);
        notice="Demonstrando: "+preset.Name;nextStatus=0;
    }

    private void ApplyWorkspaceSettings(HubSettings s)
    {
        cursorImage=s.CursorImage;trailImage=s.TrailImage;hotX=Math.Clamp(s.HotX,0,1);hotY=Math.Clamp(s.HotY,0,1);
        try{cursorArt=cursorImage is null?null:RasterAsset.Load(cursorImage);}catch{cursorImage=null;cursorArt=null;}
        try{trailArt=trailImage is null?null:RasterAsset.Load(trailImage);}catch{trailImage=null;trailArt=null;}
        cursorRgb=CursorHub.Core.ColourValue.Valid(s.CursorRgb);trailRgb=CursorHub.Core.ColourValue.Valid(s.TrailRgb);RefreshColourControls();
        sizeBinding.Value=Math.Clamp(s.Size,8,48);trailSizeBinding.Value=s.ResolvedTrailSize;
        lifetimeBinding.Value=Math.Clamp(s.Lifetime,60,600);colourIndex=Math.Clamp(s.Colour,0,3);trailColourIndex=s.ResolvedTrailColour;
        rateBinding.Value=Math.Clamp(s.Rate,30,360);economy=s.Economy;UpdateEconomyLabel();trail.Clear();ApplyRate();
    }

    private HubSettings presetDraft=new();
    private Guid? editingPresetId;
    private string draftName="";
    private HubTextBox? draftNameBox;
    private SpriteText? presetError;
    private int editorTab;
    private long presetEditRevision;
    private void ClosePresetEditor(){presetEditRevision++;presetEditor.Hide();if(hotspotDialog is not null)hotspotDialog.Hide();}
    private void AcceptPresetImage(long revision,bool isTrail,string path)
    {
        if(revision!=presetEditRevision||presetEditor.Alpha==0)return;
        presetDraft=isTrail?presetDraft with{TrailImage=path}:presetDraft with{CursorImage=path,HotX=.5f,HotY=.5f};
        RenderPresetEditor();OpenImageEditor(presetDraft,isTrail,result=>{if(revision!=presetEditRevision||presetEditor.Alpha==0)return;presetDraft=result;RenderPresetEditor();});
    }
    private void OpenPresetEditor(CursorPreset? existing)
    {
        presetEditRevision++;presetMenu.Hide();editingPresetId=existing?.Id;var original=existing?.Settings??CurrentSettings();presetDraft=original with{TrailSize=original.ResolvedTrailSize,TrailColour=original.ResolvedTrailColour};
        draftName=existing?.Name??$"Preset {presets.Items.Count+1}";draftNameBox=null;editorTab=0;
        RenderPresetEditor();presetEditor.Show();
    }
    private void RenderPresetEditor()
    {
        if(draftNameBox is not null)draftName=draftNameBox.Text;
        presetEditor.Clear();
        presetEditor.Add(new Box {RelativeSizeAxes=Axes.Both,Colour=darkTheme?new Color4(36,42,54,255):new Color4(238,241,246,255)});
        string[] tabs={"CURSOR","RASTRO","AJUSTES"};
        for(int i=0;i<3;i++){int tab=i;var button=MakeButton(tabs[i],16+i*162,12,156,()=>{editorTab=tab;RenderPresetEditor();});button.Height=28;button.TextColour=i==editorTab?Orange:Muted;presetEditor.Add(button);}
        presetEditor.Add(draftNameBox=new HubTextBox {Position=new Vector2(16,50),Size=new Vector2(480,32),Text=draftName,PlaceholderText="Nome do preset"});
        var options=new Container {Position=new Vector2(16,94),Size=new Vector2(480,212)};presetEditor.Add(options);
        if(editorTab<2)
        {
            bool isTrail=editorTab==1;
            AddSlider(options,isTrail?"Tamanho do rastro":"Tamanho do cursor",0,isTrail?presetDraft.ResolvedTrailSize:presetDraft.Size,8,48,v=>presetDraft=isTrail?presetDraft with{TrailSize=v}:presetDraft with{Size=v},"px");
            string? image=isTrail?presetDraft.TrailImage:presetDraft.CursorImage;
            if(image is null)
            {
                for(int i=0;i<palette.Length;i++){int colour=i;var swatch=MakeButton("",260,i*33,38,()=>{presetDraft=isTrail?presetDraft with{TrailColour=colour,TrailRgb=null}:presetDraft with{Colour=colour,CursorRgb=null};RenderPresetEditor();});swatch.BackgroundColour=palette[i];swatch.Height=26;options.Add(swatch);}
                options.Add(MakeButton("OUTRA COR",260,140,200,()=>
                {
                    long revision=presetEditRevision;
                    ChooseColour(Rgb(ResolveColour(isTrail?presetDraft.ResolvedTrailColour:presetDraft.Colour,isTrail?presetDraft.TrailRgb:presetDraft.CursorRgb)),value=>
                    {
                        if(revision!=presetEditRevision||presetEditor.Alpha==0)return;
                        presetDraft=isTrail?presetDraft with{TrailRgb=value}:presetDraft with{CursorRgb=value};RenderPresetEditor();
                    });
                }));
            }
            options.Add(MakeButton("IMPORTAR PNG / SVG",0,96,230,()=>
            {
                draftName=draftNameBox.Text;
                long revision=presetEditRevision;
                ChooseImage((path,_)=>AcceptPresetImage(revision,isTrail,path));
            }));
            options.Add(MakeButton(isTrail?"EDITAR IMAGEM":"IMAGEM / HOTSPOT",0,138,230,()=>OpenImageEditor(presetDraft,isTrail,result=>{presetDraft=result;RenderPresetEditor();})));
            options.Add(MakeButton("FORMATO CIRCULAR",0,180,230,()=>{presetDraft=isTrail?presetDraft with{TrailImage=null}:presetDraft with{CursorImage=null,HotX=.5f,HotY=.5f};RenderPresetEditor();}));
            options.Add(new Sprite {Texture=UserImage(image),Position=new Vector2(330,43),Size=new Vector2(100),Colour=image is null?ResolveColour(isTrail?presetDraft.ResolvedTrailColour:presetDraft.Colour,isTrail?presetDraft.TrailRgb:presetDraft.CursorRgb):Color4.White});
        }
        else
        {
            AddSlider(options,"Duracao do rastro",0,Math.Clamp(presetDraft.Lifetime,60,600),60,600,v=>presetDraft=presetDraft with{Lifetime=v},"ms");
            AddSlider(options,"Limite de desenho",92,Math.Clamp(presetDraft.Rate,30,360),30,360,v=>presetDraft=presetDraft with{Rate=v},"Hz");
            var economy=MakeButton("Economia: "+(presetDraft.Economy?"ligada":"desligada"),254,12,218,()=>{});
            economy.Action=()=>{presetDraft=presetDraft with{Economy=!presetDraft.Economy};economy.Text="Economia: "+(presetDraft.Economy?"ligada":"desligada");};options.Add(economy);
        }
        presetEditor.Add(MakeButton("SALVAR PRESET",16,318,230,CommitPresetEditor));
        presetEditor.Add(MakeButton("CANCELAR",266,318,230,ClosePresetEditor));
        presetEditor.Add(presetError=Label("",16,356,10,new Color4(220,85,95,255)));
    }
    private void CommitPresetEditor()
    {
        try
        {
            draftName=draftNameBox?.Text??draftName;
            var preset=new CursorPreset(editingPresetId??Guid.NewGuid(),draftName,presetDraft);
            presets.Put(preset);editingPresetId=preset.Id;demonstratedPreset=preset.Id;ApplyWorkspaceSettings(presetDraft);
            presetPage=Math.Max(0,presets.Items.FindIndex(p=>p.Id==preset.Id)/PresetsPerPage);
            ClosePresetEditor();SelectSection(0);RefreshPresets();notice="Preset e miniatura salvos";nextStatus=0;
            try { SaveWorkspace(CurrentSettings()); }
            catch(Exception e) { notice="Preset salvo; falha ao salvar ajustes: "+e.Message; }
        }
        catch(Exception e){notice="Nao foi possivel salvar: "+e.Message;if(presetError is not null)presetError.Text=e.Message;nextStatus=0;}
    }
}

