using osu.Framework;
using System.Runtime.InteropServices;

namespace CursorHub;

internal static class Program
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(nint owner, string text, string title, uint type);
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--verify-overlay") { Windows.OverlayProbe.Run(Path.GetFullPath(args[1])); return; }
        if (args.Length == 3 && args[0] == "--cursor-guard" && int.TryParse(args[1], out int parent))
        {
            try { Windows.CursorController.RunGuard(parent, args[2]); } catch { }
            return;
        }
        using var singleInstance = new Mutex(true, "Local\\CursorHub-DesktopEffects", out bool firstInstance);
        if (!firstInstance)
        {
            MessageBox(0, "Ja existe uma instancia do Cursor Hub aberta. Feche a versao anterior (inclusive se estiver minimizada) e abra esta versao novamente.", "Cursor Hub - versao ja aberta", 0x40);
            return;
        }
        bool verification = args.Length == 2 && (args[0] == "--verify" || args[0] == "--verify-ui" || args[0] == "--verify-dark-ui");
        using var host = Host.GetSuitableDesktopHost("CursorHub-Prototype", new HostOptions { PortableInstallation = verification });
        using var game = new CursorHubGame();
        if (verification) { game.VerificationDirectory = Path.GetFullPath(args[1]); game.VerifyUiOnly = args[0] != "--verify"; game.ForceDarkVerification = args[0] == "--verify-dark-ui"; }
        host.Run(game);
    }
}
