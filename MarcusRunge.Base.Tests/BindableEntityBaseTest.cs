namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains unit tests for the <see cref="BindableEntityBase"/> class.
    /// </summary>
    public class BindableEntityBaseTest
    {
        /// <summary>
        /// Constructors the with identifier sets identifier.
        /// </summary>
        [Fact]
        public void Constructor_WithId_SetsId()
        {
            // Act
            var systemUnderTest = new TestEntity(42);

            // Assert
            Assert.Equal(42, systemUnderTest.Id);
        }

        /// <summary>
        /// Identifiers the uses set property from base class.
        /// </summary>
        [Fact]
        public void Id_UsesSetProperty_FromBaseClass()
        {
            // This test ensures that the Id property is wired to SetProperty,
            // but does NOT re-test SetProperty behavior itself.

            // Arrange
            var systemUnderTest = new TestEntity();
            string? raisedProperty = null;
            systemUnderTest.PropertyChanged += (_, e) => raisedProperty = e.PropertyName;

            // Act
            systemUnderTest.Id = 7;

            // Assert
            Assert.Equal(nameof(TestEntity.Id), raisedProperty);
        }

        /// <summary>
        /// Rows the version does not raise property changed.
        /// </summary>
        [Fact]
        public void RowVersion_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestEntity();
            var raised = false;
            systemUnderTest.PropertyChanged += (_, _) => raised = true;

            // Act
            systemUnderTest.RowVersion = [1, 2, 3];

            // Assert
            Assert.False(raised);
        }

        private sealed class TestEntity : BindableEntityBase
        {
            public TestEntity() { }
            public TestEntity(int id) : base(id) { }
        }
    }
}