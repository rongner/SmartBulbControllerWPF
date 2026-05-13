namespace SmartBulbControllerWPF.Helpers;

internal static class ColorHelper
{
    internal static (byte R, byte G, byte B) HsvToRgb(double h, double s, double v)
    {
        h = ((h % 360) + 360) % 360;
        if (s <= 0) { var c = (byte)(v * 255 + 0.5); return (c, c, c); }

        var sector = h / 60;
        var i = (int)sector;
        var f = sector - i;
        var p = v * (1 - s);
        var q = v * (1 - s * f);
        var t = v * (1 - s * (1 - f));

        var (r, g, b) = i switch
        {
            0 => (v, t, p),
            1 => (q, v, p),
            2 => (p, v, t),
            3 => (p, q, v),
            4 => (t, p, v),
            _ => (v, p, q),
        };
        return ((byte)(r * 255 + 0.5), (byte)(g * 255 + 0.5), (byte)(b * 255 + 0.5));
    }

    internal static (double H, double S, double V) RgbToHsv(byte r, byte g, byte b)
    {
        double rd = r / 255.0, gd = g / 255.0, bd = b / 255.0;
        var max   = Math.Max(rd, Math.Max(gd, bd));
        var min   = Math.Min(rd, Math.Min(gd, bd));
        var delta = max - min;

        double h = 0;
        if (delta > 0)
        {
            if      (max == rd) h = 60 * ((gd - bd) / delta % 6);
            else if (max == gd) h = 60 * ((bd - rd) / delta + 2);
            else                h = 60 * ((rd - gd) / delta + 4);
            if (h < 0) h += 360;
        }

        return (h, max == 0 ? 0 : delta / max, max);
    }
}
