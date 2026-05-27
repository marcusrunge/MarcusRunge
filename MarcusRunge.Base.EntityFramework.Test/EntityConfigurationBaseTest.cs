using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace MarcusRunge.Base.EntityFramework.Test
{
    /// <summary>
    /// Contains unit tests for the <see cref="EntityConfigurationBase{TEntity, TConfiguration}"/> class.
    /// </summary>
    public class EntityConfigurationBaseTest
    {
        /// <summary>
        /// Configures the should configure identifier as value generated on add.
        /// </summary>
        [Fact]
        public void Configure_ShouldConfigureId_AsValueGeneratedOnAdd()
        {
            // Arrange
            var modelBuilder = new ModelBuilder();
            var configuration = new TestConfiguration();

            // Act
            configuration.Configure(modelBuilder.Entity<TestEntity>());

            // Assert
            var property = modelBuilder.Model
                .FindEntityType(typeof(TestEntity))!
                .FindProperty(nameof(TestEntity.Id));

            Assert.NotNull(property);
            Assert.Equal(ValueGenerated.OnAdd, property!.ValueGenerated);
        }

        /// <summary>
        /// Configures the should configure row version as concurrency token with correct generation.
        /// </summary>
        [Fact]
        public void Configure_ShouldConfigureRowVersion_AsConcurrencyToken_WithCorrectGeneration()
        {
            // Arrange
            var modelBuilder = new ModelBuilder();
            var configuration = new TestConfiguration();

            // Act
            configuration.Configure(modelBuilder.Entity<TestEntity>());

            // Assert
            var property = modelBuilder.Model
                .FindEntityType(typeof(TestEntity))!
                .FindProperty(nameof(TestEntity.RowVersion));

            Assert.NotNull(property);
            Assert.True(property!.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, property.ValueGenerated);
        }

        /// <summary>
        /// Configures the should set primary key identifier.
        /// </summary>
        [Fact]
        public void Configure_ShouldSetPrimaryKey_Id()
        {
            // Arrange
            var modelBuilder = new ModelBuilder();
            var configuration = new TestConfiguration();

            // Act
            configuration.Configure(modelBuilder.Entity<TestEntity>());

            // Assert
            var entityType = modelBuilder.Model.FindEntityType(typeof(TestEntity));
            var key = entityType!.FindPrimaryKey();

            Assert.NotNull(key);
            Assert.Single(key!.Properties);
            Assert.Equal(nameof(TestEntity.Id), key.Properties[0].Name);
        }

        /// <summary>
        /// Creates the should return new instance each time.
        /// </summary>
        [Fact]
        public void Create_ShouldReturnNewInstance_EachTime()
        {
            // Act
            var first = TestConfiguration.Create();
            var second = TestConfiguration.Create();

            // Assert
            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.IsType<TestConfiguration>(first);
            Assert.NotSame(first, second);
        }

        private sealed class TestConfiguration
            : EntityConfigurationBase<TestEntity, TestConfiguration>
        {
        }

        private sealed class TestEntity : BindableEntityBase
        {
        }
    }
}