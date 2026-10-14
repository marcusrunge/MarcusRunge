using MarcusRunge.Toolbox.Localization.Core;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace MarcusRunge.Toolbox.Localization.Wpf
{
    /// <summary>
    /// Converts enumeration values to their localized descriptions when a description attribute is present.
    /// </summary>
    public sealed class EnumDescriptionValueConverter : IValueConverter
    {
        /// <summary>
        /// Converts an enumeration value to its localized description.
        /// </summary>
        /// <param name="value">The value produced by the binding source.</param>
        /// <param name="targetType">The type of the binding target property.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use when resolving the localized description.</param>
        /// <returns>
        /// The localized description for an enumeration value, the original value for a non-enumeration
        /// value, or <see cref="DependencyProperty.UnsetValue"/> when the binding value is unset.
        /// </returns>
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (ReferenceEquals(value, DependencyProperty.UnsetValue)) return DependencyProperty.UnsetValue;
            return value is Enum enumValue ? EnumDescriptionProvider.GetDescription(enumValue, culture) : value;
        }

        /// <summary>
        /// Returns <see cref="Binding.DoNothing"/> because conversion from localized descriptions
        /// to enumeration values is not supported.
        /// </summary>
        /// <param name="value">The value produced by the binding target.</param>
        /// <param name="targetType">The type to convert to.</param>
        /// <param name="parameter">The converter parameter to use.</param>
        /// <param name="culture">The culture to use in the converter.</param>
        /// <returns><see cref="Binding.DoNothing"/>.</returns>
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => Binding.DoNothing;
    }
}