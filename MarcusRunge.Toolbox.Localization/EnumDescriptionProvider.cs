using System.ComponentModel;
using System.Reflection;

namespace MarcusRunge.Toolbox.Localization
{
    /// <summary>
    /// Provides localized descriptions for enum values based on the DescriptionAttribute.
    /// </summary>
    public static class EnumDescriptionProvider
    {
        /// <summary>
        /// Gets the description of the specified enumeration value.
        /// </summary>
        /// <param name="value">The enumeration value.</param>
        /// <returns>
        /// The description from <see cref="DescriptionAttribute"/> if available;
        /// otherwise, the enum value name.
        /// </returns>
        public static string GetDescription(Enum value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var enumType = value.GetType();
            var name = Enum.GetName(enumType, value);

            if (name is null)
                return value.ToString();

            var fieldInfo = enumType.GetField(name);

            if (fieldInfo is null)
                return value.ToString();

            var attribute = fieldInfo.GetCustomAttributes<DescriptionAttribute>(false).FirstOrDefault();

            return attribute?.Description is string description && !string.IsNullOrWhiteSpace(description) ? description : value.ToString();
        }
    }
}