using System.Threading;

namespace GridAgent.Concurrency
{
	interface IThreadSynchronizer
	{
		void InvokeAsynchronously(SendOrPostCallback callback, object state);
		void InvokeSynchronously(SendOrPostCallback callback, object state);
		/// <summary>
		/// 
		/// </summary>
		/// <exception cref="TaskException">Occurs if call made 
		/// from the same thread that the ThreadSynchronizer 
		/// was created.</exception>
		void RaiseExceptionIfOnSameThread();

		bool InvokeRequired { get; }
	}
}
