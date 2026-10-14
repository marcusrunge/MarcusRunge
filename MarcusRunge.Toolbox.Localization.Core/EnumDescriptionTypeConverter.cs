using System.ComponentModel;
using System.Globalization;

namespace MarcusRunge.Toolbox.Localization.Core
{
    /// <summary>
    /// Converts enumeration values to their localized descriptions when a
    /// <see cref="DescriptionAttribute"/> is available.
    /// </summary>
    public sealed class EnumDescriptionTypeConverter(Type type) : EnumConverter(type)
    {
        /// <inheritdoc/>
        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (destinationType is null) throw new ArgumentNullException(nameof(destinationType));
            return destinationType == typeof(string) && value is Enum enumValue ? EnumDescriptionProvider.GetDescription(enumValue, culture) : base.ConvertTo(context, culture, value, destinationType);
        }
    }
}