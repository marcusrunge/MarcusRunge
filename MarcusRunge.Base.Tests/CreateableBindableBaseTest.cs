using System.Reflection;

namespace MarcusRunge.Base.Test
{
    /// <summary>
    /// Contains unit tests for the <see cref="CreateableBindableBase{TBase, TConcrete, TInitializationResult}"/> class.
    /// </summary>
    public class CreateableBindableBaseTest
    {
        private interface ITestCreateable
        { }

        /// <summary>
        /// Creates the should call on create exactly once.
        /// </summary>
        [Fact]
        public void Create_ShouldCallOnCreate_ExactlyOnce()
        {
            // Arrange
            var baseObj = new object();

            // Act
            var first = (TestCreateable)CreateViaReflection(typeof(TestCreateable), baseObj);
            var second = (TestCreateable)CreateViaReflection(typeof(TestCreateable), baseObj);

            // Assert
            Assert.Same(first, second);
            Assert.Equal(1, TestCreateable.SyncCreateCount);
        }

        /// <summary>
        /// Creates the should initialize and raise on created.
        /// </summary>
        [Fact]
        public async Task Create_ShouldInitializeAndRaiseOnCreated()
        {
            var baseObj = new object();
            var systemUnderTest = (TestCreateable)CreateViaReflection(typeof(TestCreateable), baseObj);

            var concrete = systemUnderTest;
            var raised = false;
            concrete.OnCreated += (_, _) => raised = true;

            await concrete.Initialization!;

            Assert.True(concrete.IsCreated);
            Assert.True(raised);
        }

        /// <summary>
        /// Creates the should return same instance.
        /// </summary>
        [Fact]
        public async Task Create_ShouldReturnSameInstance()
        {
            var baseObj = new object();

            var first = (TestCreateable)CreateViaReflection(typeof(TestCreateable), baseObj);
            var second = (TestCreateable)CreateViaReflection(typeof(TestCreateable), baseObj);

            await first.Initialization!;

            Assert.Same(first, second);
        }

        /// <summary>
        /// Creates the when called concurrently returns single instance.
        /// </summary>
        [Fact]
        public async Task Create_WhenCalledConcurrently_ReturnsSingleInstance()
        {
            var baseObj = new object();

            var tasks = Enumerable.Range(0, 20)
                .Select(_ => Task.Run(() => CreateViaReflection(typeof(TestCreateable), baseObj)))
                .ToArray();

            var results = await Task.WhenAll(tasks);

            var first = results[0];
            Assert.All(results, r => Assert.Same(first, r));
        }

        /// <summary>
        /// Initializations the should run only once.
        /// </summary>
        [Fact]
        public async Task Initialization_ShouldRunOnlyOnce()
        {
            var baseObj = new object();

            var systemUnderTest = (TestCreateable)CreateViaReflection(typeof(TestCreateable), baseObj);

            var init1 = systemUnderTest.Initialization;
            var init2 = systemUnderTest.Initialization;

            await Task.WhenAll(init1!, init2!);

            Assert.Same(init1, init2);
            Assert.Equal(1, TestCreateable.AsyncCallCount);
        }

        /// <summary>
        /// Initializations the when throws exception is captured.
        /// </summary>
        [Fact]
        public async Task Initialization_WhenThrows_ExceptionIsCaptured()
        {
            var baseObj = new object();

            var systemUnderTest = (FailingCreateable)CreateViaReflection(typeof(FailingCreateable), baseObj);

            await Assert.ThrowsAsync<InvalidOperationException>(() => systemUnderTest.Initialization!);

            Assert.NotNull(systemUnderTest.InitializationException);
        }

        /// <summary>
        /// Called when [created when subscribed after creation is called immediately].
        /// </summary>
        [Fact]
        public async Task OnCreated_WhenSubscribedAfterCreation_IsCalledImmediately()
        {
            var baseObj = new object();
            var systemUnderTest = (TestCreateable)CreateViaReflection(typeof(TestCreateable), baseObj);

            await systemUnderTest.Initialization!;

            var raised = false;

            systemUnderTest.OnCreated += (_, _) => raised = true;

            Assert.True(raised);
        }

        private static object CreateViaReflection(Type concreteType, object baseObj)
        {
            var baseType = concreteType.BaseType!;
            var method = baseType.GetMethod("Create", BindingFlags.Static | BindingFlags.Public) ?? baseType.GetMethod("Create", BindingFlags.Static | BindingFlags.NonPublic);
            return method!.Invoke(null, [baseObj])!;
        }

        private sealed class FailingCreateable : CreateableBindableBase<object, FailingCreateable, object>
        {
            protected override void OnCreate(object @base)
            {
            }

            protected override Task OnCreateAsync(object @base, CancellationToken cancellationToken) => throw new InvalidOperationException("Boom");
        }

        private sealed class TestCreateable : CreateableBindableBase<ITestCreateable, TestCreateable, object>, ITestCreateable
        {
            public static int AsyncCallCount;
            public static int SyncCreateCount;

            protected override void OnCreate(object @base) => Interlocked.Increment(ref SyncCreateCount);

            protected override async Task OnCreateAsync(object @base, CancellationToken cancellationToken)
            {
                Interlocked.Increment(ref AsyncCallCount);
                await Task.Delay(10, cancellationToken);
            }
        }
    }
}