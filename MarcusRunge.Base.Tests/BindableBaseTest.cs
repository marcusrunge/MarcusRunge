namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains unit tests for the <see cref="BindableBase"/> class.
    /// </summary>
    public class BindableBaseTest
    {
        /// <summary>
        /// Sets the property when value changes updates value and raises property changed.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueChanges_UpdatesValueAndRaisesPropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            var raisedProperties = new List<string?>();

            systemUnderTest.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

            // Act
            systemUnderTest.Name = "Test";

            // Assert
            Assert.Equal("Test", systemUnderTest.Name);
            Assert.Single(raisedProperties);
            Assert.Equal(nameof(TestBindableBase.Name), raisedProperties[0]);
        }

        /// <summary>
        /// Sets the property when value is equal does not raise property changed.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueIsEqual_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase { Name = "Test" };
            var raised = false;

            systemUnderTest.PropertyChanged += (_, _) => raised = true;

            // Act
            systemUnderTest.Name = "Test";

            // Assert
            Assert.False(raised);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Sets the property when value changes returns true.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueChanges_ReturnsTrue()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();

            // Act
            var result = systemUnderTest.SetNameDirectly("Test", nameof(TestBindableBase.Name));

            // Assert
            Assert.True(result);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Sets the property when value is equal returns false.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueIsEqual_ReturnsFalse()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase { Name = "Test" };

            // Act
            var result = systemUnderTest.SetNameDirectly("Test", nameof(TestBindableBase.Name));

            // Assert
            Assert.False(result);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Called when [property changed when called explicitly raises property changed with given name].
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCalledExplicitly_RaisesPropertyChangedWithGivenName()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            string? raisedPropertyName = null;

            systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

            // Act
            systemUnderTest.RaisePropertyChanged("CustomProperty");

            // Assert
            Assert.Equal("CustomProperty", raisedPropertyName);
        }

        /// <summary>
        /// Called when [property changed when called without subscribers does not throw].
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCalledWithoutSubscribers_DoesNotThrow()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();

            // Act / Assert
            var exception = Record.Exception(() => systemUnderTest.RaisePropertyChanged("Name"));
            Assert.Null(exception);
        }
        
        private sealed class TestBindableBase : BindableBase
        {
            private string? _name;

            public string? Name { get => _name; set => SetProperty(ref _name, value); }

            public bool SetNameDirectly(string? value, string? propertyName = null) => SetProperty(ref _name, value, propertyName);

            public void RaisePropertyChanged(string? propertyName = null) => OnPropertyChanged(propertyName);
        }
    }
}