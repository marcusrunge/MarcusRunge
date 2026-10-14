using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace MarcusRunge.Toolbox.Localization.Core
{
    /// <summary>
    /// Provides a localized description for an enumeration value using a resource file.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class LocalizedDescriptionAttribute : DescriptionAttribute
    {
        private static readonly ConcurrentDictionary<Type, ResourceManager> _resourceManagers = new();

        private readonly string _resourceKey;
        private readonly ResourceManager _resourceManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalizedDescriptionAttribute"/> class.
        /// </summary>
        /// <param name="resourceKey">The resource key used to resolve the localized description.</param>
        /// <param name="resourceType">The strongly typed resource class.</param>
        /// <exception cref="ArgumentException">
        /// <paramref name="resourceKey"/> is empty or consists only of whitespace.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="resourceType"/> is <see langword="null"/>.
        /// </exception>
        public LocalizedDescriptionAttribute(string resourceKey, Type resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceKey)) throw new ArgumentException("The resource key must not be empty or consist only of whitespace.", nameof(resourceKey));
            if (resourceType is null) throw new ArgumentNullException(nameof(resourceType));
            _resourceKey = resourceKey;
            _resourceManager = _resourceManagers.GetOrAdd(resourceType, static type => new ResourceManager(type));
        }

        /// <summary>
        /// Gets the localized description for the current user-interface culture.
        /// </summary>
        /// <remarks>
        /// A visible resource-key marker is returned when the resource cannot be resolved.
        /// </remarks>
        public override string Description => GetDescription(CultureInfo.CurrentUICulture);

        /// <summary>
        /// Gets the localized description for the specified culture.
        /// </summary>
        /// <param name="culture">
        /// The culture used to resolve the resource. When <see langword="null"/>,
        /// <see cref="CultureInfo.CurrentUICulture"/> is used.
        /// </param>
        /// <returns>
        /// The localized description, or a visible resource-key marker when the resource cannot be resolved.
        /// </returns>
        public string GetDescription(CultureInfo? culture)
        {
            try
            {
                var value = _resourceManager.GetString(_resourceKey, culture ?? CultureInfo.CurrentUICulture);
                return string.IsNullOrWhiteSpace(value) ? GetMissingResourceMarker() : value;
            }
            catch (MissingManifestResourceException)
            {
                return GetMissingResourceMarker();
            }
            catch (MissingSatelliteAssemblyException)
            {
                return GetMissingResourceMarker();
            }
        }

        private string GetMissingResourceMarker() => $"[[{_resourceKey}]]";
    }
}