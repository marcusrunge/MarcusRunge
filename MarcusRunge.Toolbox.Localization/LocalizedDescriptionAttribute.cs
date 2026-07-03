using System.ComponentModel;
using System.Resources;

namespace MarcusRunge.Toolbox.Localization
{
    /// <summary>
    /// Provides a localized description for an enumeration value,
    /// allowing for easy localization of enum descriptions using resource files.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private readonly string _resourceKey;
        private readonly ResourceManager _resourceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedDescriptionAttribute"/> class.
        /// </summary>
        /// <param name="resourceKey">The resource key.</param>
        /// <param name="resourceType">Type of the resource.</param>
        /// <exception cref="ArgumentException">Resource key must not be null or whitespace. - resourceKey</exception>
        /// <exception cref="ArgumentNullException"></exception>
        public LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceKey))
                throw new ArgumentException("Resource key must not be null or whitespace.", nameof(resourceKey));

            ArgumentNullException.ThrowIfNull(resourceType);

            _resourceKey = resourceKey;
            _resourceManager = new ResourceManager(resourceType);
        }

        /// <summary>
        /// Gets the description stored in this attribute.
        /// </summary>
        public override string Description
        {
            get
            {
                var value = _resourceManager.GetString(_resourceKey);

                return string.IsNullOrWhiteSpace(value) ? $"[[{_resourceKey}]]" : value;
            }
        }
    }
}