using Avalonia.Data.Converters;
using SdWebUiApi;
using SdWebUpApi;
using System;
using System.Globalization;

namespace StableDiffusionTagManager.Converters
{
    public class InterrogateMethodToBooleanConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var convertedValue = value as InterrogateMethod?;
            var convertedParam = parameter as InterrogateMethod?;

            return convertedValue.HasValue && convertedParam.HasValue && convertedValue.Value == convertedParam.Value;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var convertedValue = value as bool?;
            var convertedParam = parameter as InterrogateMethod?;

            if(convertedValue.HasValue && convertedValue.Value && convertedParam.HasValue)
            {
                return convertedParam.Value;
            }
            return null;
        }
    }
}
