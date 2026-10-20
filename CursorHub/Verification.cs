using System.Text.Json;
using CursorHub.Interface;
using CursorHub.Windows;
using SixLabors.ImageSharp;

namespace CursorHub;

public partial class CursorHubGame
{
    public string? VerificationDirectory { get; set; }
    public bool VerifyUiOnly { get; set; }
    private Task OnGameThread(Action action)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Scheduler.Add(() => { try { action(); completion.SetResult(); } catch (Exception e) { completion.SetException(e); } });
        return completion.Task;
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
            await Capture("01-presets");
            await OnGameThread(() =>
            {
                var sample = new CursorPreset(Guid.NewGuid(), "Neon Pulse", CurrentSettings());
                presets.Items.Add(sample); RefreshPresets(); OpenPresetMenu(sample, 0);
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
            await OnGameThread(() => chrome!.ToggleMaximize());
            await Capture("05-maximized");
            await OnGameThread(() => chrome!.ToggleMaximize());
            await Capture("06-unmaximized"); results.Add("Maximizar/restaurar executados");
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
            await File.WriteAllTextAsync(Path.Combine(VerificationDirectory!, "results.json"), JsonSerializer.Serialize(results));
            await OnGameThread(() => Exit());
        }
    }
}

