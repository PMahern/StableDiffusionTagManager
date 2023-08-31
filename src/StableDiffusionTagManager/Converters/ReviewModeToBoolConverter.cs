using Avalonia.Data.Converters;
using StableDiffusionTagManager.Views;
using System;
using System.Globalization;

namespace StableDiffusionTagManager.Converters
{
    public class ReviewModeToBoolConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var reviewMode = value as ImageReviewDialogMode?;
            var reviewModeParam = parameter as ImageReviewDialogMode?;

            if (reviewMode != null && reviewModeParam != null)
            {
                return reviewMode == reviewModeParam;
            }

            return false;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
