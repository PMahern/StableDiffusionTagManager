using Avalonia.Data.Converters;
using StableDiffusionTagManager.Controls;
using System;
using System.Globalization;

namespace StableDiffusionTagManager.Converters
{
    public class ImageViewerModeToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var convertedValue = value as ImageViewerMode?;
            var convertedParam = parameter as ImageViewerMode?;

            return convertedValue.HasValue && convertedParam.HasValue && convertedValue.Value == convertedParam.Value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
