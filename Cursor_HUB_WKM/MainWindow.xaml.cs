using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Cursor_HUB_WKM
{
    public partial class MainWindow : Window
    {
        private double particleSize = 32; // tamanho inicial
        private void TrailSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            particleSize = e.NewValue;
            if (trailSizeLabel != null)
                trailSizeLabel.Text = $"{(int)particleSize} px";
        }



        private bool trailActive = false;
        private string pngPath = "";
        private BitmapSource? spriteBitmap; // textura do rastro (pré-carregada e reutilizada)
        private readonly List<Particle> particles = new();
        private System.Drawing.Point lastPos = new System.Drawing.Point(-1, -1);

        // Parâmetros do rastro
        private const int MaxParticles = 150;           // limite máximo
        private const int LifetimeMs = 500;             // vida de cada partícula
        private const int MinMovePixels = 2;            // movimento mínimo para criar partícula

        // Host visual que desenha as partículas (sem criar controles por partícula)
        private TrailHost? trailHost;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            trailSizeLabel.Text = $"{(int)trailSizeSlider.Value} px";
            particleSize = trailSizeSlider.Value;

            var windowInteropHelper = new System.Windows.Interop.WindowInteropHelper(this);
            var source = System.Windows.Interop.HwndSource.FromHwnd(windowInteropHelper.Handle);
            source.AddHook(HwndHook);

            RegisterHotKey(windowInteropHelper.Handle, 1, MOD_CONTROL | MOD_SHIFT, KeyInterop.VirtualKeyFromKey(Key.X));
        }

        // Escolher PNG
        private void BtnChoosePng_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Imagens PNG|*.png"
            };
            if (dlg.ShowDialog() == true)
            {
                pngPath = dlg.FileName;
                txtPngPath.Text = pngPath;

                var img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.UriSource = new Uri(pngPath);
                img.EndInit();
                img.Freeze();
                spriteBitmap = img;
            }
        }

        // Ativar/Desativar pelo botão
        private void BtnToggleTrail_Click(object sender, RoutedEventArgs e)
        {
            if (!trailActive)
                ActivateTrail();
            else
                DeactivateTrail();
        }

        private void ActivateTrail()
        {
            trailActive = true;
            btnToggleTrail.Content = "Desativar Rastro";

            HubRoot.Visibility = Visibility.Collapsed;
            OverlayCanvas.Visibility = Visibility.Visible;

            // Expande a janela para tela cheia
            this.Left = 0;
            this.Top = 0;
            this.Width = SystemParameters.PrimaryScreenWidth;
            this.Height = SystemParameters.PrimaryScreenHeight;

            this.Background = Brushes.Transparent;
            this.Topmost = true;
            this.ShowInTaskbar = false;

            var hwnd = new WindowInteropHelper(this).Handle;
            WindowUtils.MakeWindowTransparent(hwnd);

            // Expande o canvas para cobrir a tela
            OverlayCanvas.Width = this.Width;
            OverlayCanvas.Height = this.Height;

            // Cria o host visual (uma vez)
            if (trailHost == null)
            {
                trailHost = new TrailHost(particles, () => spriteBitmap);
                trailHost.IsHitTestVisible = false;
                OverlayCanvas.Children.Add(trailHost);
            }

            // Ocupa toda a tela
            Canvas.SetLeft(trailHost, 0);
            Canvas.SetTop(trailHost, 0);
            trailHost.Width = OverlayCanvas.Width;
            trailHost.Height = OverlayCanvas.Height;

            // Inicia loop de render (por frame, sincronizado com GPU)
            CompositionTarget.Rendering += OnRenderFrame;
        }

        private void DeactivateTrail()
        {
            trailActive = false;
            btnToggleTrail.Content = "Ativar Rastro";

            HubRoot.Visibility = Visibility.Visible;
            OverlayCanvas.Visibility = Visibility.Collapsed;

            this.Background = Brushes.White;
            this.Topmost = false;
            this.ShowInTaskbar = true;

            this.Width = 666;
            this.Height = 250;
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var hwnd = new WindowInteropHelper(this).Handle;
            WindowUtils.RemoveWindowTransparent(hwnd);
        }

        // --- Atalho global (Ctrl+Shift+X) ---
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, int vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                if (id == 1) // nosso atalho
                {
                    if (trailActive)
                        DeactivateTrail();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        // Loop de frame sincronizado com a GPU
        private void OnRenderFrame(object? sender, EventArgs e)
        {
            // Captura posição do cursor (tela)
            NativeMethods.GetCursorPos(out var pos);

            // Cria partícula só se o mouse se moveu o suficiente
            if (Math.Abs(pos.X - lastPos.X) > MinMovePixels || Math.Abs(pos.Y - lastPos.Y) > MinMovePixels)
            {
                lastPos = pos;

                if (spriteBitmap != null)
                {
                    particles.Add(new Particle
                    {
                        X = pos.X,
                        Y = pos.Y,
                        CreatedAt = DateTime.UtcNow,
                        LifetimeMs = LifetimeMs,
                        Size = particleSize
                    });

                    // Limite de partículas
                    if (particles.Count > MaxParticles)
                        particles.RemoveRange(0, particles.Count - MaxParticles);
                }
            }

            // Atualiza e solicita render
            var now = DateTime.UtcNow;
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];
                double progress = (now - p.CreatedAt).TotalMilliseconds / p.LifetimeMs;
                if (progress >= 1.0)
                {
                    particles.RemoveAt(i);
                }
                else
                {
                    p.Opacity = 1.0 - progress;
                    particles[i] = p;
                }
            }


            trailHost?.InvalidateVisual();
        }

        // Partícula leve (sem controles WPF)
        public struct Particle
        {
            public double X;
            public double Y;
            public DateTime CreatedAt;
            public int LifetimeMs;
            public double Opacity;
            public double Size; // novo campo
        }

        public static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool GetCursorPos(out System.Drawing.Point lpPoint);
        }

        // Elemento que desenha diretamente via DrawingContext (sem criar Image controls)
        private sealed class TrailHost : FrameworkElement
        {
            private readonly List<Particle> particlesRef;
            private readonly Func<BitmapSource?> getSprite;

            public TrailHost(List<Particle> particles, Func<BitmapSource?> spriteAccessor)
            {
                particlesRef = particles;
                getSprite = spriteAccessor;
            }

            protected override void OnRender(DrawingContext dc)
            {
                var sprite = getSprite();
                if (sprite == null || particlesRef.Count == 0)
                    return;

                foreach (var p in particlesRef)
                {
                    if (p.Opacity <= 0) continue;

                    double half = p.Size / 2.0;
                    dc.PushOpacity(p.Opacity);
                    dc.DrawImage(sprite, new Rect(p.X - half, p.Y - half, p.Size, p.Size));
                    dc.Pop();
                }
            }
        }

        public static class WindowUtils
        {
            private const int GWL_EXSTYLE = -20;
            private const int WS_EX_TRANSPARENT = 0x20;
            private const int WS_EX_LAYERED = 0x80000;

            [DllImport("user32.dll", SetLastError = true)]
            private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

            [DllImport("user32.dll")]
            private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

            public static void MakeWindowTransparent(IntPtr hwnd)
            {
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            }

            public static void RemoveWindowTransparent(IntPtr hwnd)
            {
                int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                // remove apenas o WS_EX_TRANSPARENT, mantendo WS_EX_LAYERED
                extendedStyle &= ~WS_EX_TRANSPARENT;
                extendedStyle |= WS_EX_LAYERED;
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle);
            }
        }
    }
}