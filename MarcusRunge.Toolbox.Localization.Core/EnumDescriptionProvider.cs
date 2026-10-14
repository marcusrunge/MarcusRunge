using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace MarcusRunge.Toolbox.Localization.Core
{
    /// <summary>
    /// Provides localized descriptions for enumeration values based on <see cref="DescriptionAttribute"/>.
    /// </summary>
    public static class EnumDescriptionProvider
    {
        /// <summary>
        /// Gets the description of the specified enumeration value for the current user-interface culture.
        /// </summary>
        /// <param name="value">The enumeration value.</param>
        /// <returns>
        /// The localized description when available; otherwise, the textual representation of
        /// <paramref name="value"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static string GetDescription(Enum value) => GetDescription(value, CultureInfo.CurrentUICulture);

        /// <summary>
        /// Gets the description of the specified enumeration value for the specified culture.
        /// </summary>
        /// <param name="value">The enumeration value.</param>
        /// <param name="culture">
        /// The culture used to resolve localized descriptions. When <see langword="null"/>,
        /// <see cref="CultureInfo.CurrentUICulture"/> is used.
        /// </param>
        /// <returns>
        /// The localized description when available; otherwise, the textual representation of
        /// <paramref name="value"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        public static string GetDescription(Enum value, CultureInfo? culture)
        {
            if (value is null) throw new ArgumentNullException(nameof(value));

            var enumType = value.GetType();
            var valueName = Enum.GetName(enumType, value);
            if (valueName is not null) return GetFieldDescription(enumType, valueName, culture) ?? valueName;
            if (!enumType.IsDefined(typeof(FlagsAttribute), false)) return value.ToString();
            var names = value.ToString().Split([','], StringSplitOptions.RemoveEmptyEntries);
            if (names.Length == 0) return value.ToString();
            var descriptions = new string[names.Length];
            for (var index = 0; index < names.Length; index++)
            {
                var name = names[index].Trim();
                descriptions[index] = GetFieldDescription(enumType, name, culture) ?? name;
            }

            return string.Join(", ", descriptions);
        }

        private static string? GetFieldDescription(Type enumType, string fieldName, CultureInfo? culture)
        {
            var fieldInfo = enumType.GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
            var attribute = fieldInfo?.GetCustomAttribute<DescriptionAttribute>(false);
            if (attribute is LocalizedDescriptionAttribute localizedAttribute) return localizedAttribute.GetDescription(culture);
            return attribute?.Description is string description && !string.IsNullOrWhiteSpace(description) ? description : null;
        }
    }
}