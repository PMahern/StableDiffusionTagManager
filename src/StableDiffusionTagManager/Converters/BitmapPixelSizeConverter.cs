using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;


namespace StableDiffusionTagManager.Converters
{
    internal class BitmapPixelSizeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var bitmap = value as Bitmap;
            var pixelSize = bitmap?.PixelSize;

            if (pixelSize != null)
            {
                return $"{pixelSize.Value.Width}w x {pixelSize.Value.Height}h";
            }

            return "";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
