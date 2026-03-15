using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    public class IndentConverter : IValueConverter
    {
        /// <summary>
        /// Singleton instance
        /// </summary>
        public static IndentConverter Instance { get; } = new IndentConverter();

        /// <inheritdoc cref="IndentConverter"/>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int indent)
            {
                return new Thickness(20 * indent, 0, 0, 0);
            }

            return new Thickness();
        }

        /// <summary>
        /// Convert-back is not supported (one-way binding only).
        /// </summary>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
