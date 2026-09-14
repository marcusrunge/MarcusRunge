using System.Runtime.CompilerServices;

namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains garbage collection and lifetime tests for scoped instances created by
    /// <see cref="CreateableBindableBase{TInterface, TClass, TBase}"/>.
    /// </summary>
    public class CreateableBindableBaseLifetimeTest
    {
        private interface ITestCreateable
        {
        }

        /// <summary>
        /// Verifies that the static scoped-state cache does not permanently retain a scope
        /// after the corresponding object graph is no longer referenced.
        /// </summary>
        [Fact]
        public void ScopeCanBeGarbageCollected()
        {
            var scopeReference = CreateScopeReference();

            ForceGarbageCollection(scopeReference);

            Assert.False(scopeReference.IsAlive);
        }

        /// <summary>
        /// Verifies that the static scoped-state cache does not permanently retain either
        /// the scoped instance or its scope after the complete object graph becomes unreachable.
        /// </summary>
        [Fact]
        public void ScopedInstanceDoesNotKeepScopeAliveUnexpectedly()
        {
            var references = CreateScopedGraphReferences();

            ForceGarbageCollection(references.Scope, references.Instance);

            Assert.False(references.Scope.IsAlive);
            Assert.False(references.Instance.IsAlive);
        }

        /// <summary>
        /// Verifies that a reachable scoped instance retains its initialization context
        /// for the lifetime of the instance.
        /// </summary>
        [Fact]
        public void ReachableScopedInstance_KeepsInitializationContextAlive()
        {
            var references = CreateReachableScopedInstance();

            ForceGarbageCollection(references.Scope);

            Assert.True(references.Scope.IsAlive);
            Assert.NotNull(references.Instance);

            GC.KeepAlive(references.Instance);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference CreateScopeReference()
        {
            var scope = new object();
            _ = GarbageCollectableCreateable.Create(scope, CreationLifetime.Scoped);

            return new WeakReference(scope);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ScopedGraphReferences CreateScopedGraphReferences()
        {
            var scope = new object();
            var instance = GarbageCollectableCreateable.Create(scope, CreationLifetime.Scoped);

            return new ScopedGraphReferences(new WeakReference(scope), new WeakReference(instance));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ReachableScopedInstance CreateReachableScopedInstance()
        {
            var scope = new object();
            var instance = GarbageCollectableCreateable.Create(scope, CreationLifetime.Scoped);

            return new ReachableScopedInstance(new WeakReference(scope), instance);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ForceGarbageCollection(params WeakReference[] references)
        {
            const int maximumAttempts = 10;

            for (var attempt = 0; attempt < maximumAttempts && references.Any(reference => reference.IsAlive); attempt++)
            {
                // A full blocking collection and finalizer drain make the weak-lifetime assertion
                // independent of ordinary background collection timing.
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
                GC.WaitForPendingFinalizers();
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true);
            }
        }

        private sealed class GarbageCollectableCreateable : CreateableBindableBase<ITestCreateable, GarbageCollectableCreateable, object>, ITestCreateable
        {
            protected override void OnCreate(object context)
            {
            }

            protected override Task OnCreateAsync(object context, CancellationToken cancellationToken) => Task.CompletedTask;
        }

        private sealed class ReachableScopedInstance(WeakReference scope, ITestCreateable instance)
        {
            public ITestCreateable Instance { get; } = instance;

            public WeakReference Scope { get; } = scope;
        }

        private sealed class ScopedGraphReferences(WeakReference scope, WeakReference instance)
        {
            public WeakReference Instance { get; } = instance;

            public WeakReference Scope { get; } = scope;
        }
    }
}