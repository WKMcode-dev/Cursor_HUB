using System.Diagnostics;
using CursorHub.Core;
using CursorHub.Interface;
using CursorHub.Windows;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osuTK;
using osuTK.Graphics;

namespace CursorHub;

public partial class CursorHubGame : Game
{
    private readonly TrailBuffer trail = new();
    private readonly Stopwatch clock = Stopwatch.StartNew();
    private readonly Circle[] dots = new Circle[256];
    private readonly string windowTitle = "Cursor Hub | " + Guid.NewGuid().ToString("N")[..8];
    private Container panel = null!;
    private Box background = null!;
    private Circle cursor = null!;
    private SpriteText status = null!;
    private CursorInput? input;
    private DesktopEffects? desktop;
    private bool playing, desktopWasActive;
    private string? reportedDesktopError;
    private string? reportedDesktopWarning;
    private WindowChrome? chrome;
    private bool economy = true;
    private bool enabled = true, idle;
    private double lastMovement, nextStatus;
    private System.Numerics.Vector2? lastPosition;
    private int drawnDots;
    private int selectedRate = 60, size = 18, lifetime = 180, colourIndex;
    private readonly Color4[] palette = { new(44, 137, 157, 255), new(206, 93, 163, 255), new(124, 105, 183, 255), new(37, 43, 55, 255) };
    private Color4 Accent => palette[colourIndex];

    public override void SetHost(osu.Framework.Platform.GameHost host)
    {
        base.SetHost(host);
        host.Window.Title = windowTitle;
    }

    [BackgroundDependencyLoader]
    private void Load(FrameworkConfigManager config)
    {
        config.SetValue(FrameworkSetting.FrameSync, FrameSync.Unlimited);
        config.SetValue(FrameworkSetting.WindowMode, WindowMode.Windowed);
        config.SetValue(FrameworkSetting.WindowedSize, new System.Drawing.Size(960, 600));
        var saved = HubSettings.Load();
        size = saved.Size; lifetime = saved.Lifetime; selectedRate = saved.Rate;
        colourIndex = saved.Colour; economy = saved.Economy;
        Add(background = new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black });
        Add(panel = BuildInterface());
        for (int i = 0; i < dots.Length; i++)
            Add(dots[i] = new Circle { Origin = Anchor.Centre, Alpha = 0, Depth = -10 });
        Add(cursor = new Circle { Origin = Anchor.Centre, Alpha = 0, Depth = -11 });
    }

    protected override void LoadComplete()
    {
        base.LoadComplete();
        input = new CursorInput(CursorInput.FindWindow(null, windowTitle));
        desktop = new DesktopEffects();
        try
        {
            chrome = new WindowChrome(CursorInput.FindWindow(null, windowTitle));
            chrome.Sync(Host.Window.ClientSize.Width, Host.Window.ClientSize.Height);
        }
        catch (Exception error) { notice = "Falha no recorte: " + error.Message; }
        lastMovement = clock.Elapsed.TotalMilliseconds;
        ApplyRate();
        if (VerificationDirectory is not null) _ = Verify();
    }

    private void ApplyRate()
    {
        // Overlay never has focus: inactive cap must match the selected rate.
        double rate = !enabled || economy && idle ? 15 : selectedRate;
        Host.MaximumUpdateHz = Host.MaximumDrawHz = Host.MaximumInactiveHz = rate;
    }

    private void TogglePlay()
    {
        if (playing)
        {
            desktop?.Stop(); playing = false; desktopWasActive = false;
            playButton.Text = "PLAY"; notice = "Cursor do Windows restaurando";
        }
        else
        {
            reportedDesktopError = null; reportedDesktopWarning = null;
            var c = Accent;
            desktop?.Play(new EffectOptions(size, lifetime, selectedRate, (byte)(c.R * 255), (byte)(c.G * 255), (byte)(c.B * 255)));
            playing = true;
            playButton.Text = "STOP"; notice = "Cursor global ativando / Ctrl+Shift+X para parar";
        }
        nextStatus = 0;
    }

    protected override void Update()
    {
        base.Update();
        double now = clock.Elapsed.TotalMilliseconds;
        chrome?.Sync();
        if (desktop?.Active == true)
        {
            if (!desktopWasActive) { notice = "Cursor e rastro globais ativos"; nextStatus = 0; }
            desktopWasActive = true;
        }
        if (desktop?.Warning is { } warning && warning != reportedDesktopWarning)
        {
            reportedDesktopWarning = warning;
            notice = "Rastro global ativo / cursor original mantido"; nextStatus = 0;
        }
        if (desktop?.Error is { } error && error != reportedDesktopError)
        {
            reportedDesktopError = error; playing = false; playButton.Text = "PLAY";
            notice = "Play indisponivel: " + error; nextStatus = 0;
        }
        else if (playing && desktopWasActive && desktop?.Active != true)
        {
            playing = false; desktopWasActive = false; playButton.Text = "PLAY";
            notice = "Cursor global parado"; nextStatus = 0;
        }
        var position = input?.Cursor(DrawWidth, DrawHeight);
        if (position is { } previewPoint)
        {
            float x = previewPoint.X * 800 / DrawWidth, y = previewPoint.Y * 500 / DrawHeight;
            var bounds = previewCard;
            if (x < bounds.X + 4 || x > bounds.X + bounds.Width - 4 || y < bounds.Y + 4 || y > bounds.Y + bounds.Height - 4 || presetEditor.Alpha > 0 || presetMenu.Alpha > 0) position = null;
        }
        bool moved = position != lastPosition;
        if (moved) lastMovement = now;
        lastPosition = position;
        bool shouldIdle = now - lastMovement > lifetime + 250;
        if (idle != shouldIdle) { idle = shouldIdle; ApplyRate(); }

        trail.Advance(enabled ? position : null, now, lifetime, Math.Max(2, size / 4f));
        int visibleDots = enabled ? trail.Count : 0;
        for (int i = 0; i < Math.Max(visibleDots, drawnDots); i++)
        {
            var dot = dots[i];
            if (!enabled || i >= trail.Count) { if (dot.Alpha != 0) dot.Alpha = 0; continue; }
            var sample = trail[i];
            float remaining = Math.Clamp(1 - (float)((now - sample.Born) / lifetime), 0, 1);
            dot.Position = new Vector2(sample.Position.X, sample.Position.Y);
            dot.Size = new Vector2(size * (DrawWidth / 800) * (0.25f + remaining * 0.65f));
            dot.Colour = Accent;
            dot.Alpha = remaining * 0.65f;
        }
        drawnDots = visibleDots;
        previewPlaceholder.Colour = Accent;
        previewPlaceholder.Alpha = position.HasValue || trail.Count > 0 ? 0 : 1;
        cursor.Alpha = enabled && position.HasValue ? 1 : 0;
        if (position is { } p)
        {
            cursor.Position = new Vector2(p.X, p.Y);
            cursor.Size = new Vector2(size * (DrawWidth / 800));
            cursor.Colour = Accent;
        }
        if (now >= nextStatus)
        {
            status.Text = $"{(notice.Length > 52 ? notice[..51] + ".." : notice)} / {(!enabled || economy && idle ? 15 : selectedRate)} Hz";
            nextStatus = now + 500;
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        desktop?.Dispose();
        chrome?.Dispose();
        base.Dispose(isDisposing);
    }
}

