using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SmartBulbControllerWPF.Helpers;

namespace SmartBulbControllerWPF.Views;

public partial class ColorWheelPicker : UserControl
{
    private const int    Size   = 180;
    private const double Radius = Size / 2.0;

    private bool _isDragging;
    private bool _suppressUpdate;

    // ── Dependency properties ─────────────────────────────────────────────────

    public static readonly DependencyProperty HueProperty = DependencyProperty.Register(
        nameof(Hue), typeof(double), typeof(ColorWheelPicker),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnHueSatChanged));

    public static readonly DependencyProperty SaturationProperty = DependencyProperty.Register(
        nameof(Saturation), typeof(double), typeof(ColorWheelPicker),
        new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnHueSatChanged));

    public double Hue
    {
        get => (double)GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    public double Saturation
    {
        get => (double)GetValue(SaturationProperty);
        set => SetValue(SaturationProperty, value);
    }

    // ── Init ──────────────────────────────────────────────────────────────────

    public ColorWheelPicker()
    {
        InitializeComponent();
        Loaded += (_, _) => { RenderWheel(); UpdateSelector(); };
    }

    private static void OnHueSatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var wheel = (ColorWheelPicker)d;
        if (!wheel._suppressUpdate) wheel.UpdateSelector();
    }

    // ── Wheel rendering ───────────────────────────────────────────────────────

    private void RenderWheel()
    {
        var bmp    = new WriteableBitmap(Size, Size, 96, 96, PixelFormats.Bgra32, null);
        var pixels = new int[Size * Size];

        for (int y = 0; y < Size; y++)
        {
            for (int x = 0; x < Size; x++)
            {
                double dx   = x - Radius, dy = y - Radius;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                if (dist > Radius) { pixels[y * Size + x] = 0; continue; }

                double hue = Math.Atan2(dy, dx) * 180 / Math.PI + 180;
                double sat = dist / Radius;
                var (r, g, b) = ColorHelper.HsvToRgb(hue, sat, 1.0);

                // BGRA32: B | (G<<8) | (R<<16) | (A<<24)
                pixels[y * Size + x] = (int)((uint)b | ((uint)g << 8) | ((uint)r << 16) | 0xFF000000u);
            }
        }

        bmp.WritePixels(new Int32Rect(0, 0, Size, Size), pixels, Size * 4, 0);
        WheelImage.Source = bmp;
    }

    // ── Selector positioning ──────────────────────────────────────────────────

    private void UpdateSelector()
    {
        double angle = (Hue - 180) * Math.PI / 180;
        double x     = Radius + Math.Cos(angle) * Saturation * Radius;
        double y     = Radius + Math.Sin(angle) * Saturation * Radius;

        Canvas.SetLeft(SelectorOuter, x - SelectorOuter.Width  / 2);
        Canvas.SetTop (SelectorOuter, y - SelectorOuter.Height / 2);
        Canvas.SetLeft(SelectorInner, x - SelectorInner.Width  / 2);
        Canvas.SetTop (SelectorInner, y - SelectorInner.Height / 2);
    }

    // ── Mouse interaction ─────────────────────────────────────────────────────

    private void UpdateFromPoint(Point p)
    {
        double dx   = p.X - Radius, dy = p.Y - Radius;
        double dist = Math.Sqrt(dx * dx + dy * dy);

        if (dist > Radius) { dx *= Radius / dist; dy *= Radius / dist; dist = Radius; }

        _suppressUpdate = true;
        Hue        = ((Math.Atan2(dy, dx) * 180 / Math.PI + 180) % 360 + 360) % 360;
        Saturation = Math.Min(dist / Radius, 1.0);
        _suppressUpdate = false;

        UpdateSelector();
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = true;
        WheelCanvas.CaptureMouse();
        UpdateFromPoint(e.GetPosition(WheelCanvas));
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging) UpdateFromPoint(e.GetPosition(WheelCanvas));
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        WheelCanvas.ReleaseMouseCapture();
    }
}
