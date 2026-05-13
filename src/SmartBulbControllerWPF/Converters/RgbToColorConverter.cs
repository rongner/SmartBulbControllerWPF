using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SmartBulbControllerWPF.Converters;

public class RgbToColorConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length != 3) return Colors.White;
        var r = System.Convert.ToByte(values[0]);
        var g = System.Convert.ToByte(values[1]);
        var b = System.Convert.ToByte(values[2]);
        return Color.FromRgb(r, g, b);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
