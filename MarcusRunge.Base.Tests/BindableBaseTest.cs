using System.Threading;

namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains unit tests for the <see cref="BindableBase"/> class.
    /// </summary>
    public class BindableBaseTest
    {
        /// <summary>
        /// Verifies that changing a property updates its value and raises the expected notification.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueChanges_UpdatesValueAndRaisesPropertyChanged()
        {
            // Disable context dispatch because this test verifies SetProperty behavior independently
            // from synchronization-context forwarding.
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();

            var raisedProperties = new List<string?>();

            systemUnderTest.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

            systemUnderTest.Name = "Test";

            Assert.Equal("Test", systemUnderTest.Name);
            Assert.Single(raisedProperties);
            Assert.Equal(nameof(TestBindableBase.Name), raisedProperties[0]);
        }

        /// <summary>
        /// Verifies that assigning an equal value does not raise a property change notification.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueIsEqual_DoesNotRaisePropertyChanged()
        {
            // Disable context dispatch so the test observes only the equality and notification behavior.
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            systemUnderTest.Name = "Test";

            var raised = false;

            systemUnderTest.PropertyChanged += (_, _) => raised = true;

            systemUnderTest.Name = "Test";

            Assert.False(raised);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Verifies that changing a property returns <see langword="true"/>.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueChanges_ReturnsTrue()
        {
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();

            var result = systemUnderTest.SetNameDirectly("Test", nameof(TestBindableBase.Name));

            Assert.True(result);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Verifies that assigning an equal value returns <see langword="false"/>.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueIsEqual_ReturnsFalse()
        {
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            systemUnderTest.Name = "Test";

            var result = systemUnderTest.SetNameDirectly("Test", nameof(TestBindableBase.Name));

            Assert.False(result);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Verifies that an explicit property change notification preserves the supplied logical property name.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCalledExplicitly_RaisesPropertyChangedWithGivenName()
        {
            // Logical notification names do not have to identify public CLR properties.
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();

            string? raisedPropertyName = null;

            systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

            systemUnderTest.RaisePropertyChanged("CustomProperty");

            Assert.Equal("CustomProperty", raisedPropertyName);
        }

        /// <summary>
        /// Verifies that raising a property change notification without subscribers does not throw.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCalledWithoutSubscribers_DoesNotThrow()
        {
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();

            var exception = Record.Exception(() => systemUnderTest.RaisePropertyChanged(nameof(TestBindableBase.Name)));

            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that property change notifications are raised synchronously when the captured
        /// synchronization context is also the current context.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCapturedContextIsCurrent_RaisesSynchronously()
        {
            var originalContext = SynchronizationContext.Current;
            var notificationContext = new TestSynchronizationContext();

            try
            {
                SynchronizationContext.SetSynchronizationContext(notificationContext);

                var systemUnderTest = new TestBindableBase();
                string? raisedPropertyName = null;

                systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

                systemUnderTest.Name = "Test";

                Assert.Equal(nameof(TestBindableBase.Name), raisedPropertyName);
                Assert.Equal(0, notificationContext.PendingCallbackCount);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        /// <summary>
        /// Verifies that property change notifications are posted when the captured synchronization
        /// context differs from the current context.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCapturedContextDiffers_PostsNotificationToCapturedContext()
        {
            var originalContext = SynchronizationContext.Current;
            var notificationContext = new TestSynchronizationContext();

            try
            {
                SynchronizationContext.SetSynchronizationContext(notificationContext);
                var systemUnderTest = new TestBindableBase();

                SynchronizationContext.SetSynchronizationContext(null);

                string? raisedPropertyName = null;

                systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

                systemUnderTest.Name = "Test";

                Assert.Null(raisedPropertyName);
                Assert.Equal(1, notificationContext.PendingCallbackCount);

                notificationContext.ExecutePendingCallbacks();

                Assert.Equal(nameof(TestBindableBase.Name), raisedPropertyName);
                Assert.Equal(0, notificationContext.PendingCallbackCount);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        /// <summary>
        /// Provides access to the protected members of <see cref="BindableBase"/> for testing.
        /// </summary>
        private sealed class TestBindableBase : BindableBase
        {
            private string? _name;

            /// <summary>
            /// Gets or sets the test name.
            /// </summary>
            public string? Name
            {
                get => _name;
                set => SetProperty(ref _name, value);
            }

            /// <summary>
            /// Disables synchronization-context dispatch for tests that verify only property assignment behavior.
            /// </summary>
            public void DisableNotificationContext() => NotificationContext = null;

            /// <summary>
            /// Sets the name through <see cref="BindableBase.SetProperty{T}"/>.
            /// </summary>
            /// <param name="value">The value to set.</param>
            /// <param name="propertyName">The notification property name.</param>
            /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
            public bool SetNameDirectly(string? value, string? propertyName = null) => SetProperty(ref _name, value, propertyName);

            /// <summary>
            /// Raises a property change notification with the specified name.
            /// </summary>
            /// <param name="propertyName">The property notification name.</param>
            public void RaisePropertyChanged(string? propertyName = null) => OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Provides deterministic synchronization-context dispatch for unit tests.
        /// </summary>
        private sealed class TestSynchronizationContext : SynchronizationContext
        {
            private readonly Queue<PostedCallback> _pendingCallbacks = new();

            /// <summary>
            /// Gets the number of callbacks waiting to be executed.
            /// </summary>
            public int PendingCallbackCount
            {
                get
                {
                    lock (_pendingCallbacks)
                    {
                        return _pendingCallbacks.Count;
                    }
                }
            }

            /// <inheritdoc/>
            public override void Post(SendOrPostCallback callback, object? state)
            {
                ArgumentNullException.ThrowIfNull(callback);

                lock (_pendingCallbacks)
                {
                    _pendingCallbacks.Enqueue(new PostedCallback(callback, state));
                }
            }

            /// <summary>
            /// Executes all currently pending callbacks on the calling thread.
            /// </summary>
            public void ExecutePendingCallbacks()
            {
                while (TryDequeue(out var callback))
                    callback.Callback(callback.State);
            }

            private bool TryDequeue(out PostedCallback callback)
            {
                lock (_pendingCallbacks)
                {
                    if (_pendingCallbacks.Count == 0)
                    {
                        callback = default;
                        return false;
                    }

                    callback = _pendingCallbacks.Dequeue();
                    return true;
                }
            }

            private readonly struct PostedCallback
            {
                public PostedCallback(SendOrPostCallback callback, object? state)
                {
                    Callback = callback;
                    State = state;
                }

                public SendOrPostCallback Callback { get; }

                public object? State { get; }
            }
        }
    }
}