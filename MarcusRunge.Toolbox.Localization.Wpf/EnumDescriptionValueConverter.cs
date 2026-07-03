using MarcusRunge.Toolbox.Localization.Core;
using System.Globalization;
using System.Windows.Data;

namespace MarcusRunge.Toolbox.Localization.Wpf
{
    /// <summary>
    /// Converts enum values to their localized description if a DescriptionAttribute is present.
    /// </summary>
    /// <seealso cref="IValueConverter" />
    public sealed class EnumDescriptionValueConverter : IValueConverter
    {
        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <see langword="null" />, the valid null value is used.
        /// </returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is Enum enumValue ? EnumDescriptionProvider.GetDescription(enumValue) : value;

        /// <summary>
        /// Converts a value.
        /// </summary>
        /// <param name="value">The value that is produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns>
        /// A converted value. If the method returns <see langword="null" />, the valid null value is used.
        /// </returns>
        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }
}