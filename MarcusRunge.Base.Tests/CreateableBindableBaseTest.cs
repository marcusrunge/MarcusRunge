using System.ComponentModel;

namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains unit tests for <see cref="CreateableBindableBase{TInterface, TClass, TBase}"/>.
    /// </summary>
    public class CreateableBindableBaseTest
    {
        private interface ITestCreateable
        {
        }

        /// <summary>
        /// Verifies that the existing default creation API uses the global lifetime.
        /// </summary>
        [Fact]
        public void GlobalCreate_ReturnsSameInstance()
        {
            var context = new object();

            var first = TestCreateable<GlobalCreateScenario>.Create(context);
            var second = TestCreateable<GlobalCreateScenario>.Create(context);

            Assert.Same(first, second);
            Assert.Equal(1, TestCreateable<GlobalCreateScenario>.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that the existing asynchronous creation API uses the global lifetime.
        /// </summary>
        [Fact]
        public async Task GlobalCreateAsync_ReturnsSameInstance()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();

            var first = await TestCreateable<GlobalCreateAsyncScenario>.CreateAsync(context, cancellationToken);
            var second = await TestCreateable<GlobalCreateAsyncScenario>.CreateAsync(context, cancellationToken);

            Assert.Same(first, second);
            Assert.Equal(1, TestCreateable<GlobalCreateAsyncScenario>.SyncCreateCount);
            Assert.Equal(1, TestCreateable<GlobalCreateAsyncScenario>.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that the same scoped context reference returns the same instance.
        /// </summary>
        [Fact]
        public void SameScope_ReturnsSameInstance()
        {
            var context = new object();

            var first = TestCreateable<SameScopeScenario>.Create(context, CreationLifetime.Scoped);
            var second = TestCreateable<SameScopeScenario>.Create(context, CreationLifetime.Scoped);

            Assert.Same(first, second);
            Assert.Equal(1, TestCreateable<SameScopeScenario>.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that different scoped context references return different instances.
        /// </summary>
        [Fact]
        public void DifferentScopes_ReturnDifferentInstances()
        {
            var firstContext = new object();
            var secondContext = new object();

            var first = TestCreateable<DifferentScopesScenario>.Create(firstContext, CreationLifetime.Scoped);
            var second = TestCreateable<DifferentScopesScenario>.Create(secondContext, CreationLifetime.Scoped);

            Assert.NotSame(first, second);
            Assert.Equal(2, TestCreateable<DifferentScopesScenario>.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that global and scoped creation use independent creation states.
        /// </summary>
        [Fact]
        public void GlobalAndScopedCreation_ReturnDifferentInstances()
        {
            var context = new object();

            var global = TestCreateable<GlobalAndScopedScenario>.Create(context);
            var scoped = TestCreateable<GlobalAndScopedScenario>.Create(context, CreationLifetime.Scoped);

            Assert.NotSame(global, scoped);
            Assert.Equal(2, TestCreateable<GlobalAndScopedScenario>.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that transient creation returns a new instance for every call.
        /// </summary>
        [Fact]
        public void TransientCreate_ReturnsNewInstancePerCall()
        {
            var context = new object();

            var first = TestCreateable<TransientScenario>.Create(context, CreationLifetime.Transient);
            var second = TestCreateable<TransientScenario>.Create(context, CreationLifetime.Transient);

            Assert.NotSame(first, second);
            Assert.Equal(2, TestCreateable<TransientScenario>.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that concurrent global creation creates exactly one instance.
        /// </summary>
        [Fact]
        public async Task ParallelGlobalCreate_CreatesExactlyOneInstance()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var startSignal = CreateCompletionSource();

            var tasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(async () =>
                {
                    await startSignal.Task.WaitAsync(cancellationToken);
                    return TestCreateable<ParallelGlobalScenario>.Create(context);
                }, cancellationToken))
                .ToArray();

            startSignal.TrySetResult();

            var results = await Task.WhenAll(tasks).WaitAsync(cancellationToken);
            var first = results[0];

            Assert.All(results, result => Assert.Same(first, result));
            Assert.Equal(1, TestCreateable<ParallelGlobalScenario>.SyncCreateCount);
            Assert.Equal(1, TestCreateable<ParallelGlobalScenario>.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that concurrent scoped creation creates exactly one instance for the same context.
        /// </summary>
        [Fact]
        public async Task ParallelSameScopeCreate_CreatesExactlyOneInstance()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var startSignal = CreateCompletionSource();

            var tasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(async () =>
                {
                    await startSignal.Task.WaitAsync(cancellationToken);
                    return TestCreateable<ParallelSameScopeScenario>.Create(context, CreationLifetime.Scoped);
                }, cancellationToken))
                .ToArray();

            startSignal.TrySetResult();

            var results = await Task.WhenAll(tasks).WaitAsync(cancellationToken);
            var first = results[0];

            Assert.All(results, result => Assert.Same(first, result));
            Assert.Equal(1, TestCreateable<ParallelSameScopeScenario>.SyncCreateCount);
            Assert.Equal(1, TestCreateable<ParallelSameScopeScenario>.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that concurrent creation of different scopes creates one instance per context.
        /// </summary>
        [Fact]
        public async Task ParallelDifferentScopeCreate_CreatesOneInstancePerScope()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var firstContext = new object();
            var secondContext = new object();
            var startSignal = CreateCompletionSource();

            var firstTasks = CreateScopedTasks<ParallelDifferentScopesScenario>(firstContext, startSignal.Task, cancellationToken);
            var secondTasks = CreateScopedTasks<ParallelDifferentScopesScenario>(secondContext, startSignal.Task, cancellationToken);

            startSignal.TrySetResult();

            var firstResults = await Task.WhenAll(firstTasks).WaitAsync(cancellationToken);
            var secondResults = await Task.WhenAll(secondTasks).WaitAsync(cancellationToken);

            Assert.All(firstResults, result => Assert.Same(firstResults[0], result));
            Assert.All(secondResults, result => Assert.Same(secondResults[0], result));
            Assert.NotSame(firstResults[0], secondResults[0]);
            Assert.Equal(2, TestCreateable<ParallelDifferentScopesScenario>.SyncCreateCount);
            Assert.Equal(2, TestCreateable<ParallelDifferentScopesScenario>.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that synchronous initialization runs once per creation state.
        /// </summary>
        [Fact]
        public void OnCreate_RunsExactlyOncePerCreationState()
        {
            var globalContext = new object();
            var firstScopedContext = new object();
            var secondScopedContext = new object();

            _ = TestCreateable<OnCreateCountScenario>.Create(globalContext);
            _ = TestCreateable<OnCreateCountScenario>.Create(globalContext);
            _ = TestCreateable<OnCreateCountScenario>.Create(firstScopedContext, CreationLifetime.Scoped);
            _ = TestCreateable<OnCreateCountScenario>.Create(firstScopedContext, CreationLifetime.Scoped);
            _ = TestCreateable<OnCreateCountScenario>.Create(secondScopedContext, CreationLifetime.Scoped);
            _ = TestCreateable<OnCreateCountScenario>.Create(globalContext, CreationLifetime.Transient);

            Assert.Equal(4, TestCreateable<OnCreateCountScenario>.SyncCreateCount);
        }

        /// <summary>
        /// Verifies that asynchronous initialization runs once per creation state.
        /// </summary>
        [Fact]
        public async Task OnCreateAsync_RunsExactlyOncePerCreationState()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var globalContext = new object();
            var scopedContext = new object();

            _ = await TestCreateable<OnCreateAsyncCountScenario>.CreateAsync(globalContext, cancellationToken);
            _ = await TestCreateable<OnCreateAsyncCountScenario>.CreateAsync(globalContext, cancellationToken);
            _ = await TestCreateable<OnCreateAsyncCountScenario>.CreateAsync(scopedContext, CreationLifetime.Scoped, cancellationToken);
            _ = await TestCreateable<OnCreateAsyncCountScenario>.CreateAsync(scopedContext, CreationLifetime.Scoped, cancellationToken);
            _ = await TestCreateable<OnCreateAsyncCountScenario>.CreateAsync(globalContext, CreationLifetime.Transient, cancellationToken);

            Assert.Equal(3, TestCreateable<OnCreateAsyncCountScenario>.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that a different context is rejected for the same global creation state.
        /// </summary>
        [Fact]
        public void DifferentContextForSameState_IsRejected()
        {
            var firstContext = new object();
            var secondContext = new object();

            _ = TestCreateable<DifferentGlobalContextScenario>.Create(firstContext);

            var exception = Assert.Throws<InvalidOperationException>(() => TestCreateable<DifferentGlobalContextScenario>.Create(secondContext));

            Assert.Contains(nameof(CreationLifetime.Scoped), exception.Message, StringComparison.Ordinal);
            Assert.Contains(nameof(CreationLifetime.Transient), exception.Message, StringComparison.Ordinal);
        }

        /// <summary>
        /// Verifies that different contexts are accepted when they identify different scopes.
        /// </summary>
        [Fact]
        public void DifferentContextInDifferentScopes_IsAccepted()
        {
            var firstContext = new object();
            var secondContext = new object();

            var first = TestCreateable<DifferentScopedContextScenario>.Create(firstContext, CreationLifetime.Scoped);
            var second = TestCreateable<DifferentScopedContextScenario>.Create(secondContext, CreationLifetime.Scoped);

            Assert.NotSame(first, second);
        }

        /// <summary>
        /// Verifies that value-type contexts cannot be used as weak scope identities.
        /// </summary>
        [Fact]
        public void ScopedCreation_WithValueTypeContext_IsRejected()
        {
            var exception = Assert.Throws<InvalidOperationException>(() => ValueContextCreateable.Create(42, CreationLifetime.Scoped));

            Assert.Contains("reference type", exception.Message, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Verifies that caller cancellation does not cancel shared initialization.
        /// </summary>
        [Fact]
        public async Task WaitingCancellation_DoesNotCancelSharedInitialization()
        {
            var testCancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            using var waiterCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(testCancellationToken);

            var canceledWaiter = ControlledCreateable<WaitingCancellationScenario>.CreateAsync(context, waiterCancellationSource.Token);

            await ControlledCreateable<WaitingCancellationScenario>.InitializationEntered.Task.WaitAsync(testCancellationToken);

            waiterCancellationSource.Cancel();

            var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWaiter);

            Assert.Equal(waiterCancellationSource.Token, exception.CancellationToken);
            Assert.False(ControlledCreateable<WaitingCancellationScenario>.ObservedInitializationToken.CanBeCanceled);
            Assert.False(ControlledCreateable<WaitingCancellationScenario>.InitializationCompletion.Task.IsCompleted);

            ControlledCreateable<WaitingCancellationScenario>.InitializationCompletion.TrySetResult();

            var instance = await ControlledCreateable<WaitingCancellationScenario>.CreateAsync(context, testCancellationToken);

            Assert.True(((ControlledCreateable<WaitingCancellationScenario>)instance).IsCreated);
            Assert.Equal(1, ControlledCreateable<WaitingCancellationScenario>.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that canceling one waiter does not affect another waiter.
        /// </summary>
        [Fact]
        public async Task CanceledWaiter_DoesNotAffectOtherWaiter()
        {
            var testCancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            using var firstWaiterCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(testCancellationToken);

            var firstWaiter = ControlledCreateable<CanceledWaiterScenario>.CreateAsync(context, firstWaiterCancellationSource.Token);
            var secondWaiter = ControlledCreateable<CanceledWaiterScenario>.CreateAsync(context, testCancellationToken);

            await ControlledCreateable<CanceledWaiterScenario>.InitializationEntered.Task.WaitAsync(testCancellationToken);

            firstWaiterCancellationSource.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => firstWaiter);

            Assert.False(secondWaiter.IsCompleted);

            ControlledCreateable<CanceledWaiterScenario>.InitializationCompletion.TrySetResult();

            var secondResult = await secondWaiter;

            Assert.NotNull(secondResult);
            Assert.Equal(1, ControlledCreateable<CanceledWaiterScenario>.AsyncCreateCount);
        }

        /// <summary>
        /// Verifies that an initialization failure remains isolated to its scoped state.
        /// </summary>
        [Fact]
        public async Task FailedScope_DoesNotAffectOtherScope()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var failingContext = new FailureContext(true);
            var successfulContext = new FailureContext(false);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                FailureControlledCreateable.CreateAsync(failingContext, CreationLifetime.Scoped, cancellationToken));

            var successful = await FailureControlledCreateable.CreateAsync(successfulContext, CreationLifetime.Scoped, cancellationToken);

            Assert.NotNull(successful);
            Assert.True(((FailureControlledCreateable)successful).IsCreated);
        }

        /// <summary>
        /// Verifies that a failed scoped state does not affect the global state.
        /// </summary>
        [Fact]
        public async Task FailedScopedInitialization_DoesNotAffectGlobalState()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var failingContext = new FailureContext(true);
            var successfulContext = new FailureContext(false);

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                FailureControlledCreateable.CreateAsync(failingContext, CreationLifetime.Scoped, cancellationToken));

            var global = await FailureControlledCreateable.CreateAsync(successfulContext, cancellationToken);

            Assert.NotNull(global);
            Assert.True(((FailureControlledCreateable)global).IsCreated);
        }

        /// <summary>
        /// Verifies that created state belongs to the individual creation state.
        /// </summary>
        [Fact]
        public async Task IsCreated_IsStateSpecific()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var controlledContext = new object();
            var completedContext = new object();

            var controlled = (ControlledCreateable<StateSpecificCreatedScenario>)ControlledCreateable<StateSpecificCreatedScenario>.Create(
                controlledContext,
                CreationLifetime.Scoped);

            var completed = (TestCreateable<StateSpecificCompletedScenario>)await TestCreateable<StateSpecificCompletedScenario>.CreateAsync(
                completedContext,
                CreationLifetime.Scoped,
                cancellationToken);

            await ControlledCreateable<StateSpecificCreatedScenario>.InitializationEntered.Task.WaitAsync(cancellationToken);

            Assert.False(controlled.IsCreated);
            Assert.True(completed.IsCreated);

            ControlledCreateable<StateSpecificCreatedScenario>.InitializationCompletion.TrySetResult();

            await WaitForInitializationAsync(controlled, cancellationToken);

            Assert.True(controlled.IsCreated);
        }

        /// <summary>
        /// Verifies that initializing state belongs to the individual creation state.
        /// </summary>
        [Fact]
        public async Task IsInitializing_IsStateSpecific()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var firstContext = new object();
            var secondContext = new object();

            var first = (ControlledCreateable<StateSpecificInitializingScenario>)ControlledCreateable<StateSpecificInitializingScenario>.Create(
                firstContext,
                CreationLifetime.Scoped);

            await ControlledCreateable<StateSpecificInitializingScenario>.InitializationEntered.Task.WaitAsync(cancellationToken);

            var second = (TestCreateable<StateSpecificNotInitializingScenario>)await TestCreateable<StateSpecificNotInitializingScenario>.CreateAsync(
                secondContext,
                CreationLifetime.Scoped,
                cancellationToken);

            Assert.True(first.IsInitializing);
            Assert.False(second.IsInitializing);

            ControlledCreateable<StateSpecificInitializingScenario>.InitializationCompletion.TrySetResult();

            await WaitForInitializationAsync(first, cancellationToken);
        }

        /// <summary>
        /// Verifies that initialization failures belong to the individual creation state.
        /// </summary>
        [Fact]
        public async Task InitializationException_IsStateSpecific()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var failingContext = new FailureContext(true);
            var successfulContext = new FailureContext(false);

            var failing = (FailureControlledCreateable)FailureControlledCreateable.Create(
                failingContext,
                CreationLifetime.Scoped);

            var failingInitialization = Assert.IsAssignableFrom<Task>(failing.Initialization);

            await Assert.ThrowsAsync<InvalidOperationException>(() => failingInitialization.WaitAsync(cancellationToken));

            var successful = (FailureControlledCreateable)await FailureControlledCreateable.CreateAsync(successfulContext, CreationLifetime.Scoped, cancellationToken);

            Assert.IsType<InvalidOperationException>(failing.InitializationException);
            Assert.Null(successful.InitializationException);
        }

        /// <summary>
        /// Verifies that the created event is raised exactly once.
        /// </summary>
        [Fact]
        public async Task OnCreated_RaisedExactlyOnce()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var instance = (ControlledCreateable<OnCreatedOnceScenario>)ControlledCreateable<OnCreatedOnceScenario>.Create(context);
            var invocationCount = 0;

            instance.OnCreated += (_, _) => Interlocked.Increment(ref invocationCount);

            await ControlledCreateable<OnCreatedOnceScenario>.InitializationEntered.Task.WaitAsync(cancellationToken);

            ControlledCreateable<OnCreatedOnceScenario>.InitializationCompletion.TrySetResult();

            await WaitForInitializationAsync(instance, cancellationToken);
            _ = ControlledCreateable<OnCreatedOnceScenario>.Create(context);

            Assert.Equal(1, invocationCount);
        }

        /// <summary>
        /// Verifies that a subscriber registered after creation is invoked immediately.
        /// </summary>
        [Fact]
        public async Task LateOnCreatedSubscriber_IsInvokedImmediately()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var instance = (TestCreateable<LateSubscriberScenario>)await TestCreateable<LateSubscriberScenario>.CreateAsync(
                context,
                cancellationToken);

            var invocationCount = 0;

            instance.OnCreated += (_, _) => Interlocked.Increment(ref invocationCount);

            Assert.Equal(1, invocationCount);
        }

        /// <summary>
        /// Verifies that a failing subscriber does not fail successful initialization.
        /// </summary>
        [Fact]
        public async Task FailingOnCreatedSubscriber_DoesNotFailInitialization()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var instance = (SubscriberAwareCreateable<FailingSubscriberScenario>)SubscriberAwareCreateable<FailingSubscriberScenario>.Create(context);

            instance.OnCreated += (_, _) => throw new InvalidOperationException("Subscriber failure");

            await SubscriberAwareCreateable<FailingSubscriberScenario>.InitializationEntered.Task.WaitAsync(cancellationToken);

            SubscriberAwareCreateable<FailingSubscriberScenario>.InitializationCompletion.TrySetResult();

            await WaitForInitializationAsync(instance, cancellationToken);

            Assert.True(instance.IsCreated);
            Assert.Null(instance.InitializationException);
            Assert.Equal(1, instance.ReportedSubscriberExceptionCount);
        }

        /// <summary>
        /// Verifies that a failing subscriber does not block subsequent subscribers.
        /// </summary>
        [Fact]
        public async Task FailingOnCreatedSubscriber_DoesNotBlockOtherSubscribers()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var instance = (SubscriberAwareCreateable<MultipleSubscribersScenario>)SubscriberAwareCreateable<MultipleSubscribersScenario>.Create(context);
            var successfulSubscriberInvocationCount = 0;

            instance.OnCreated += (_, _) => throw new InvalidOperationException("Subscriber failure");
            instance.OnCreated += (_, _) => Interlocked.Increment(ref successfulSubscriberInvocationCount);

            await SubscriberAwareCreateable<MultipleSubscribersScenario>.InitializationEntered.Task.WaitAsync(cancellationToken);

            SubscriberAwareCreateable<MultipleSubscribersScenario>.InitializationCompletion.TrySetResult();

            await WaitForInitializationAsync(instance, cancellationToken);

            Assert.Equal(1, successfulSubscriberInvocationCount);
            Assert.Equal(1, instance.ReportedSubscriberExceptionCount);
        }

        /// <summary>
        /// Verifies that resetting a global state during initialization is rejected.
        /// </summary>
        [Fact]
        public async Task ResetDuringInitialization_IsRejectedDeterministically()
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var context = new object();
            var instance = (ControlledCreateable<ResetScenario>)ControlledCreateable<ResetScenario>.Create(context);

            await ControlledCreateable<ResetScenario>.InitializationEntered.Task.WaitAsync(cancellationToken);

            var exception = Assert.Throws<InvalidOperationException>(
                ControlledCreateable<ResetScenario>.ResetGlobalStateForTests);

            Assert.Contains("cannot be reset", exception.Message, StringComparison.OrdinalIgnoreCase);

            ControlledCreateable<ResetScenario>.InitializationCompletion.TrySetResult();

            await WaitForInitializationAsync(instance, cancellationToken);

            ControlledCreateable<ResetScenario>.ResetGlobalStateForTests();
        }

        /// <summary>
        /// Verifies that an unsupported lifetime value is rejected.
        /// </summary>
        [Fact]
        public void InvalidLifetime_IsRejected()
        {
            var context = new object();
            var lifetime = (CreationLifetime)int.MaxValue;

            Assert.Throws<InvalidEnumArgumentException>(() =>
                TestCreateable<InvalidLifetimeScenario>.Create(context, lifetime));
        }

        private static Task<ITestCreateable>[] CreateScopedTasks<TScenario>(
            object context,
            Task startSignal,
            CancellationToken cancellationToken) =>
            Enumerable.Range(0, 10)
                .Select(_ => Task.Run(async () =>
                {
                    await startSignal.WaitAsync(cancellationToken);
                    return TestCreateable<TScenario>.Create(context, CreationLifetime.Scoped);
                }, cancellationToken))
                .ToArray();

        private static TaskCompletionSource CreateCompletionSource() =>
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        private static async Task WaitForInitializationAsync(ICreateableAware createable, CancellationToken cancellationToken)
        {
            var initialization = createable.Initialization;

            Assert.NotNull(initialization);

            await initialization.WaitAsync(cancellationToken);
        }

        private sealed class TestCreateable<TScenario> : CreateableBindableBase<ITestCreateable, TestCreateable<TScenario>, object>, ITestCreateable
        {
            public static int AsyncCreateCount;
            public static int SyncCreateCount;

            protected override void OnCreate(object context) => Interlocked.Increment(ref SyncCreateCount);

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken)
            {
                Assert.False(cancellationToken.CanBeCanceled);
                Interlocked.Increment(ref AsyncCreateCount);
                return Task.CompletedTask;
            }
        }

        private sealed class ControlledCreateable<TScenario> : CreateableBindableBase<ITestCreateable, ControlledCreateable<TScenario>, object>, ITestCreateable
        {
            public static readonly TaskCompletionSource InitializationCompletion = CreateCompletionSource();
            public static readonly TaskCompletionSource InitializationEntered = CreateCompletionSource();

            public static int AsyncCreateCount;
            public static CancellationToken ObservedInitializationToken;

            protected override void OnCreate(object context)
            {
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken)
            {
                ObservedInitializationToken = cancellationToken;
                Interlocked.Increment(ref AsyncCreateCount);
                InitializationEntered.TrySetResult();
                return InitializationCompletion.Task;
            }
        }

        private sealed class SubscriberAwareCreateable<TScenario> : CreateableBindableBase<ITestCreateable, SubscriberAwareCreateable<TScenario>, object>, ITestCreateable
        {
            public static readonly TaskCompletionSource InitializationCompletion = CreateCompletionSource();
            public static readonly TaskCompletionSource InitializationEntered = CreateCompletionSource();

            private int _reportedSubscriberExceptionCount;

            public int ReportedSubscriberExceptionCount => Volatile.Read(ref _reportedSubscriberExceptionCount);

            protected override void OnCreate(object context)
            {
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken)
            {
                InitializationEntered.TrySetResult();
                return InitializationCompletion.Task;
            }

            protected override void OnCreatedSubscriberException(Exception exception)
            {
                Assert.IsType<InvalidOperationException>(exception);
                Interlocked.Increment(ref _reportedSubscriberExceptionCount);
            }
        }

        private sealed class FailureControlledCreateable : CreateableBindableBase<ITestCreateable, FailureControlledCreateable, FailureContext>, ITestCreateable
        {
            protected override void OnCreate(FailureContext context)
            {
            }

            protected override Task OnCreateAsync(FailureContext context, CancellationToken cancellationToken) =>
                context.ShouldFail
                    ? Task.FromException(new InvalidOperationException("Initialization failed."))
                    : Task.CompletedTask;
        }

        private sealed class ValueContextCreateable : CreateableBindableBase<ITestCreateable, ValueContextCreateable, int>, ITestCreateable
        {
            protected override void OnCreate(int context)
            {
            }

            protected override Task OnCreateAsync(int context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class FailureContext(bool shouldFail)
        {
            public bool ShouldFail { get; } = shouldFail;
        }

        private sealed class CanceledWaiterScenario
        {
        }

        private sealed class DifferentGlobalContextScenario
        {
        }

        private sealed class DifferentScopedContextScenario
        {
        }

        private sealed class DifferentScopesScenario
        {
        }

        private sealed class FailingSubscriberScenario
        {
        }

        private sealed class GlobalAndScopedScenario
        {
        }

        private sealed class GlobalCreateAsyncScenario
        {
        }

        private sealed class GlobalCreateScenario
        {
        }

        private sealed class InvalidLifetimeScenario
        {
        }

        private sealed class LateSubscriberScenario
        {
        }

        private sealed class MultipleSubscribersScenario
        {
        }

        private sealed class OnCreateAsyncCountScenario
        {
        }

        private sealed class OnCreateCountScenario
        {
        }

        private sealed class OnCreatedOnceScenario
        {
        }

        private sealed class ParallelDifferentScopesScenario
        {
        }

        private sealed class ParallelGlobalScenario
        {
        }

        private sealed class ParallelSameScopeScenario
        {
        }

        private sealed class ResetScenario
        {
        }

        private sealed class SameScopeScenario
        {
        }

        private sealed class StateSpecificCompletedScenario
        {
        }

        private sealed class StateSpecificCreatedScenario
        {
        }

        private sealed class StateSpecificInitializingScenario
        {
        }

        private sealed class StateSpecificNotInitializingScenario
        {
        }

        private sealed class TransientScenario
        {
        }

        private sealed class WaitingCancellationScenario
        {
        }
    }
}