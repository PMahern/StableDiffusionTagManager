using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace StableDiffusionTagManager.Converters
{
    public class ColorBrushConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var color = value as Color?;

            if(color != null)
            {
                return new SolidColorBrush(color.Value);
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
