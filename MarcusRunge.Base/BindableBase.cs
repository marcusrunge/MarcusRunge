using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace MarcusRunge.Base
{
    /// <summary>
    /// Provides a base implementation for objects that notify subscribers when property values change.
    /// </summary>
    /// <remarks>
    /// The synchronization context available during construction is captured automatically.
    /// No context is captured when the instance is created on a thread without an installed
    /// synchronization context. Derived classes can replace or remove the captured context
    /// through <see cref="NotificationContext"/>.
    /// </remarks>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BindableBase"/> class and captures the current
        /// synchronization context, if one is available.
        /// </summary>
        protected BindableBase() => NotificationContext = SynchronizationContext.Current;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the synchronization context used to dispatch property change notifications.
        /// </summary>
        /// <remarks>
        /// When no context is assigned, notifications are raised synchronously on the calling thread.
        /// When the calling context differs from the assigned context, notifications are posted
        /// asynchronously to the assigned context.
        /// </remarks>
        protected SynchronizationContext? NotificationContext { get; set; }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the changed property. A <see langword="null"/> or empty value indicates that
        /// all properties may have changed. Logical or composite property names are also supported.
        /// </param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            VerifyPropertyName(propertyName);
            var notificationContext = NotificationContext;
            if (notificationContext is not null && notificationContext != SynchronizationContext.Current)
            {
                // Resolve the current subscriber list when the callback executes because subscribers may
                // change between scheduling and dispatching the notification.
                notificationContext.Post(static state =>
                {
                    var notification = (PropertyChangedNotification)state!;
                    notification.Source.RaisePropertyChanged(notification.PropertyName);
                }, new PropertyChangedNotification(this, propertyName));
                return;
            }

            RaisePropertyChanged(propertyName);
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for multiple properties.
        /// </summary>
        /// <param name="propertyNames">
        /// The property names to notify. When no names are supplied, a single notification for all
        /// properties is raised.
        /// </param>
        protected void RaisePropertyChanged(params string?[] propertyNames)
        {
            if (propertyNames.Length == 0)
            {
                OnPropertyChanged(null);
                return;
            }

            foreach (var propertyName in propertyNames) OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Assigns a value to a backing field and raises <see cref="PropertyChanged"/> when the value changed.
        /// </summary>
        /// <typeparam name="T">The property value type.</typeparam>
        /// <param name="backingField">A reference to the property's backing field.</param>
        /// <param name="value">The value to assign.</param>
        /// <param name="propertyName">
        /// The name of the changed property. When omitted, the compiler supplies the caller member name.
        /// </param>
        /// <param name="onChanged">
        /// An optional callback invoked after the backing field was updated and before the notification is raised.
        /// If the callback throws an exception, the new field value remains assigned and no notification is raised.
        /// </param>
        /// <param name="comparer">
        /// An optional equality comparer. <see cref="EqualityComparer{T}.Default"/> is used when omitted.
        /// </param>
        /// <returns>
        /// <see langword="true"/> when the value changed; otherwise <see langword="false"/>.
        /// </returns>
        protected virtual bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null, Action? onChanged = null, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;
            if (comparer.Equals(backingField, value)) return false;
            backingField = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Handles an exception thrown by a <see cref="PropertyChanged"/> subscriber.
        /// </summary>
        /// <param name="exception">The subscriber exception.</param>
        /// <remarks>
        /// Subscriber failures do not prevent other subscribers from being invoked and do not propagate
        /// to the operation that raised the notification. Derived classes may override this method to
        /// integrate diagnostic reporting. Exceptions thrown by this method are isolated as well.
        /// </remarks>
        protected virtual void OnPropertyChangedSubscriberException(Exception exception) => Debug.WriteLine($"A PropertyChanged subscriber threw an exception: {exception}");

        /// <summary>
        /// Provides a debug-only extension point for validating property notification names.
        /// </summary>
        /// <param name="propertyName">
        /// The property notification name to validate. A <see langword="null"/> or empty value represents
        /// all properties. Logical and composite names are supported by the base implementation.
        /// </param>
        /// <remarks>
        /// The base implementation intentionally performs no validation because property notification names
        /// do not have to identify public CLR properties. Derived classes may override this method when they
        /// require stricter validation.
        /// </remarks>
        [Conditional("DEBUG")]
        protected virtual void VerifyPropertyName(string? propertyName)
        {
            // PropertyChanged supports logical or composite names that do not necessarily map to public CLR properties.
            _ = propertyName;
        }

        /// <summary>
        /// Raises a property change notification synchronously on the current thread.
        /// </summary>
        /// <param name="propertyName">The name of the changed property.</param>
        /// <remarks>
        /// Each subscriber is invoked independently so a failing subscriber cannot prevent subsequent
        /// subscribers from receiving the notification.
        /// </remarks>
        private void RaisePropertyChanged(string? propertyName)
        {
            var handlers = PropertyChanged;
            if (handlers is null) return;

            var args = new PropertyChangedEventArgs(propertyName);
            foreach (var subscriber in handlers.GetInvocationList())
            {
                try
                {
                    ((PropertyChangedEventHandler)subscriber)(this, args);
                }
                catch (Exception exception)
                {
                    TryReportPropertyChangedSubscriberException(exception);
                }
            }
        }

        /// <summary>
        /// Reports a subscriber exception while isolating failures from the diagnostic handler itself.
        /// </summary>
        /// <param name="exception">The subscriber exception.</param>
        private void TryReportPropertyChangedSubscriberException(Exception exception)
        {
            try
            {
                OnPropertyChangedSubscriberException(exception);
            }
            catch (Exception diagnosticException)
            {
                Debug.WriteLine($"Reporting a PropertyChanged subscriber exception failed: {diagnosticException}");
            }
        }

        /// <summary>
        /// Contains the data required to dispatch a property change notification to another synchronization context.
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of the <see cref="PropertyChangedNotification"/> class.
        /// </remarks>
        /// <param name="source">The notification source.</param>
        /// <param name="propertyName">The name of the changed property.</param>
        private sealed class PropertyChangedNotification(BindableBase source, string? propertyName)
        {
            /// <summary>
            /// Gets the name of the changed property.
            /// </summary>
            public string? PropertyName { get; } = propertyName;

            /// <summary>
            /// Gets the notification source.
            /// </summary>
            public BindableBase Source { get; } = source;
        }
    }
}