namespace MarcusRunge.Base
{
    /// <summary>
    /// Exposes the asynchronous initialization state of a createable component.
    /// </summary>
    public interface ICreateableAware
    {
        /// <summary>
        /// Occurs after asynchronous initialization completed successfully.
        /// </summary>
        /// <remarks>
        /// A subscriber registered after successful initialization is invoked immediately.
        /// Subscriber failures do not fail initialization and do not prevent other subscribers
        /// from being invoked.
        /// </remarks>
        event EventHandler? OnCreated;

        /// <summary>
        /// Gets the task representing asynchronous initialization, or <c>null</c> if initialization
        /// has not been started.
        /// </summary>
        Task? Initialization { get; }

        /// <summary>
        /// Gets the exception captured during synchronous construction or asynchronous initialization.
        /// </summary>
        Exception? InitializationException { get; }

        /// <summary>
        /// Gets a value indicating whether asynchronous initialization completed successfully.
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// Gets a value indicating whether asynchronous initialization is currently running.
        /// </summary>
        bool IsInitializing { get; }
    }
}