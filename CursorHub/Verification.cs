using CursorHub.Core;
using System.Text.Json;
using CursorHub.Interface;
using CursorHub.Windows;
using SixLabors.ImageSharp;

namespace CursorHub;

public partial class CursorHubGame
{
    public string? VerificationDirectory { get; set; }
    public bool ForceDarkVerification {get;set;}
    public bool VerifyUiOnly { get; set; }
    private bool verifyPreviewPointer;
    private Task OnGameThread(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Scheduler.Add(() => { try { action(); completion.SetResult(); } catch (Exception e) { completion.SetException(e); } });
        return completion.Task;
    }
    private async Task VerifyNativeInput(List<string> results)
    {
        async Task ClickAt(float x,float y)
        {
            await OnGameThread(()=>UiInputProbe.Move(CursorInput.FindWindow(null,windowTitle),x,y));
            await Task.Delay(100);await OnGameThread(UiInputProbe.Press);
            await Task.Delay(100);await OnGameThread(UiInputProbe.Release);
        }
        async Task Click(float x,float y,Func<bool> expected,string label)
        {
            await ClickAt(x,y);
            for(int i=0;i<30;i++)
            {
                await Task.Delay(100);bool done=false;await OnGameThread(()=>done=expected());if(done){results.Add("PASS clique real: "+label);return;}
            }
            throw new Exception("Clique real nao concluiu: "+label);
        }
        await OnGameThread(()=>{ApplyWorkspaceSettings(new HubSettings());chrome!.ToggleMaximize();});await Task.Delay(400);
        await Click(121,171,()=>currentSection==1,"abrir CURSOR");
        await Click(485,207,()=>size==48,"slider do cursor ate 48px");
        await Click(121,213,()=>currentSection==2,"abrir RASTRO");
        await Click(265,197,()=>trailSize==8&&size==48,"rastro de 8px independente");
        await Click(121,129,()=>currentSection==0,"abrir PRESETS");
        await OnGameThread(()=>{if(presetRows.Children.Count()!=1)throw new Exception("Colecao vazia tem cards extras.");});
        await Click(300,136,()=>presetEditor.Alpha>0,"novo preset");
        await Click(395,419,()=>presetEditor.Alpha==0&&presets.Items.Count==1,"salvar novo preset");
        await Click(327,157,()=>presetMenu.Alpha>0,"menu do preset");
        await Click(350,136,()=>presetEditor.Alpha>0,"editar pelo menu");
        await ClickAt(520,150);await Task.Delay(150);
        await OnGameThread(()=>UiInputProbe.Key(0x11,false));await Task.Delay(100);
        await OnGameThread(()=>UiInputProbe.Key(0x41,false));await Task.Delay(100);
        await OnGameThread(()=>UiInputProbe.Key(0x41,true));await Task.Delay(100);
        await OnGameThread(()=>UiInputProbe.Key(0x11,true));await Task.Delay(100);
        await OnGameThread(()=>UiInputProbe.ReplaceText("E2E edited"));await Task.Delay(250);
        await OnGameThread(()=>{if(draftNameBox!.Text!="E2E edited")throw new Exception("Digitacao real nao chegou ao nome do preset: "+draftNameBox.Text);});
        await Click(395,419,()=>presetEditor.Alpha==0&&presets.Items[0].Name=="E2E edited","salvar edicao e nome digitado");
        await Click(327,157,()=>presetMenu.Alpha>0,"reabrir menu");
        await Click(350,160,()=>presetMenu.Alpha==0&&demonstratedPreset==presets.Items[0].Id,"usar preset");
        await Click(327,157,()=>presetMenu.Alpha>0,"menu para excluir");
        await Click(350,112,()=>presets.Items.Count==0&&presetRows.Children.Count()==1,"excluir e deixar somente NOVO");
        await OnGameThread(()=>{for(int i=0;i<24;i++)presets.Items.Add(new CursorPreset(Guid.NewGuid(),"Page "+i,CurrentSettings()));RefreshPresets();});
        await Click(312,434,()=>presetPage==1&&presetRows.Children.Count()==1,"pagina seguinte com unico NOVO");
        await Click(278,434,()=>presetPage==0&&presetRows.Children.Count()==24,"pagina anterior");
        await Click(1018,465,()=>desktop!.Playback.Snapshot.Requested,"Play pelo botao");
        for(int i=0;i<60&&desktop!.Playback.Snapshot.Phase!=PlaybackPhase.Playing;i++)await Task.Delay(100);
        if(desktop!.Playback.Snapshot.Phase!=PlaybackPhase.Playing)throw new Exception("Play real nao confirmou: "+desktop.Error);
        await Click(1018,465,()=>!desktop!.Playback.Snapshot.Requested,"Stop pelo botao");
        for(int i=0;i<60&&desktop!.Playback.Snapshot.Phase!=PlaybackPhase.Stopped;i++)await Task.Delay(100);
        if(desktop!.Playback.Snapshot.Phase!=PlaybackPhase.Stopped)throw new Exception("Stop real nao confirmou.");
        await OnGameThread(()=>{presets.Items.Clear();presetPage=0;demonstratedPreset=null;RefreshPresets();chrome!.ToggleMaximize();});await Task.Delay(300);
    }
    private async Task VerifyErrorCases(List<string> results)
    {
        await OnGameThread(()=>
        {
            var baseline=CurrentSettings();int count=presets.Items.Count;
            OpenPresetEditor(null);long old=presetEditRevision;ClosePresetEditor();
            AcceptPresetImage(old,false,Path.Combine(AssetPaths.Directory,"cursor.png"));
            if(presetEditor.Alpha!=0||hotspotDialog.Alpha!=0||CurrentSettings()!=baseline||presets.Items.Count!=count)throw new Exception("Importacao atrasada reabriu ou alterou editor cancelado.");
            OpenPresetEditor(null);draftNameBox!.Text="";CommitPresetEditor();
            if(presets.Items.Count!=count||presetEditor.Alpha==0)throw new Exception("Nome invalido foi salvo ou fechou editor.");ClosePresetEditor();
            OpenPresetEditor(null);draftNameBox!.Text="Retry after locked file";
            using(var locked=File.Open(Path.Combine(VerificationDirectory!,"presets.json"),FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                SaveSettings();
                if(presets.Items.Count!=count || presetEditor.Alpha==0 || string.IsNullOrWhiteSpace(presetError?.Text.ToString()))
                    throw new Exception("Falha de gravacao perdeu o rascunho ou ficou invisivel.");
            }
            SaveSettings();
            if(presets.Items.Count!=count+1 || presetEditor.Alpha!=0)throw new Exception("Salvar novamente apos falha nao funcionou.");
            presets.Remove(demonstratedPreset!.Value);demonstratedPreset=null;ApplyWorkspaceSettings(baseline);RefreshPresets();
            results.Add("PASS falha de gravacao preserva rascunho, mostra erro e permite salvar novamente");
            string invalid=Path.Combine(VerificationDirectory!,"invalid.png");File.WriteAllText(invalid,"not a PNG");
            bool rejected=false;try{RasterAsset.Import(invalid);}catch(InvalidOperationException){rejected=true;}
            if(!rejected)throw new Exception("PNG invalido nao foi recusado.");
            var snapshot=CurrentSettings();ApplyWorkspaceSettings(snapshot with{CursorImage=invalid});if(cursorImage is not null)throw new Exception("PNG invalido nao usou fallback.");
            _=UserImage(invalid);ApplyWorkspaceSettings(snapshot);
            cursorImage=null;cursorArt=null;RefreshColourControls();if(cursorColours.Alpha==0)throw new Exception("Bolinha perdeu controles de cor.");
            cursorRgb=0x12ABEF;trailRgb=0xEF7012;
            var rgb=CurrentSettings();if(Rgb(Accent)!=0x12ABEF||Rgb(TrailAccent)!=0xEF7012)throw new Exception("RGB nao aplicado a previa.");
            ApplyTheme();if(cursorRgb!=rgb.CursorRgb||trailRgb!=rgb.TrailRgb)throw new Exception("Tema perdeu cores personalizadas.");ApplyTheme();
            ApplyWorkspaceSettings(snapshot);SelectSection(0);
            results.Add("PASS cancelamento com importacao atrasada, nome invalido, PNG corrompido, fallback e cores apos troca de tema");
        });
    }
    private async Task Verify()
    {
        var results = new List<string>();
        Directory.CreateDirectory(VerificationDirectory!);
        async Task Capture(string name)
        {
            await Task.Delay(400);
            using var shot = await Host.TakeScreenshotAsync();
            await shot.SaveAsPngAsync(Path.Combine(VerificationDirectory!, name + ".png"));
            chrome!.VerifyNoBlackPixels(shot);
            results.Add(name + ": sem pixels pretos dentro da regiao visivel");
        }
        try
        {
            await Task.Delay(2000);
            if(VerifyRestart){await VerifyRestartedSession(results);return;}
            await VerifyNativeInput(results);
            await VerifyPresetSaving(results);
            await Capture("00-empty-collection");
            await OnGameThread(() =>
            {
                for (int i = 0; i < 24; i++)
                    presets.Items.Add(new CursorPreset(Guid.NewGuid(), $"Chain {i + 1:00}", CurrentSettings() with { Colour = i % 4 }));
                RefreshPresets();
            });
            await Capture("01-presets");
            await OnGameThread(()=>SelectSection(4));
            await Capture("01c-settings");
            await OnGameThread(()=> {int oldSize=size;bool old=darkTheme;ApplyTheme();if(darkTheme==old||size!=oldSize)throw new Exception("Tema nao aplicado ou perdeu tamanho");});
            await Capture("01e-theme-live");
            await OnGameThread(ApplyTheme);
            results.Add("Tema alternado ao vivo e ajustes preservados");
            await OnGameThread(()=>
            {
                _ = RasterAsset.Import(Path.Combine(AssetPaths.Directory,"import-test.svg"));
                cursorImage=RasterAsset.Import(Path.Combine(AssetPaths.Directory,"cursor.png"));
                cursorImage=RasterAsset.Transform(cursorImage,45,true,false);cursorArt=RasterAsset.Load(cursorImage);hotX=.15f;hotY=.2f;SelectSection(1);
                if(cursorColours.Alpha!=0)throw new Exception("Imagem importada ainda mostra controles de cor.");OpenHotspot();
            });
            await Capture("01d-hotspot");
            await OnGameThread(()=> {hotspotDialog.Hide();cursorImage=null;cursorArt=null;hotX=hotY=.5f;});
            results.Add("Importacao SVG/PNG e editor de hotspot executados");
            await OnGameThread(()=>
            {
                var icon=RasterAsset.Import(Path.Combine(AssetPaths.Directory,"import-test.svg"));
                var tail=RasterAsset.Import(Path.Combine(AssetPaths.Directory,"particle.png"));
                OpenPresetEditor(null);
                presetDraft=CurrentSettings() with{Size=40,TrailSize=8,CursorImage=icon,TrailImage=tail,HotX=.12f,HotY=.83f,Lifetime=450,Rate=165,Colour=2,TrailColour=1,CursorRgb=0x12ABEF,TrailRgb=0xEF7012};
                draftNameBox!.Text="Custom Arrow";RenderPresetEditor();
            });
            await Capture("01f-preset-cursor");
            await OnGameThread(()=>{editorTab=1;RenderPresetEditor();});
            await Capture("01g-preset-trail");
            await OnGameThread(()=>{editorTab=2;RenderPresetEditor();});
            await Capture("01h-preset-options");
            await OnGameThread(()=>
            {
                var expected=presetDraft;CommitPresetEditor();
                var saved=new PresetStore(Path.Combine(VerificationDirectory!,"presets.json")).Items.First(p=>p.Id==demonstratedPreset);
                if(saved.Settings!=expected)throw new Exception("Edicao completa perdeu campos ao salvar.");
                sizeBinding.Value=48;if(trailSize!=8)throw new Exception("Cursor ainda alterou tamanho do rastro.");
                trailSizeBinding.Value=24;if(size!=48)throw new Exception("Rastro ainda alterou tamanho do cursor.");
                Demonstrate(saved);if(cursorImage!=expected.CursorImage||trailImage!=expected.TrailImage||trailSize!=8||size!=40)throw new Exception("Preset nao restaurou imagens e tamanhos.");
                SelectSection(0);RefreshPresets();
            });
            await Capture("01i-custom-thumbnail");
            results.Add("Edicao completa preserva imagens, hotspot, tamanhos e cores independentes; preset recarregado do disco");
            await OnGameThread(() => SelectSection(3));
            await Capture("01b-performance");
            await OnGameThread(() => { rateBinding.Value = 165; if (selectedRate != 165) throw new Exception("165Hz nao aplicado"); SelectSection(0); });
            results.Add("Limite configuravel de 165Hz aplicado");
            await OnGameThread(() =>
            {
                var sample = new CursorPreset(Guid.NewGuid(), "Neon Pulse", CurrentSettings());
                presets.Items.Add(sample); RefreshPresets(); OpenPresetMenu(sample, 23);
            });
            await Capture("02-menu");
            await OnGameThread(() => { presetMenu.Alpha = 0; OpenPresetEditor(null); });
            await Capture("03-editor");
            await OnGameThread(() => { presetEditor.Alpha = 0; chrome!.Minimize(); });
            await Task.Delay(600);
            await OnGameThread(() => chrome!.Restore());
            await Capture("04-restored"); results.Add("Minimizar/restaurar executados");
            if (chrome!.SoftEdgePixels == 0) throw new Exception("Borda alfa ausente");
            results.Add($"Bordas com {chrome.SoftEdgePixels} pixels de alfa parcial");
            await OnGameThread(() => chrome!.VerifyCutouts()); results.Add("Regiao nativa preserva controles e vazios dos recortes");
            await OnGameThread(() => { sizeBinding.Value = 8; verifyPreviewPointer = true; });
            await Capture("05-cursor-8px-windowed");
            void CheckCursorPixels()
            {
                float pixelSize = cursor.DrawWidth / (input?.PixelScale ?? 1);
                if (Math.Abs(pixelSize - 8) > .01f) throw new Exception($"Cursor de 8px foi escalado para {pixelSize}");
                if (Math.Abs(panel.Scale.X - panel.Scale.Y) > .0001f) throw new Exception("Interface esticada");
                results.Add($"Cursor com {pixelSize} px fisicos; escala uniforme {panel.Scale.X:F3}");
            }
            await OnGameThread(CheckCursorPixels);
            await OnGameThread(() => chrome!.ToggleMaximize());
            await Capture("05-maximized");
            await OnGameThread(()=>{ApplyTheme();ApplyTheme();});
            results.Add("Tema alternado com janela maximizada sem perder restauracao");
            await OnGameThread(CheckCursorPixels);
            await OnGameThread(() => chrome!.ToggleMaximize());
            await Capture("06-unmaximized");
            await OnGameThread(() => { CheckCursorPixels(); verifyPreviewPointer = false; }); results.Add("Maximizar/restaurar executados");
            await VerifyErrorCases(results);
            await OnGameThread(WriteRestartCheckpoint);
            if (VerifyUiOnly) return;
            await OnGameThread(TogglePlay);
            await Task.Delay(1700);
            if (desktop?.Active != true)
            {
                results.Add("BLOCKED troca global: " + desktop?.Details);
                // Independently validate the alpha compositor without changing any system cursor.
                desktop!.Play(new EffectOptions(18, 180, 60, 255, 155, 32, false));
                await Task.Delay(600);
                if (!desktop.Active) throw new Exception("Compositor: " + desktop.Details);
                results.Add("Compositor alfa iniciado sem trocar cursor do sistema");
            }
            else
            {
                results.Add("Play global ativo / aviso: " + (desktop.Warning ?? "nenhum"));
                if (desktop.PresentedFrames == 0) throw new Exception("Rastro sem apresentacao nativa");
                await OnGameThread(() => chrome!.Minimize());
                await Task.Delay(500);
                if (!desktop.Active) throw new Exception("Rastro parou ao minimizar");
                results.Add($"Compositor ativo com janela minimizada; {desktop.PresentedFrames} apresentacoes");
                await OnGameThread(() => chrome!.Restore());
            }
            desktop!.Stop();
            await Task.Delay(600);
            if (desktop.Active) throw new Exception("Stop nao concluiu");
            results.Add("Stop concluiu; recursos liberados");
        }
        catch (Exception e) { results.Add("FAIL " + e); }
        finally
        {
            await File.WriteAllTextAsync(Path.Combine(VerificationDirectory!, VerifyRestart?"restart-results.json":"results.json"), JsonSerializer.Serialize(results));
            await OnGameThread(() => Exit());
        }
    }
}


