namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains unit tests for the <see cref="BindableEntityBase"/> class.
    /// </summary>
    public class BindableEntityBaseTest
    {
        /// <summary>
        /// Verifies that the constructor assigns the specified database identity.
        /// </summary>
        [Fact]
        public void Constructor_WithId_SetsId()
        {
            // Arrange / Act
            var systemUnderTest = new TestBindableEntityBase(42);

            // Assert
            Assert.Equal(42, systemUnderTest.Id);
        }

        /// <summary>
        /// Verifies that changing the database identity raises a property change notification
        /// through the inherited SetProperty implementation.
        /// </summary>
        [Fact]
        public void Id_UsesSetProperty_FromBaseClass()
        {
            // Arrange
            var systemUnderTest = new TestBindableEntityBase();
            string? raisedPropertyName = null;

            systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

            // Act
            systemUnderTest.Id = 42;

            // Assert
            Assert.Equal(42, systemUnderTest.Id);
            Assert.Equal(nameof(BindableEntityBase.Id), raisedPropertyName);
        }

        /// <summary>
        /// Verifies that changing the database concurrency token does not raise a property change notification.
        /// </summary>
        [Fact]
        public void RowVersion_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestBindableEntityBase();
            var raised = false;

            systemUnderTest.PropertyChanged += (_, _) => raised = true;

            // Act
            systemUnderTest.RowVersion = [1, 2, 3];

            // Assert
            Assert.False(raised);
            Assert.Equal(new byte[] { 1, 2, 3 }, systemUnderTest.RowVersion);
        }

        /// <summary>
        /// Provides a concrete bindable entity for testing.
        /// </summary>
        private sealed class TestBindableEntityBase : BindableEntityBase
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="TestBindableEntityBase"/> class.
            /// </summary>
            public TestBindableEntityBase()
            {
                // Synchronization-context dispatch is tested separately by BindableBaseTest.
                // These tests verify only the BindableEntityBase property contracts.
                NotificationContext = null;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="TestBindableEntityBase"/> class
            /// with the specified database identity.
            /// </summary>
            /// <param name="id">The database identity.</param>
            public TestBindableEntityBase(int id) : base(id)
            {
                // Synchronization-context dispatch is tested separately by BindableBaseTest.
                // These tests verify only the BindableEntityBase property contracts.
                NotificationContext = null;
            }
        }
    }
}