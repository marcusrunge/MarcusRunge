namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains unit tests for the <see cref="BindableEntityBase"/> class.
    /// </summary>
    public class BindableEntityBaseTest
    {
        /// <summary>
        /// Verifies that the default constructor initializes the database identity with its default value.
        /// </summary>
        [Fact]
        public void Constructor_WithoutId_UsesDefaultId()
        {
            // Arrange / Act
            var systemUnderTest = new TestBindableEntityBase();

            // Assert
            Assert.Equal(0, systemUnderTest.Id);
        }

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
        /// through the inherited <see cref="BindableBase.SetProperty{T}"/> implementation.
        /// </summary>
        [Fact]
        public void Id_WhenChanged_UpdatesValueAndRaisesPropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestBindableEntityBase();
            var raisedProperties = new List<string?>();

            systemUnderTest.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

            // Act
            systemUnderTest.Id = 42;

            // Assert
            Assert.Equal(42, systemUnderTest.Id);
            var item = Assert.Single(raisedProperties);
            Assert.Equal(nameof(BindableEntityBase.Id), item);
        }

        /// <summary>
        /// Verifies that assigning the current database identity does not raise a property change notification.
        /// </summary>
        [Fact]
        public void Id_WhenAssignedCurrentValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestBindableEntityBase(42);
            var notificationWasRaised = false;

            systemUnderTest.PropertyChanged += (_, _) => notificationWasRaised = true;

            // Act
            systemUnderTest.Id = 42;

            // Assert
            Assert.False(notificationWasRaised);
            Assert.Equal(42, systemUnderTest.Id);
        }

        /// <summary>
        /// Verifies that an identity notification uses the concrete entity instance as its sender.
        /// </summary>
        [Fact]
        public void Id_WhenChanged_UsesEntityAsPropertyChangedSender()
        {
            // Arrange
            var systemUnderTest = new TestBindableEntityBase();
            object? sender = null;

            systemUnderTest.PropertyChanged += (eventSender, _) => sender = eventSender;

            systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

            // Act
            systemUnderTest.Id = 42;

            // Assert
            Assert.Same(systemUnderTest, sender);
        }

        /// <summary>
        /// Verifies that changing the database concurrency token does not raise a property change notification.
        /// </summary>
        [Fact]
        public void RowVersion_WhenChanged_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestBindableEntityBase();
            var notificationWasRaised = false;

            systemUnderTest.PropertyChanged += (_, _) => notificationWasRaised = true;

            // Act
            systemUnderTest.RowVersion = [1, 2, 3];

            // Assert
            Assert.False(notificationWasRaised);
            Assert.Equal(new byte[] { 1, 2, 3 }, systemUnderTest.RowVersion);
        }

        /// <summary>
        /// Verifies that assigning the database concurrency token stores the supplied array reference.
        /// </summary>
        [Fact]
        public void RowVersion_WhenAssigned_PreservesArrayReference()
        {
            // Arrange
            var systemUnderTest = new TestBindableEntityBase();
            byte[] rowVersion = [1, 2, 3];

            // Act
            systemUnderTest.RowVersion = rowVersion;

            // Assert
            Assert.Same(rowVersion, systemUnderTest.RowVersion);
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