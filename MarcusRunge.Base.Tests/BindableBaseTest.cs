using System.ComponentModel;
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
            var item = Assert.Single(raisedProperties);
            Assert.Equal(nameof(TestBindableBase.Name), item);
        }

        /// <summary>
        /// Verifies that assigning an equal value does not raise a property change notification.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueIsEqual_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            systemUnderTest.Name = "Test";

            var notificationWasRaised = false;
            systemUnderTest.PropertyChanged += (_, _) => notificationWasRaised = true;

            systemUnderTest.Name = "Test";

            // Assert
            Assert.False(notificationWasRaised);
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
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            systemUnderTest.Name = "Test";

            var result = systemUnderTest.SetNameDirectly("Test", nameof(TestBindableBase.Name));

            Assert.False(result);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Verifies that the change callback runs after assignment and before property notification.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueChanges_InvokesCallbackAfterAssignmentAndBeforeNotification()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            var operations = new List<string>();

            systemUnderTest.PropertyChanged += (_, _) =>
            {
                Assert.Equal("Test", systemUnderTest.Name);
                operations.Add("Notification");
            };

            // Act
            var result = systemUnderTest.SetNameWithCallback(
                "Test",
                () =>
                {
                    Assert.Equal("Test", systemUnderTest.Name);
                    operations.Add("Callback");
                });

            // Assert
            Assert.True(result);
            Assert.Equal(["Callback", "Notification"], operations);
        }

        /// <summary>
        /// Verifies that a failing change callback leaves the assigned value in place and prevents notification.
        /// </summary>
        [Fact]
        public void SetProperty_WhenCallbackThrows_KeepsAssignedValueAndDoesNotNotify()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            var notificationWasRaised = false;
            var expectedException = new InvalidOperationException("Callback failed.");

            systemUnderTest.PropertyChanged += (_, _) => notificationWasRaised = true;

            // Act
            var actualException = Assert.Throws<InvalidOperationException>(
                () => systemUnderTest.SetNameWithCallback("Test", () => throw expectedException));

            // Assert
            Assert.Same(expectedException, actualException);
            Assert.Equal("Test", systemUnderTest.Name);
            Assert.False(notificationWasRaised);
        }

        /// <summary>
        /// Verifies that a change callback is not invoked when the assigned value is equal.
        /// </summary>
        [Fact]
        public void SetProperty_WhenValueIsEqual_DoesNotInvokeCallback()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            systemUnderTest.Name = "Test";
            var callbackWasInvoked = false;

            // Act
            var result = systemUnderTest.SetNameWithCallback("Test", () => callbackWasInvoked = true);

            // Assert
            Assert.False(result);
            Assert.False(callbackWasInvoked);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Verifies that <see cref="BindableBase.SetProperty{T}"/> uses the supplied equality comparer.
        /// </summary>
        [Fact]
        public void SetProperty_WhenCustomComparerConsidersValuesEqual_DoesNotUpdateOrNotify()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            systemUnderTest.Name = "Test";

            var notificationWasRaised = false;
            systemUnderTest.PropertyChanged += (_, _) => notificationWasRaised = true;

            // Act
            var result = systemUnderTest.SetNameWithComparer("TEST", StringComparer.OrdinalIgnoreCase);

            // Assert
            Assert.False(result);
            Assert.Equal("Test", systemUnderTest.Name);
            Assert.False(notificationWasRaised);
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
        /// Verifies that a property change notification uses the concrete instance as its sender.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenRaised_UsesInstanceAsSender()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            object? sender = null;

            systemUnderTest.PropertyChanged += (eventSender, _) => sender = eventSender;

            // Act
            systemUnderTest.Name = "Test";

            // Assert
            Assert.Same(systemUnderTest, sender);
        }

        /// <summary>
        /// Verifies that raising a property change notification without subscribers does not throw.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCalledWithoutSubscribers_DoesNotThrow()
        {
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();

            // Act
            var exception = Record.Exception(() => systemUnderTest.RaisePropertyChanged(nameof(TestBindableBase.Name)));

            // Assert
            Assert.Null(exception);
        }

        /// <summary>
        /// Verifies that an exception thrown by a property change subscriber propagates to the caller
        /// when the notification is raised synchronously.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenSynchronousSubscriberThrows_PropagatesException()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            var expectedException = new InvalidOperationException("Subscriber failed.");

            systemUnderTest.PropertyChanged += (_, _) => throw expectedException;

            // Act
            var actualException = Assert.Throws<InvalidOperationException>(() => systemUnderTest.Name = "Test");

            // Assert
            Assert.Same(expectedException, actualException);
            Assert.Equal("Test", systemUnderTest.Name);
        }

        /// <summary>
        /// Verifies that a failing property change subscriber prevents subsequent subscribers from being invoked.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenSubscriberThrows_DoesNotInvokeSubsequentSubscribers()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            var subsequentSubscriberWasInvoked = false;

            systemUnderTest.PropertyChanged += (_, _) => throw new InvalidOperationException("Subscriber failed.");
            systemUnderTest.PropertyChanged += (_, _) => subsequentSubscriberWasInvoked = true;

            // Act
            Assert.Throws<InvalidOperationException>(() => systemUnderTest.Name = "Test");

            // Assert
            Assert.False(subsequentSubscriberWasInvoked);
        }

        /// <summary>
        /// Verifies that multiple property names are raised in the supplied order.
        /// </summary>
        [Fact]
        public void RaisePropertyChanged_WhenMultipleNamesAreSupplied_RaisesAllNamesInOrder()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            var raisedProperties = new List<string?>();

            systemUnderTest.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

            // Act
            systemUnderTest.RaiseProperties("First", "Second");

            // Assert
            Assert.Equal(["First", "Second"], raisedProperties);
        }

        /// <summary>
        /// Verifies that an empty property-name collection raises one notification for all properties.
        /// </summary>
        [Fact]
        public void RaisePropertyChanged_WhenNoNamesAreSupplied_RaisesSingleAllPropertiesNotification()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            var raisedProperties = new List<string?>();

            systemUnderTest.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

            // Act
            systemUnderTest.RaiseProperties();

            // Assert
            var item = Assert.Single(raisedProperties);
            Assert.Null(item);
        }

        /// <summary>
        /// Verifies that a null property-name collection raises one notification for all properties.
        /// </summary>
        [Fact]
        public void RaisePropertyChanged_WhenNamesArrayIsNull_RaisesSingleAllPropertiesNotification()
        {
            // Arrange
            var systemUnderTest = new TestBindableBase();
            systemUnderTest.DisableNotificationContext();
            var raisedProperties = new List<string?>();

            systemUnderTest.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

            // Act
            systemUnderTest.RaiseProperties(null);

            // Assert
            var item = Assert.Single(raisedProperties);
            Assert.Null(item);
        }

        /// <summary>
        /// Verifies that property change notifications are raised synchronously when the captured
        /// synchronization context is also the current context.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenCapturedContextIsCurrent_RaisesSynchronously()
        {
            // Arrange
            var originalContext = SynchronizationContext.Current;
            var notificationContext = new TestSynchronizationContext();

            try
            {
                SynchronizationContext.SetSynchronizationContext(notificationContext);

                var systemUnderTest = new TestBindableBase();
                string? raisedPropertyName = null;

                systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

                // Act
                systemUnderTest.Name = "Test";

                // Assert
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
            // Arrange
            var originalContext = SynchronizationContext.Current;
            var notificationContext = new TestSynchronizationContext();

            try
            {
                SynchronizationContext.SetSynchronizationContext(notificationContext);
                var systemUnderTest = new TestBindableBase();

                SynchronizationContext.SetSynchronizationContext(null);

                string? raisedPropertyName = null;
                systemUnderTest.PropertyChanged += (_, args) => raisedPropertyName = args.PropertyName;

                // Act
                systemUnderTest.Name = "Test";

                // Assert
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
        /// Verifies that an exception thrown by a posted subscriber occurs while the captured context
        /// processes the notification rather than while the property is assigned.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenPostedSubscriberThrows_PropagatesWhileProcessingCapturedContext()
        {
            // Arrange
            var originalContext = SynchronizationContext.Current;
            var notificationContext = new TestSynchronizationContext();

            try
            {
                SynchronizationContext.SetSynchronizationContext(notificationContext);
                var systemUnderTest = new TestBindableBase();

                SynchronizationContext.SetSynchronizationContext(null);

                var expectedException = new InvalidOperationException("Subscriber failed.");
                systemUnderTest.PropertyChanged += (_, _) => throw expectedException;

                // Act
                var assignmentException = Record.Exception(() => systemUnderTest.Name = "Test");
                var actualException = Assert.Throws<InvalidOperationException>(() => notificationContext.ExecutePendingCallbacks());

                // Assert
                Assert.Null(assignmentException);
                Assert.Same(expectedException, actualException);
                Assert.Equal("Test", systemUnderTest.Name);
                Assert.Equal(0, notificationContext.PendingCallbackCount);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        /// <summary>
        /// Verifies that the current subscriber list is resolved when a posted notification executes.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenNotificationIsPosted_UsesSubscribersRegisteredAtExecutionTime()
        {
            // Arrange
            var originalContext = SynchronizationContext.Current;
            var notificationContext = new TestSynchronizationContext();

            try
            {
                SynchronizationContext.SetSynchronizationContext(notificationContext);
                var systemUnderTest = new TestBindableBase();

                SynchronizationContext.SetSynchronizationContext(null);

                var originalSubscriberWasInvoked = false;
                var laterSubscriberWasInvoked = false;

                void originalSubscriber(object? _1, PropertyChangedEventArgs _2) => originalSubscriberWasInvoked = true;
                systemUnderTest.PropertyChanged += originalSubscriber;

                systemUnderTest.Name = "Test";

                systemUnderTest.PropertyChanged -= originalSubscriber;
                systemUnderTest.PropertyChanged += (_, _) => laterSubscriberWasInvoked = true;

                // Act
                notificationContext.ExecutePendingCallbacks();

                // Assert
                Assert.False(originalSubscriberWasInvoked);
                Assert.True(laterSubscriberWasInvoked);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(originalContext);
            }
        }

        /// <summary>
        /// Verifies that posted notifications preserve their scheduling order.
        /// </summary>
        [Fact]
        public void OnPropertyChanged_WhenMultipleNotificationsArePosted_PreservesOrder()
        {
            // Arrange
            var originalContext = SynchronizationContext.Current;
            var notificationContext = new TestSynchronizationContext();

            try
            {
                SynchronizationContext.SetSynchronizationContext(notificationContext);
                var systemUnderTest = new TestBindableBase();

                SynchronizationContext.SetSynchronizationContext(null);

                var raisedProperties = new List<string?>();
                systemUnderTest.PropertyChanged += (_, args) => raisedProperties.Add(args.PropertyName);

                // Act
                systemUnderTest.RaiseProperties("First", "Second");

                // Assert
                Assert.Empty(raisedProperties);
                Assert.Equal(2, notificationContext.PendingCallbackCount);

                notificationContext.ExecutePendingCallbacks();

                Assert.Equal(["First", "Second"], raisedProperties);
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
            /// Sets the name and invokes the specified callback after assignment.
            /// </summary>
            /// <param name="value">The value to set.</param>
            /// <param name="onChanged">The callback invoked after assignment.</param>
            /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
            public bool SetNameWithCallback(string? value, Action onChanged) => SetProperty(ref _name, value, onChanged: onChanged);

            /// <summary>
            /// Sets the name using the specified equality comparer.
            /// </summary>
            /// <param name="value">The value to set.</param>
            /// <param name="comparer">The equality comparer.</param>
            /// <returns><see langword="true"/> when the value changed; otherwise <see langword="false"/>.</returns>
            public bool SetNameWithComparer(string? value, IEqualityComparer<string?> comparer) => SetProperty(ref _name, value, comparer: comparer);

            /// <summary>
            /// Raises a property change notification with the specified name.
            /// </summary>
            /// <param name="propertyName">The property notification name.</param>
            public void RaisePropertyChanged(string? propertyName = null) => OnPropertyChanged(propertyName);

            /// <summary>
            /// Raises property change notifications for the specified property names.
            /// </summary>
            /// <param name="propertyNames">The property names to notify.</param>
            public void RaiseProperties(params string?[]? propertyNames) => RaisePropertyChanged(propertyNames);
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
                while (TryDequeue(out var callback)) callback.Callback(callback.State);
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

            private readonly struct PostedCallback(SendOrPostCallback callback, object? state)
            {
                public SendOrPostCallback Callback { get; } = callback;

                public object? State { get; } = state;
            }
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