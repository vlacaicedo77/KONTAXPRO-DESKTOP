using System.Globalization;
using System.Windows.Data;

namespace KONTAXPRO.Desktop.Converters;

public class BooleanToStatusTextConverter : IValueConverter
{
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        return value is true
            ? "Activo"
            : "Inactivo";
    }

    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}