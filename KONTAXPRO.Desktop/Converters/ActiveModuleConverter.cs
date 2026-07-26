using System;
using System.Globalization;
using System.Windows.Data;

namespace KONTAXPRO.Desktop.Converters
{
    public class ActiveModuleConverter : IValueConverter
    {
        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return string.Equals(
                value?.ToString(),
                parameter?.ToString(),
                StringComparison.OrdinalIgnoreCase);
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
}