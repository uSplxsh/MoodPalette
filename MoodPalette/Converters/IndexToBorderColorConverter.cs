using System;
using System.Globalization;
using Microsoft.Maui.Graphics;

namespace MoodPalette.Converters
{
    public class IndexToBorderColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int selectedIndex && parameter is string parameterIndex && int.TryParse(parameterIndex, out int index))
            {
                return selectedIndex == index ? Colors.Black : Colors.Transparent;
            }
            return Colors.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}