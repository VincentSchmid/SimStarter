using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace SimStarter.UI
{
    [ValueConversion(typeof(string), typeof(ImageSource))]
    public sealed class IconConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var path = value as string;
            return IconHelper.GetIcon(path);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
