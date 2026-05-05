using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace PdfGlown.UI.Aval.Sample
{
    public class PercentConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            if (value is float floatValue)
                return floatValue.ToString("p0");
            return "100%";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
        {
            var floatValue = value is string stringValue && float.TryParse(stringValue.TrimEnd('%'), out var result) ? result / 100F : 1F;
            return floatValue;
        }
    }
}
