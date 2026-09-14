using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace MarcusRunge.Base
{
    /// <summary>
    /// Provides thread-safe synchronous construction and one-time asynchronous initialization
    /// with global, scoped, and transient lifetime support.
    /// </summary>
    /// <typeparam name="TInterface">The interface implemented by the concrete class.</typeparam>
    /// <typeparam name="TClass">The concrete class that inherits from this base class.</typeparam>
    /// <typeparam name="TBase">The context used for synchronous and asynchronous initialization.</typeparam>
    public abstract class CreateableBindableBase<TInterface, TClass, TBase> : BindableBase, ICreateableAware where TClass : CreateableBindableBase<TInterface, TClass, TBase>, TInterface, new()
    {
        private static readonly object _globalStateSynchronization = new();
        private static readonly ConditionalWeakTable<object, CreationState> _scopedStates = new();

        [ThreadStatic]
        private static CreationState? _currentCreationState;

        private static CreationState _globalState = new();

        private readonly object _createdHandlersSynchronization = new();

        private EventHandler? _createdHandlers;
        private CreationState? _creationState;

        /// <inheritdoc/>
        public event EventHandler? OnCreated
        {
            add
            {
                if (value is null)
                    return;

                if (IsCreated)
                {
                    InvokeCreatedHandler(value);
                    return;
                }

                lock (_createdHandlersSynchronization)
                {
                    if (!IsCreated)
                    {
                        _createdHandlers += value;
                        return;
                    }
                }

                InvokeCreatedHandler(value);
            }
            remove
            {
                if (value is null)
                    return;

                lock (_createdHandlersSynchronization)
                {
                    _createdHandlers -= value;
                }
            }
        }

        /// <inheritdoc/>
        public Task? Initialization
        {
            get
            {
                var state = GetCreationState();
                return state is null ? null : Volatile.Read(ref state.Initialization);
            }
        }

        /// <inheritdoc/>
        Task? ICreateableAware.Initialization => Initialization;

        /// <inheritdoc/>
        public Exception? InitializationException
        {
            get
            {
                var state = GetCreationState();
                return state is null ? null : Volatile.Read(ref state.InitializationException);
            }
        }

        /// <inheritdoc/>
        Exception? ICreateableAware.InitializationException => InitializationException;

        /// <inheritdoc/>
        public bool IsCreated
        {
            get
            {
                var state = GetCreationState();
                return state is not null && Volatile.Read(ref state.IsCreated) == 1;
            }
        }

        /// <inheritdoc/>
        bool ICreateableAware.IsCreated => IsCreated;

        /// <inheritdoc/>
        public bool IsInitializing
        {
            get
            {
                var initialization = Initialization;
                return initialization is { IsCompleted: false };
            }
        }

        /// <inheritdoc/>
        bool ICreateableAware.IsInitializing => IsInitializing;

        /// <summary>
        /// Creates or returns the global instance and starts its asynchronous initialization.
        /// </summary>
        /// <param name="context">The initialization context.</param>
        /// <returns>The global instance.</returns>
        /// <remarks>
        /// This overload preserves the original global lifetime behavior.
        /// The method returns after synchronous initialization and does not wait for asynchronous initialization.
        /// </remarks>
        public static TInterface Create(TBase context) => Create(context, CreationLifetime.Global);

        /// <summary>
        /// Creates or returns an instance using the requested lifetime and starts its asynchronous initialization.
        /// </summary>
        /// <param name="context">The initialization context.</param>
        /// <param name="lifetime">The requested creation lifetime.</param>
        /// <returns>The created instance.</returns>
        /// <remarks>
        /// For <see cref="CreationLifetime.Scoped"/>, the reference identity of
        /// <paramref name="context"/> identifies the scope.
        /// </remarks>
        public static TInterface Create(TBase context, CreationLifetime lifetime) => CreateInstance(context, lifetime);

        /// <summary>
        /// Creates or returns the global instance and waits for asynchronous initialization.
        /// </summary>
        /// <param name="context">The initialization context.</param>
        /// <param name="cancellationToken">
        /// A token that cancels only this caller's wait. It does not cancel shared initialization.
        /// </param>
        /// <returns>The fully initialized global instance.</returns>
        public static Task<TInterface> CreateAsync(TBase context, CancellationToken cancellationToken = default) => CreateAsync(context, CreationLifetime.Global, cancellationToken);

        /// <summary>
        /// Creates or returns an instance using the requested lifetime and waits for asynchronous initialization.
        /// </summary>
        /// <param name="context">The initialization context.</param>
        /// <param name="lifetime">The requested creation lifetime.</param>
        /// <param name="cancellationToken">
        /// A token that cancels only this caller's wait. It does not cancel shared initialization.
        /// </param>
        /// <returns>The fully initialized instance.</returns>
        /// <remarks>
        /// Shared initialization always receives <see cref="CancellationToken.None"/>.
        /// Application lifetime cancellation must be supplied through the initialization context.
        /// </remarks>
        public static async Task<TInterface> CreateAsync(TBase context, CreationLifetime lifetime, CancellationToken cancellationToken = default)
        {
            var instance = CreateInstance(context, lifetime);

            if (instance.Initialization is { } initialization)
                await WaitWithCancellationAsync(initialization, cancellationToken).ConfigureAwait(false);

            return instance;
        }

        /// <summary>
        /// Resets the global creation state for unit tests.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The global state is currently being synchronously or asynchronously initialized.
        /// </exception>
        internal static void ResetGlobalStateForTests()
        {
            lock (_globalStateSynchronization)
            {
                var state = _globalState;

                lock (state.Synchronization)
                {
                    var synchronousCreationIsRunning = state.CreationStarted && !state.CreationCompletion.Task.IsCompleted;
                    var asynchronousInitializationIsRunning = state.Initialization is { IsCompleted: false };

                    if (synchronousCreationIsRunning || asynchronousInitializationIsRunning)
                        throw new InvalidOperationException($"The global creation state for '{typeof(TClass).FullName}' cannot be reset while initialization is running.");

                    _globalState = new CreationState();
                }
            }
        }

        /// <summary>
        /// Performs synchronous initialization before the instance is published.
        /// </summary>
        /// <param name="context">The initialization context.</param>
        /// <remarks>
        /// Implementations must establish every invariant required for safely exposing the instance.
        /// A failure permanently faults the affected creation state.
        /// </remarks>
        protected abstract void OnCreate(TBase context);

        /// <summary>
        /// Performs one-time asynchronous initialization.
        /// </summary>
        /// <param name="context">The initialization context.</param>
        /// <param name="cancellationToken">
        /// The initialization token. The base implementation supplies
        /// <see cref="CancellationToken.None"/> so caller wait cancellation cannot cancel shared initialization.
        /// </param>
        /// <returns>A task representing asynchronous initialization.</returns>
        protected abstract Task OnCreateAsync(TBase context, CancellationToken cancellationToken);

        /// <summary>
        /// Handles an exception thrown by an <see cref="OnCreated"/> subscriber.
        /// </summary>
        /// <param name="exception">The subscriber exception.</param>
        /// <remarks>
        /// Subscriber failures do not fail completed initialization and do not prevent other subscribers
        /// from being invoked. Derived classes may override this method to integrate diagnostic reporting.
        /// Exceptions thrown by this method are isolated as well.
        /// </remarks>
        protected virtual void OnCreatedSubscriberException(Exception exception) => Debug.WriteLine($"An OnCreated subscriber threw an exception: {exception}");

        private static bool ContextsAreIdentical(TBase first, TBase second) => typeof(TBase).IsValueType ? EqualityComparer<TBase>.Default.Equals(first, second) : ReferenceEquals(first, second);

        private static TClass CreateCore(CreationState state, TBase context)
        {
            var startsCreation = false;

            lock (state.Synchronization)
            {
                ValidateContext(state, context);

                if (state.CreationException is { } creationException)
                    ExceptionDispatchInfo.Capture(creationException).Throw();

                if (!state.CreationStarted)
                {
                    state.CreationStarted = true;
                    startsCreation = true;
                }
            }

            if (startsCreation)
                StartCreation(state, context);
            else
                ThrowIfCreationIsReentrant(state);

            return state.CreationCompletion.Task.GetAwaiter().GetResult();
        }

        private static TClass CreateInstance(TBase context, CreationLifetime lifetime)
        {
            var state = GetState(context, lifetime);
            return CreateCore(state, context);
        }

        private static CreationState CreateScopedState(object _) => new();

        private static CreationState GetGlobalState()
        {
            lock (_globalStateSynchronization)
            {
                return _globalState;
            }
        }

        private static CreationState GetScopedState(TBase context)
        {
            if (typeof(TBase).IsValueType)
                throw new InvalidOperationException($"The scoped lifetime requires the initialization context type '{typeof(TBase).FullName}' to be a reference type.");

            if (context is not object scope)
                throw new ArgumentNullException(nameof(context), "The scoped lifetime requires a non-null initialization context.");

            return _scopedStates.GetValue(scope, CreateScopedState);
        }

        private static CreationState GetState(TBase context, CreationLifetime lifetime) => lifetime switch
        {
            CreationLifetime.Global => GetGlobalState(),
            CreationLifetime.Scoped => GetScopedState(context),
            CreationLifetime.Transient => new CreationState(),
            _ => throw new InvalidEnumArgumentException(nameof(lifetime), (int)lifetime, typeof(CreationLifetime))
        };

        private static void StartCreation(CreationState state, TBase context)
        {
            var previousCreationState = _currentCreationState;
            _currentCreationState = state;

            try
            {
                // User-defined hooks run outside internal locks to prevent lock inversion and reentrancy deadlocks.
                var instance = new TClass();
                instance.AttachCreationState(state);
                instance.OnCreate(context);

                // The initialization task is assigned before the instance is published to waiting callers.
                var initialization = instance.InitializeAsync(context);

                lock (state.Synchronization)
                {
                    state.Initialization = initialization;
                }

                state.CreationCompletion.TrySetResult(instance);
            }
            catch (Exception exception)
            {
                lock (state.Synchronization)
                {
                    state.CreationException = exception;
                    state.InitializationException = exception;
                }

                state.CreationCompletion.TrySetException(exception);
            }
            finally
            {
                _currentCreationState = previousCreationState;
            }
        }

        private static void ThrowIfCreationIsReentrant(CreationState state)
        {
            if (ReferenceEquals(_currentCreationState, state))
                throw new InvalidOperationException($"Recursive creation of '{typeof(TClass).FullName}' for the same creation state is not supported.");
        }

        private static void ValidateContext(CreationState state, TBase context)
        {
            if (!state.HasInitializationContext)
            {
                state.InitializationContext = context;
                state.HasInitializationContext = true;
                return;
            }

            if (ContextsAreIdentical(state.InitializationContext, context))
                return;

            throw new InvalidOperationException($"The creation state for '{typeof(TClass).FullName}' was already initialized with a different context. Use '{CreationLifetime.Scoped}' with a stable context instance or '{CreationLifetime.Transient}' for an independent object graph.");
        }

        private static async Task WaitWithCancellationAsync(Task task, CancellationToken cancellationToken)
        {
            if (task.IsCompleted)
            {
                await task.ConfigureAwait(false);
                return;
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!cancellationToken.CanBeCanceled)
            {
                await task.ConfigureAwait(false);
                return;
            }

            var cancellationCompletion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

            using (cancellationToken.Register(static state => ((TaskCompletionSource<object?>)state!).TrySetResult(null), cancellationCompletion))
            {
                var completedTask = await Task.WhenAny(task, cancellationCompletion.Task).ConfigureAwait(false);

                // Initialization takes precedence when completion and cancellation become observable together.
                if (task.IsCompleted)
                {
                    await task.ConfigureAwait(false);
                    return;
                }

                if (completedTask == cancellationCompletion.Task)
                    throw new OperationCanceledException(cancellationToken);

                await task.ConfigureAwait(false);
            }
        }

        private void AttachCreationState(CreationState state)
        {
            if (Interlocked.CompareExchange(ref _creationState, state, null) is not null)
                throw new InvalidOperationException("The instance is already attached to a creation state.");
        }

        private CreationState? GetCreationState() => Volatile.Read(ref _creationState);

        private CreationState GetRequiredCreationState() => GetCreationState() ?? throw new InvalidOperationException("The instance is not attached to a creation state.");

        private async Task InitializeAsync(TBase context)
        {
            var state = GetRequiredCreationState();

            try
            {
                await OnCreateAsync(context, CancellationToken.None).ConfigureAwait(false);
                Volatile.Write(ref state.IsCreated, 1);
            }
            catch (Exception exception)
            {
                Volatile.Write(ref state.InitializationException, exception);
                throw;
            }

            RaiseCreated();
        }

        private void InvokeCreatedHandler(EventHandler handler)
        {
            try
            {
                handler(this, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                TryReportCreatedSubscriberException(exception);
            }
        }

        private void RaiseCreated()
        {
            EventHandler? handlers;

            lock (_createdHandlersSynchronization)
            {
                handlers = _createdHandlers;
                _createdHandlers = null;
            }

            if (handlers is null)
                return;

            foreach (var subscriber in handlers.GetInvocationList())
                InvokeCreatedHandler((EventHandler)subscriber);
        }

        private void TryReportCreatedSubscriberException(Exception exception)
        {
            try
            {
                OnCreatedSubscriberException(exception);
            }
            catch (Exception diagnosticException)
            {
                Debug.WriteLine($"Reporting an OnCreated subscriber exception failed: {diagnosticException}");
            }
        }

        private sealed class CreationState
        {
            internal readonly TaskCompletionSource<TClass> CreationCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            internal readonly object Synchronization = new();

            internal Exception? CreationException;
            internal bool CreationStarted;
            internal bool HasInitializationContext;
            internal Task? Initialization;
            internal TBase InitializationContext = default!;
            internal Exception? InitializationException;
            internal int IsCreated;
        }
    }
}