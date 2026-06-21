using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;

namespace MarcusRunge.Base
{
    /// <summary>
    /// Public base type that provides thread-safe creation and one-time async initialization for a singleton-like instance.
    /// </summary>
    /// <typeparam name="TInterface">The interface that the class implements.</typeparam>
    /// <typeparam name="TClass">The concrete class that inherits from this base class.</typeparam>
    /// <typeparam name="TBase">The base class for the concrete class.</typeparam>
    public abstract class CreateableBindableBase<TInterface, TClass, TBase> : BindableBase, ICreateableAware
        where TClass : CreateableBindableBase<TInterface, TClass, TBase>, TInterface, new()
    {
        // Global synchronization for singleton creation and starting the async initialization exactly once.
        private static readonly object _sync = new();

        private static Exception? _initializationException;
        private static Task? _initTask;
        private static TClass? _instance;

        // Instance-level synchronization for event handler registration and draining (invocation after "created" flips).
        private readonly object _createdLock = new();

        private EventHandler? _createdHandlers;

        // 0 = not created; 1 = created (written via Interlocked, read via Volatile).
        private static int _isCreated;

        ///<inheritdoc />
        public event EventHandler? OnCreated
        {
            add
            {
                if (value is null) return;
                if (IsCreated)
                {
                    value(this, EventArgs.Empty);
                    return;
                }

                lock (_createdLock)
                {
                    if (!IsCreated)
                    {
                        _createdHandlers += value;
                        return;
                    }
                }

                value(this, EventArgs.Empty);
            }
            remove
            {
                if (value is null) return;
                lock (_createdLock)
                {
                    _createdHandlers -= value;
                }
            }
        }

        /// <summary>
        /// The task representing asynchronous initialization, if it has been started.
        /// </summary>
        public Task? Initialization => Volatile.Read(ref _initTask);

        /// <summary>
        /// The exception captured when initialization failed (if any).
        /// </summary>
        public Exception? InitializationException => Volatile.Read(ref _initializationException);

        /// <summary>
        /// True if the instance has transitioned to the created/completed state.
        /// </summary>
        public bool IsCreated => Volatile.Read(ref _isCreated) == 1;

        /// <summary>
        /// True if initialization has been started but not yet completed.
        /// </summary>
        public bool IsInitializing => Volatile.Read(ref _initTask) != null && !Volatile.Read(ref _initTask)!.IsCompleted;

        /// <summary>
        /// Factory method to create the singleton instance and start async initialization. The instance is created synchronously on the first call, and async initialization is triggered once and only once. Subsequent calls return the already created instance. The provided base parameter is passed to both the synchronous and asynchronous creation hooks for flexible setup.
        /// </summary>
        /// <param name="base">Initialization parameter</param>
        /// <returns>The created instance as TInterface.</returns>
        public static TInterface Create(TBase @base)
        {
            EnsureCreated(@base);
            EnsureAsyncInitStarted(@base, CancellationToken.None);
            return _instance!;
        }

        /// <summary>
        /// Async factory that ensures creation and waits for asynchronous initialization to complete. The CancellationToken only affects waiting; the initialization task itself is controlled by the first caller that starts it.
        /// </summary>
        /// <param name="base">Initialization parameter</param>
        /// <param name="cancellationToken">Cancellation token for the caller waiting on completion.</param>
        /// <returns>The created instance as TInterface.</returns>
        public static async Task<TInterface> CreateAsync(TBase @base, CancellationToken cancellationToken = default)
        {
            EnsureCreated(@base);
            EnsureAsyncInitStarted(@base, cancellationToken);

            // Wait for the initialization task to complete. We rethrow any exceptions that fault the initialization.
            var task = Volatile.Read(ref _initTask);
            if (task is null)
                return _instance!; // nothing to wait for

            await WaitWithCancellation(task, cancellationToken).ConfigureAwait(false);

            // Propagate stored exception if present (await would already rethrow), return instance.
            return _instance!;
        }

        /// <summary>
        /// Initialization hook for synchronous setup during instance creation. This runs exactly once on the first call to Create before the instance is published.
        /// </summary>
        /// <param name="base">The base parameter for initialization.</param>
        protected abstract void OnCreate(TBase @base);

        /// <summary>
        /// Initialization hook for asynchronous setup during instance creation. This runs exactly once on the first call to Create before the instance is transitioned to created.
        /// </summary>
        /// <param name="base">The base parameter for initialization.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        protected abstract Task OnCreateAsync(TBase @base, CancellationToken cancellationToken);

        private static void EnsureAsyncInitStarted(TBase @base, CancellationToken cancellationToken)
        {
            if (Volatile.Read(ref _initTask) != null) return;

            lock (_sync)
            {
                if (_initTask != null) return;
                if (_instance is null) throw new InvalidOperationException("Instance not created.");

                // Start initialization using the provided token. Only the first caller's token is used by the initialization task.
                _initTask = _instance.InitializeAsync(@base, cancellationToken);
            }
        }

        private static void EnsureCreated(TBase @base)
        {
            if (_instance != null) return;

            lock (_sync)
            {
                if (_instance != null) return;
                var inst = new TClass();
                inst.OnCreate(@base);
                _instance = inst;
            }
        }

        private async Task InitializeAsync(TBase @base, CancellationToken cancellationToken)
        {
            try
            {
                await OnCreateAsync(@base, cancellationToken).ConfigureAwait(false);

                if (Interlocked.Exchange(ref _isCreated, 1) == 0)
                {
                    EventHandler? handlers;
                    lock (_createdLock)
                    {
                        handlers = _createdHandlers;
                        _createdHandlers = null;
                    }
                    handlers?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception ex)
            {
                Volatile.Write(ref _initializationException, ex);
                throw;
            }
        }

        /// <summary>
        /// Helper to await a task while honoring a caller cancellation token on platforms where Task.WaitAsync may not be available.
        /// </summary>
        private static async Task WaitWithCancellation(Task task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                await task.ConfigureAwait(false);
                return;
            }

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var tcs = new TaskCompletionSource<object?>();
            using (cancellationToken.Register(state => ((TaskCompletionSource<object?>)state!).TrySetCanceled(), tcs))
            {
                var completed = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
                if (completed == tcs.Task)
                    throw new OperationCanceledException(cancellationToken);
                await task.ConfigureAwait(false); // propagate exceptions if any
            }
        }

        /// <summary>
        /// Resets the static singleton and initialization state. Intended for unit tests only.</summary>
        internal static void ResetForTests()
        {
            lock (_sync)
            {
                _instance = null;
                _initTask = null;
                _initializationException = null;
                Volatile.Write(ref _isCreated, 0);
            }
        }

        // Explicit interface implementations to satisfy ICreateableAware (instance view of the singleton state).
        Task? ICreateableAware.Initialization => Initialization;

        Exception? ICreateableAware.InitializationException => InitializationException;

        bool ICreateableAware.IsCreated => IsCreated;

        bool ICreateableAware.IsInitializing => IsInitializing;
    }
}
