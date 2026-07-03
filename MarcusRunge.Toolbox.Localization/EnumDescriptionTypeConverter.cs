using System.ComponentModel;
using System.Globalization;

namespace MarcusRunge.Toolbox.Localization
{
    /// <summary>
    /// Converts enum values to their localized description if a DescriptionAttribute is present.
    /// </summary>
    public sealed class EnumDescriptionTypeConverter(Type type) : EnumConverter(type)
    {
        /// <summary>
        /// Converts the given value object to the specified destination type.
        /// </summary>
        /// <param name="context">An <see cref="T:System.ComponentModel.ITypeDescriptorContext" /> that provides a format context.</param>
        /// <param name="culture">An optional <see cref="T:System.Globalization.CultureInfo" />. If not supplied, the current culture is assumed.</param>
        /// <param name="value">The <see cref="T:System.Object" /> to convert.</param>
        /// <param name="destinationType">The <see cref="T:System.Type" /> to convert the value to.</param>
        /// <returns>
        /// An <see cref="T:System.Object" /> that represents the converted <paramref name="value" />.
        /// </returns>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType == typeof(string) && value is Enum enumValue)
            {
                return EnumDescriptionProvider.GetDescription(enumValue);
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}