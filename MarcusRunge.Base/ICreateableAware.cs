using System;
using System.Threading.Tasks;

namespace MarcusRunge.Base
{
    /// <summary>
    /// Provides a contract for components that expose their creation state and a creation notification event.
    /// </summary>
    public interface ICreateableAware
    {
        /// <summary>
        /// Raised when the instance is created (transition to the created state).
        /// </summary>
        event EventHandler? OnCreated;

        /// <summary>
        /// Gets a task that represents the asynchronous initialization process of the instance. May be null if initialization has not been started.
        /// </summary>
        Task? Initialization { get; }

        /// <summary>
        /// Gets an exception that occurred during the initialization process, if any.
        /// </summary>
        Exception? InitializationException { get; }

        /// <summary>
        /// Gets a value indicating whether the instance has been created (initialization completed successfully).
        /// </summary>
        bool IsCreated { get; }

        /// <summary>
        /// Gets a value indicating whether initialization has been started and not yet completed.
        /// </summary>
        bool IsInitializing { get; }
    }
}
