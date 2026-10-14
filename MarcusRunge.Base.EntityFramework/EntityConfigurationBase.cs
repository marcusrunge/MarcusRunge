using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarcusRunge.Base.EntityFramework
{
    /// <summary>
    /// Provides a base class for entity configurations with database-generated identity
    /// and optimistic concurrency support.
    /// </summary>
    /// <typeparam name="TEntity">The type of the entity.</typeparam>
    /// <typeparam name="TConfiguration">The concrete entity configuration type.</typeparam>
    public abstract class EntityConfigurationBase<TEntity, TConfiguration> : IEntityTypeConfiguration<TEntity> where TEntity : BindableEntityBase where TConfiguration : EntityConfigurationBase<TEntity, TConfiguration>, new()
    {
        /// <inheritdoc/>
        public virtual void Configure(EntityTypeBuilder<TEntity> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            builder.HasKey(entity => entity.Id);
            builder.Property(entity => entity.Id).ValueGeneratedOnAdd();
            builder.Property(entity => entity.RowVersion).IsRowVersion();
        }

        /// <summary>
        /// Creates a new instance of the concrete entity configuration.
        /// </summary>
        /// <returns>The new configuration instance.</returns>
        public static TConfiguration Create() => new();
    }
}