namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains failure-policy and reentrancy tests for
    /// <see cref="CreateableBindableBase{TInterface, TClass, TBase}"/>.
    /// </summary>
    public class CreateableBindableBaseFailureTest
    {
        private interface ITestCreateable
        {
        }

        /// <summary>
        /// Verifies that an exception thrown by synchronous initialization faults the creation state.
        /// </summary>
        [Fact]
        public void OnCreateFailure_IsStoredForCreationState()
        {
            var context = new object();

            var firstException = Assert.Throws<InvalidOperationException>(() => SynchronousFailingCreateable.Create(context));
            var secondException = Assert.Throws<InvalidOperationException>(() => SynchronousFailingCreateable.Create(context));

            Assert.Equal("Synchronous initialization failed.", firstException.Message);
            Assert.Same(firstException, secondException);
            Assert.Equal(1, SynchronousFailingCreateable.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that a failed synchronous scoped state does not affect another scope.
        /// </summary>
        [Fact]
        public void OnCreateFailure_IsIsolatedPerScope()
        {
            var failingContext = new FailureContext(true);
            var successfulContext = new FailureContext(false);

            Assert.Throws<InvalidOperationException>(() => ScopedSynchronousFailureCreateable.Create(failingContext, CreationLifetime.Scoped));

            var successful = ScopedSynchronousFailureCreateable.Create(successfulContext, CreationLifetime.Scoped);

            Assert.NotNull(successful);
            Assert.Equal(2, ScopedSynchronousFailureCreateable.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that a synchronous exception while starting asynchronous initialization
        /// faults the creation state exactly once.
        /// </summary>
        [Fact]
        public void SynchronousOnCreateAsyncFailure_IsStoredForCreationState()
        {
            var context = new object();

            var firstException = Assert.Throws<InvalidOperationException>(() => SynchronouslyFailingAsyncCreateable.Create(context));
            var secondException = Assert.Throws<InvalidOperationException>(() => SynchronouslyFailingAsyncCreateable.Create(context));

            Assert.Equal("Starting asynchronous initialization failed.", firstException.Message);
            Assert.Same(firstException, secondException);
            Assert.Equal(1, SynchronouslyFailingAsyncCreateable.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that a fault occurring after asynchronous initialization started remains
        /// stored in the affected creation state.
        /// </summary>
        [Fact]
        public async Task AsynchronousOnCreateAsyncFailure_IsStoredForCreationState()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var instance = (AsynchronouslyFailingCreateable)AsynchronouslyFailingCreateable.Create(context);
            var initialization = Assert.IsType<Task>(instance.Initialization, exactMatch: false);

            AsynchronouslyFailingCreateable.InitializationFailure.TrySetException(
                new InvalidOperationException("Asynchronous initialization failed."));

            var firstException = await Assert.ThrowsAsync<InvalidOperationException>(() => initialization.WaitAsync(cancellationToken));
            var secondException = await Assert.ThrowsAsync<InvalidOperationException>(
                () => AsynchronouslyFailingCreateable.CreateAsync(context, cancellationToken));

            Assert.Equal("Asynchronous initialization failed.", firstException.Message);
            Assert.Same(firstException, secondException);
            Assert.Same(firstException, instance.InitializationException);
            Assert.False(instance.IsCreated);
            Assert.False(instance.IsInitializing);
            Assert.Equal(1, AsynchronouslyFailingCreateable.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that recursive creation from synchronous initialization fails immediately
        /// instead of waiting indefinitely for the same creation state.
        /// </summary>
        [Fact]
        public void RecursiveCreateFromOnCreate_IsRejected()
        {
            var context = new object();

            var exception = Assert.Throws<InvalidOperationException>(() => RecursiveOnCreateCreateable.Create(context));

            Assert.Contains("Recursive creation", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, RecursiveOnCreateCreateable.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that recursive creation from the synchronous beginning of asynchronous initialization
        /// fails immediately instead of waiting indefinitely for the same creation state.
        /// </summary>
        [Fact]
        public void RecursiveCreateFromSynchronousOnCreateAsyncStart_IsRejected()
        {
            var context = new object();

            var exception = Assert.Throws<InvalidOperationException>(() => RecursiveOnCreateAsyncCreateable.Create(context));

            Assert.Contains("Recursive creation", exception.Message, StringComparison.Ordinal);
            Assert.Equal(1, RecursiveOnCreateAsyncCreateable.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that creation of a different scoped state remains possible from synchronous initialization.
        /// </summary>
        [Fact]
        public void DifferentScopedStateCreationFromOnCreate_IsAllowed()
        {
            var context = new NestedScopeContext(new object());

            var outer = NestedScopedCreateable.Create(context, CreationLifetime.Scoped);

            Assert.NotNull(outer);
            Assert.NotNull(context.NestedInstance);
            Assert.Equal(1, NestedScopedCreateable.SyncCreateCount);
            Assert.Equal(1, NestedDependencyCreateable.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that transient creation remains independent and does not trigger same-state reentrancy detection.
        /// </summary>
        [Fact]
        public void TransientCreationFromOnCreate_IsAllowed()
        {
            var context = new TransientNestedContext();

            var outer = TransientNestedCreateable.Create(context, CreationLifetime.Transient);

            Assert.NotNull(outer);
            Assert.NotNull(context.NestedInstance);
            Assert.NotSame(outer, context.NestedInstance);
            Assert.Equal(2, TransientNestedCreateable.SyncCreateCount);
        }

        private sealed class SynchronousFailingCreateable : CreateableBindableBase<ITestCreateable, SynchronousFailingCreateable, object>, ITestCreateable
        {
            public static int SyncCreateCount;

            protected override void OnCreate(object context)
            {
                Interlocked.Increment(ref SyncCreateCount);
                throw new InvalidOperationException("Synchronous initialization failed.");
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class ScopedSynchronousFailureCreateable : CreateableBindableBase<ITestCreateable, ScopedSynchronousFailureCreateable, FailureContext>, ITestCreateable
        {
            public static int SyncCreateCount;

            protected override void OnCreate(FailureContext context)
            {
                Interlocked.Increment(ref SyncCreateCount);

                if (context.ShouldFail)
                    throw new InvalidOperationException("Scoped synchronous initialization failed.");
            }

            protected override Task OnCreateAsync(FailureContext context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class SynchronouslyFailingAsyncCreateable : CreateableBindableBase<ITestCreateable, SynchronouslyFailingAsyncCreateable, object>, ITestCreateable
        {
            public static int AsyncCreateCount;

            protected override void OnCreate(object context)
            {
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref AsyncCreateCount);
                throw new InvalidOperationException("Starting asynchronous initialization failed.");
            }
        }

        private sealed class AsynchronouslyFailingCreateable : CreateableBindableBase<ITestCreateable, AsynchronouslyFailingCreateable, object>, ITestCreateable
        {
            public static readonly TaskCompletionSource InitializationFailure = new(TaskCreationOptions.RunContinuationsAsynchronously);

            public static int AsyncCreateCount;

            protected override void OnCreate(object context)
            {
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref AsyncCreateCount);
                return InitializationFailure.Task;
            }
        }

        private sealed class RecursiveOnCreateCreateable : CreateableBindableBase<ITestCreateable, RecursiveOnCreateCreateable, object>, ITestCreateable
        {
            public static int SyncCreateCount;

            protected override void OnCreate(object context)
            {
                Interlocked.Increment(ref SyncCreateCount);
                _ = Create(context);
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class RecursiveOnCreateAsyncCreateable : CreateableBindableBase<ITestCreateable, RecursiveOnCreateAsyncCreateable, object>, ITestCreateable
        {
            public static int AsyncCreateCount;

            protected override void OnCreate(object context)
            {
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref AsyncCreateCount);
                _ = Create(context);
                return Task.CompletedTask;
            }
        }

        private sealed class NestedScopedCreateable : CreateableBindableBase<ITestCreateable, NestedScopedCreateable, NestedScopeContext>, ITestCreateable
        {
            public static int SyncCreateCount;

            protected override void OnCreate(NestedScopeContext context)
            {
                Interlocked.Increment(ref SyncCreateCount);
                context.NestedInstance = NestedDependencyCreateable.Create(context.NestedContext, CreationLifetime.Scoped);
            }

            protected override Task OnCreateAsync(NestedScopeContext context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class NestedDependencyCreateable : CreateableBindableBase<ITestCreateable, NestedDependencyCreateable, object>, ITestCreateable
        {
            public static int SyncCreateCount;

            protected override void OnCreate(object context) => Interlocked.Increment(ref SyncCreateCount);

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class TransientNestedCreateable : CreateableBindableBase<ITestCreateable, TransientNestedCreateable, TransientNestedContext>, ITestCreateable
        {
            public static int SyncCreateCount;

            protected override void OnCreate(TransientNestedContext context)
            {
                Interlocked.Increment(ref SyncCreateCount);

                context.NestedInstance ??= Create(context, CreationLifetime.Transient);
            }

            protected override Task OnCreateAsync(TransientNestedContext context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class FailureContext(bool shouldFail)
        {
            public bool ShouldFail { get; } = shouldFail;
        }

        private sealed class NestedScopeContext(object nestedContext)
        {
            public object NestedContext { get; } = nestedContext;

            public ITestCreateable? NestedInstance { get; set; }
        }

        private sealed class TransientNestedContext
        {
            public ITestCreateable? NestedInstance { get; set; }
        }
    }
}