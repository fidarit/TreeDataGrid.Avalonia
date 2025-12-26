using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Avalonia.Controls.Converters
{
    /// <summary>
    ///   Converts an integer indentation level to a <see cref="Thickness" /> value for use in
    ///   hierarchical data displays.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     IndentConverter is primarily used in the TreeDataGrid control to create appropriate left
    ///     margin indentation for hierarchical data. It multiplies the input indent level by 20
    ///     device-independent pixels to create a left margin, resulting in a visual tree structure.
    ///   </para>
    ///   <para>
    ///     This converter is typically used in templates for <see cref="Primitives.TreeDataGridExpanderCell" /> or
    ///     other controls that display hierarchical data where rows need different levels of
    ///     indentation based on their depth in the hierarchy.
    ///   </para>
    ///   <para>
    ///     Example usage in XAML:
    ///     <code>
    ///       &lt;Border Margin="{Binding Indent, Converter={x:Static converters:IndentConverter.Instance}}"/&gt;
    ///     </code>
    ///   </para>
    /// </remarks>
    public class IndentConverter : IValueConverter
    {
        /// <summary>
        ///   Gets a shared instance of the <see cref="IndentConverter" />.
        /// </summary>
        public static IndentConverter Instance { get; } = new IndentConverter();

        /// <summary>
        ///   Converts an integer indent value to a <see cref="Thickness" /> with the appropriate left margin.
        /// </summary>
        /// <param name="value">An integer representing the indentation level.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information (not used).</param>
        /// <returns>
        ///   A <see cref="Thickness" /> where the left value is set to 20 * indent and other values are 0.
        ///   Returns an empty <see cref="Thickness" /> if value is not an integer.
        /// </returns>
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is int indent)
            {
                return new Thickness(20 * indent, 0, 0, 0);
            }

            return new Thickness();
        }

        /// <summary>
        ///   Converts a thickness back to an integer indent value.
        /// </summary>
        /// <param name="value">The thickness to convert back.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">Optional parameter (not used).</param>
        /// <param name="culture">Culture information (not used).</param>
        /// <returns>
        ///   This operation is not supported and will throw <see cref="NotImplementedException" />.
        /// </returns>
        /// <exception cref="NotImplementedException">This method is not implemented.</exception>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
