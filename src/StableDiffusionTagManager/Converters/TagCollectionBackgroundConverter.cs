using Avalonia.Data.Converters;
using Avalonia.Media;
using StableDiffusionTagManager.ViewModels;
using System;
using System.Globalization;

namespace StableDiffusionTagManager.Converters
{
    public class TagCollectionBackgroundConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var isSelected = value as bool?;
            if (isSelected.HasValue && isSelected.Value)
            {
                return new SolidColorBrush(new Color(255, 75, 75, 75));
            }
            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
