using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace GridAgent.Concurrency
{
    /// <summary>
    /// Emulates System.Threading.ThreadSynchronizer
    /// for Silverlight. 
    /// </summary>
    internal class ThreadSynchronizer : DependencyObject, IThreadSynchronizer
    {
        #region Singleton implementation

        internal static readonly ThreadSynchronizer Instance = new ThreadSynchronizer();

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>The current.</value>
        public static IThreadSynchronizer Current
        {
            get { return Instance; }
        }

        #endregion

        #region IThreadSynchronizer Members

        /// <summary>
        /// Queues the specified <see cref="SendOrPostCallback"/>
        /// to be called from the <see cref="System.Threading.Thread"/>
        /// that this context was created on.
        /// </summary>
        /// <param name="callback">The callback delegate to be called
        /// on the thread that this context was instantiated.</param>
        /// <param name="state">The state that is passed to the 
        /// specified callback.</param>
        public virtual void InvokeAsynchronously(SendOrPostCallback callback, object state)
        {
            ParameterValidator.EnsureNotNull(callback, "callback");

            if (CheckAccess())
            {
                callback.Invoke(state);
            }
            else
            {
                Dispatcher.BeginInvoke(callback, state);
            }
        }

        public virtual void InvokeSynchronously(SendOrPostCallback callback, object state)
        {
            ParameterValidator.EnsureNotNull(callback, "callback");

            if (CheckAccess())
            {
                callback.Invoke(state);
            }
            else
            {
                var context = new DispatcherSynchronizationContext(Dispatcher);
                context.Send(callback, state);
            }
        }

        public void RaiseExceptionIfOnSameThread()
        {
            if (Dispatcher.CheckAccess())
            {
                throw new TaskException("Call cannot be made from the current thread. Invoke thread asynchronously.");
            }
        }

        public bool InvokeRequired
        {
            get { return CheckAccess(); }
        }

        #endregion

        /// <summary>
        /// Triggers the Initialisation of the context.
        /// Method signature set to match System.Threading.ThreadSynchronizer.SetContext
        /// Provided here for convenience to avoid code changes 
        /// when using this module in a non-Silverlight scenario.
        /// </summary>
        /// <param name="dummy">Dummy parameter 
        /// to mimic System.Threading.ThreadSynchronizer.SetContext</param>
        public static void SetContext(object dummy)
        {
            /* Intentionally left blank. */
        }
    }
}