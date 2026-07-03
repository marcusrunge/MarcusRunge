using System.Windows.Markup;

namespace MarcusRunge.Toolbox.Localization
{
    /// <summary>
    /// Provides enum values as an ItemsSource for WPF bindings.
    /// </summary>
    public sealed class EnumBindingSourceExtension : MarkupExtension
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnumBindingSourceExtension"/> class.
        /// </summary>
        /// <param name="enumType">Type of the enum.</param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException">Type must be an enum type. - enumType</exception>
        public EnumBindingSourceExtension(Type enumType)
        {
            ArgumentNullException.ThrowIfNull(enumType);

            var actualEnumType = Nullable.GetUnderlyingType(enumType) ?? enumType;

            if (!actualEnumType.IsEnum)
                throw new ArgumentException("Type must be an enum type.", nameof(enumType));

            EnumType = actualEnumType;
        }

        /// <summary>
        /// Gets the type of the enum.
        /// </summary>
        /// <value>
        /// The type of the enum.
        /// </value>
        public Type EnumType { get; }

        /// <summary>
        /// When implemented in a derived class, returns an object that is provided as the value of the target property for this markup extension.
        /// </summary>
        /// <param name="serviceProvider">A service provider helper that can provide services for the markup extension.</param>
        /// <returns>
        /// The object value to set on the property where the extension is applied.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider) => Enum.GetValues(EnumType);
    }
}