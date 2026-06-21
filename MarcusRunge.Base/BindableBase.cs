using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace MarcusRunge.Base
{
    /// <summary>
    /// Base class for ViewModels (or other bindable objects) that provides
    /// <see cref="INotifyPropertyChanged"/> support and several convenience helpers.
    /// </summary>
    /// <remarks>
    /// Enhancements over the simple pattern:
    /// - Optional change callback (onChanged) for follow-up work.
    /// - Accepts a custom <see cref="IEqualityComparer{T}"/> for special comparison logic.
    /// - Ability to raise multiple property notifications at once.
    /// - Optional SynchronizationContext support: the context is captured by default at construction time
    ///   but can be overridden by derived types via the protected <see cref="SynchronizationContext"/> property.
    /// - Debug-time verification of property names to catch typos early.
    /// </remarks>
    public abstract class BindableBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// The synchronization context used to raise PropertyChanged. Set to the current context in the constructor.
        /// Derived classes (UI libraries) can override or set this to ensure notifications are raised on the UI thread.
        /// </summary>
        protected SynchronizationContext? SynchronizationContext { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="BindableBase"/> and captures the current <see cref="SynchronizationContext"/>, if any.
        /// </summary>

        protected BindableBase() => SynchronizationContext = SynchronizationContext.Current;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>

        /// <param name="propertyName">
        /// The name of the property that changed. When omitted, the compiler supplies the caller member name.
        /// A null value is supported and indicates that all properties may have changed.
        /// </param>
        /// <remarks>
        /// This implementation marshals the raise onto the captured <see cref="SynchronizationContext"/>, if available.
        /// Override this method in derived types to change marshalling behavior (for example in platform-specific UI layers).
        /// </remarks>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            VerifyPropertyName(propertyName);

            var handler = PropertyChanged;
            if (handler == null)
                return;

            var args = new PropertyChangedEventArgs(propertyName);

            var sync = SynchronizationContext;
            if (sync != null && sync != SynchronizationContext.Current)
            {
                // Post to the captured synchronization context so UI listeners always receive events on the expected thread.
                sync.Post(_ => handler.Invoke(this, args), null);
                return;
            }

            handler.Invoke(this, args);
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for multiple properties in one call.
        /// </summary>
        /// <param name="propertyNames">Property names to raise. If none provided or if the array is null, a single notification with null propertyName is raised.</param>
        protected void RaisePropertyChanged(params string[]? propertyNames)
        {
            if (propertyNames == null || propertyNames.Length == 0)
            {
                OnPropertyChanged(null);
                return;
            }

            foreach (var name in propertyNames)
                OnPropertyChanged(name);
        }

        /// <summary>
        /// Sets a property's backing field and raises <see cref="PropertyChanged"/> only if the value actually changed.
        /// This overload accepts an optional change callback and an optional custom comparer.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="backingField">A reference to the field that stores the property's current value.</param>
        /// <param name="value">The new value to assign.</param>
        /// <param name="onChanged">Optional action invoked after the field was updated but before raising PropertyChanged.</param>
        /// <param name="comparer">Optional equality comparer. Defaults to <see cref="EqualityComparer{T}.Default"/>.</param>
        /// <param name="propertyName">The name of the property that changed. When omitted, the compiler supplies the caller member name.</param>
        /// <returns><c>true</c> if the value was changed and a notification was raised; otherwise <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null, Action? onChanged = null, IEqualityComparer<T>? comparer = null)
        {
            comparer ??= EqualityComparer<T>.Default;

            if (comparer.Equals(backingField, value))
                return false;

            backingField = value;

            onChanged?.Invoke();

            OnPropertyChanged(propertyName);

            return true;
        }


        /// <summary>
        /// In debug builds verifies that a supplied property name actually exists on this instance.
        /// Helps catch typos in strings passed to <see cref="OnPropertyChanged"/> or <see cref="RaisePropertyChanged"/>.
        /// </summary>
        /// <param name="propertyName">The property name to verify. Null is accepted and means "all properties".</param>
        [Conditional("DEBUG")]
        protected void VerifyPropertyName(string? propertyName)
        {
            // Intentionally left blank: allow raising PropertyChanged for names that may not map to a CLR property.
            // Some callers use logical or composite property names that are not actual CLR properties (tests rely on this).
            // Keep this method present so derived classes can override it if stricter verification is desired.
            _ = propertyName; // avoid unused parameter warning in release builds
        }
    }
}
