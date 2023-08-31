using Avalonia.Data.Converters;
using SdWebUpApi;
using System;
using System.Globalization;

namespace StableDiffusionTagManager.Converters
{
    internal class MaskedContentToStringConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var maskedContent = value as MaskedContent?;

            if(maskedContent != null)
            {
                switch(maskedContent)
                {
                    case MaskedContent.LatentNoise:
                        return "Latent Noise";
                    case MaskedContent.LatentNothing:
                        return "Latent Nothing";
                    case MaskedContent.Original:
                        return "Original";
                    case MaskedContent.Fill:
                        return "Fill";
                }
            }

            return null;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
