using System.Windows.Markup;

namespace MarcusRunge.Toolbox.Localization.Wpf
{
    /// <summary>
    /// Provides the declared values of an enumeration as an items source for WPF bindings.
    /// </summary>
    public sealed class EnumBindingSourceExtension : MarkupExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumBindingSourceExtension"/> class.
        /// </summary>
        /// <param name="enumType">
        /// The enumeration type. Nullable enumeration types are accepted and resolved to their
        /// underlying enumeration type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="enumType"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="enumType"/> is not an enumeration type.
        /// </exception>
        public EnumBindingSourceExtension(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);
            var actualEnumType = Nullable.GetUnderlyingType(enumType) ?? enumType;
            if (!actualEnumType.IsEnum) throw new ArgumentException("The supplied type must be an enumeration type.", nameof(enumType));
            EnumType = actualEnumType;
        }

        /// <summary>
        /// Gets the non-nullable enumeration type whose declared values are returned.
        /// </summary>
        public Type EnumType { get; }

        /// <summary>
        /// Returns the declared values of <see cref="EnumType"/>.
        /// </summary>
        /// <param name="serviceProvider">
        /// A service provider that can provide services for the markup extension.
        /// </param>
        /// <returns>An array containing the declared enumeration values.</returns>
        public override object ProvideValue(IServiceProvider serviceProvider) => Enum.GetValues(EnumType);
    }
}