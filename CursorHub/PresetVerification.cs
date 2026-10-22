using System.Text.Json;
using CursorHub.Interface;
using CursorHub.Windows;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using SixLabors.ImageSharp;

namespace CursorHub;

public partial class CursorHubGame
{
    private uint verificationColourThread,verificationImageThread;
    public bool VerifyRestart {get;set;}
    private async Task VerifyClick(float x,float y)
    {
        await OnGameThread(()=>UiInputProbe.Move(CursorInput.FindWindow(null,windowTitle),x,y));
        await Task.Delay(120);await OnGameThread(UiInputProbe.Press);
        await Task.Delay(120);await OnGameThread(UiInputProbe.Release);await Task.Delay(150);
    }
    private async Task VerifyEventually(Func<bool> condition,string label)
    {
        for(int i=0;i<100;i++){bool done=false;await OnGameThread(()=>done=condition());if(done)return;await Task.Delay(100);}
        throw new Exception(label+" Estado: "+notice+"; importando="+importingImage+"; hotspot="+hotspotDialog.Alpha);
    }
    private void CheckThumbnail(uint cursorColour,uint trailColour)
    {
        var card=(Container)presetRows.Children.First();
        var sprites=card.Children.OfType<Sprite>().ToArray();
        if(Rgb(sprites.Single(s=>s.Name=="cursor-thumbnail").Colour)!=cursorColour ||
            sprites.Where(s=>s.Name=="trail-thumbnail").Any(s=>Rgb(s.Colour)!=trailColour))
            throw new Exception("Miniatura nao corresponde as cores configuradas.");
    }
    private async Task VerifyPresetSaving(List<string> results)
    {
        await OnGameThread(()=>{SelectSection(1);chrome!.ToggleMaximize();});await Task.Delay(400);
        await VerifyClick(120,459);
        await VerifyEventually(()=>presetEditor.Alpha>0,"Salvar sem selecao nao abriu novo preset.");
        await VerifyClick(640,335);
        await PickerProbe.CompleteColour(()=>Volatile.Read(ref verificationColourThread),0xE14B87);
        await VerifyEventually(()=>!choosingColour && presetDraft.CursorRgb==0xE14B87,"Cor nativa nao chegou ao novo preset.");
        await VerifyClick(520,110); // Trail tab.
        await VerifyClick(640,335);
        await PickerProbe.CompleteColour(()=>Volatile.Read(ref verificationColourThread),0x39C8A1);
        await VerifyEventually(()=>!choosingColour && presetDraft.TrailRgb==0x39C8A1,"Cor nativa nao chegou ao rastro do preset.");
        await VerifyClick(120,459); // The sidebar must save the open draft, not the workspace.
        await VerifyEventually(()=>presetEditor.Alpha==0 && currentSection==0 && presets.Items.Count==1,"Botao lateral nao salvou o novo preset.");
        await OnGameThread(()=>
        {
            CheckThumbnail(0xE14B87,0x39C8A1);
            var disk=new PresetStore(Path.Combine(VerificationDirectory!,"presets.json")).Items.Single();
            if(disk.Settings.CursorRgb!=0xE14B87 || disk.Settings.TrailRgb!=0x39C8A1)throw new Exception("RGB do novo preset nao persistiu.");
        });
        results.Add("PASS novo preset pelo botao lateral, dialogos RGB reais, miniaturas e persistencia");
        await VerifyClick(121,171);await VerifyClick(478,259);
        await PickerProbe.CompleteColour(()=>Volatile.Read(ref verificationColourThread),0x517AE2);
        await VerifyEventually(()=>!choosingColour && cursorRgb==0x517AE2,"Cor principal nao foi aplicada.");
        await VerifyClick(121,213);await VerifyClick(478,336);
        await PickerProbe.CompleteColour(()=>Volatile.Read(ref verificationColourThread),0xF0A833);
        await VerifyEventually(()=>!choosingColour && trailRgb==0xF0A833,"Cor principal do rastro nao foi aplicada.");
        await VerifyClick(121,129);await OnGameThread(()=>CheckThumbnail(0x517AE2,0xF0A833));
        await VerifyClick(120,459);
        await OnGameThread(()=>
        {
            var disk=new PresetStore(Path.Combine(VerificationDirectory!,"presets.json")).Items.Single();
            if(disk.Settings!=CurrentSettings())throw new Exception("Salvar ajustes perdeu campos do preset.");
            CheckThumbnail(0x517AE2,0xF0A833);
        });
        using(var shot=await Host.TakeScreenshotAsync())await shot.SaveAsPngAsync(Path.Combine(VerificationDirectory!,"07-custom-colour-thumbnail.png"));
        results.Add("PASS miniatura acompanha edicao de cursor/rastro antes e depois de salvar");
        await VerifyClick(121,171);await VerifyClick(478,259);
        await PickerProbe.CompleteColour(()=>Volatile.Read(ref verificationColourThread),null);
        await VerifyEventually(()=>!choosingColour,"Cancelar cor nao terminou.");
        await OnGameThread(()=>{if(cursorRgb!=0x517AE2)throw new Exception("Cancelar cor alterou o preset.");});
        await VerifyClick(380,341);
        await PickerProbe.CompleteImage(()=>Volatile.Read(ref verificationImageThread),Path.Combine(AssetPaths.Directory,"import-test.svg"));
        await VerifyEventually(()=>!importingImage && hotspotDialog.Alpha>0,"Importar SVG pelo dialogo nao abriu hotspot.");
        await VerifyClick(790,273); // Rotate +15.
        await VerifyClick(630,315); // Flip X.
        await VerifyClick(310,155); // Hotspot in canvas.
        await VerifyClick(632,405); // Apply image.
        await VerifyEventually(()=>hotspotDialog.Alpha==0 && cursorImage is not null,"Aplicar imagem nao concluiu.");
        await OnGameThread(()=>{if(cursorColours.Alpha!=0)throw new Exception("Imagem importada mostra seletor de cor.");});
        await VerifyClick(120,459);await VerifyClick(121,129);
        await OnGameThread(()=>
        {
            var card=(Container)presetRows.Children.First();var icon=card.Children.OfType<Sprite>().Single(s=>s.Name=="cursor-thumbnail");
            if(icon.Texture!=LoadSkin(cursorImage!) || Rgb(icon.Colour)!=0xFFFFFF)throw new Exception("Miniatura nao usa imagem original sem recolorir.");
            if(new PresetStore(Path.Combine(VerificationDirectory!,"presets.json")).Items.Single().Settings!=CurrentSettings())throw new Exception("Imagem/hotspot nao persistiram.");
        });
        results.Add("PASS importacao SVG pelo gerenciador, rotacao, inversao, hotspot e miniatura sem recolorir");
        await OnGameThread(()=>
        {
            presets.Remove(presets.Items.Single().Id);demonstratedPreset=null;ApplyWorkspaceSettings(new HubSettings());SelectSection(0);chrome!.ToggleMaximize();
        });await Task.Delay(300);
    }
    private void WriteRestartCheckpoint()
    {
        var item=presets.Items.First(p=>p.Name=="Custom Arrow");Demonstrate(item);SaveWorkspace(CurrentSettings());SelectSection(0);
        File.WriteAllText(Path.Combine(VerificationDirectory!,"restart-expected.json"),JsonSerializer.Serialize(item));
    }
    private async Task VerifyRestartedSession(List<string> results)
    {
        await OnGameThread(()=>
        {
            var expected=JsonSerializer.Deserialize<CursorPreset>(File.ReadAllText(Path.Combine(VerificationDirectory!,"restart-expected.json")))!;
            var restored=presets.Items.Single(p=>p.Id==expected.Id);
            if(restored!=expected || CurrentSettings()!=expected.Settings || demonstratedPreset!=expected.Id)
                throw new Exception("Reabertura nao restaurou preset, selecao e ajustes completos.");
            if(cursorImage is null || trailImage is null || cursorColours.Alpha!=0 || trailColours.Alpha!=0)
                throw new Exception("Reabertura perdeu as imagens ou exibiu seletores de cor.");
            results.Add("PASS processo reiniciado: preset completo, RGB, imagens, hotspot e selecao preservados");
        });
    }
}

