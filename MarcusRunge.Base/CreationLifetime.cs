using MarcusRunge.Base.Properties;
using MarcusRunge.Toolbox.Localization.Core;
using System.ComponentModel;

namespace MarcusRunge.Base
{
    /// <summary>
    /// Defines how instances created by <see cref="CreateableBindableBase{TInterface, TClass, TBase}"/>
    /// are cached and shared.
    /// </summary>
    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum CreationLifetime
    {
        /// <summary>
        /// Uses one process-wide instance for each closed generic creation type.
        /// </summary>
        [LocalizedDescription("CreationLifetimeGlobal", typeof(Resources))]
        Global,

        /// <summary>
        /// Uses one instance for each initialization context reference.
        /// </summary>
        [LocalizedDescription("CreationLifetimeScoped", typeof(Resources))]
        Scoped,

        /// <summary>
        /// Creates an independent instance for every call.
        /// </summary>
        [LocalizedDescription("CreationLifetimeTransient", typeof(Resources))]
        Transient
    }
}