#region

using System;
using System.Threading;
using System.Windows.Threading;
using GridSharedLibs.ClientServices;
using ServiceStack.Logging;

#endregion

namespace GridAgent.Concurrency
{
    /// <summary>
    ///     An asynchronous locking mechanism.
    ///     Protects objects from being manipulated by more than
    ///     one <see cref="Agent" /> at the same time.
    /// </summary>
    public class GridMonitor : IDisposable
    {
        private const int TestInterval = 5000;
        private static readonly ILog Log = LogManager.GetLogger(typeof (GridMonitor));
        private readonly IGridService _gridService;
        private readonly object _lockUpdateLock = new object();
        /* TODO: make these values dynamic, or at least configurable. */
        private readonly int _timeoutSeconds = 30000;

        private DispatcherTimer _aliveTimer;
        private bool _performingLockUpdate;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GridMonitor" /> class.
        /// </summary>
        /// <param name="sync">The sync.</param>
        public GridMonitor(GridSync sync, IGridService gridService)
            : this(sync, -1, gridService)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GridMonitor" /> class.
        /// </summary>
        /// <param name="sync">The sync.</param>
        /// <param name="timeoutSeconds">The timeout seconds.</param>
        public GridMonitor(GridSync sync, int timeoutSeconds, IGridService gridService)
        {
            _gridService = gridService;

            if (sync == null)
            {
                throw new ArgumentNullException("sync");
            }

            ThreadSynchronizer.Current.InvokeAsynchronously(state => InitAliveTimer(), null);

            if (timeoutSeconds != -1)
            {
                _timeoutSeconds = timeoutSeconds;
            }

            GridSync = sync;

            try
            {
                Enter();
            }
            catch (Exception ex)
            {
                Log.Fatal("Unable to Enter GridMonitor.", ex);
                throw;
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GridMonitor" /> class.
        ///     The typeScope and localName parameters are used
        ///     to construct an identifier for the monitor. The combination
        ///     of the two determines the unique identifier for the monitor.
        /// </summary>
        /// <param name="clientId">The client's id.</param>
        /// <param name="typeScope">
        ///     A type to scope this monitor.
        ///     This is used to build an identifier for the lock.
        /// </param>
        /// <param name="localName">The local name of the lock.</param>
        public GridMonitor(Guid clientId, Type typeScope, string localName, IGridService gridService)
            : this(clientId, typeScope, localName, -1, gridService)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="GridMonitor" /> class.
        ///     The typeScope and localName parameters are used
        ///     to construct an identifier for the monitor. The combination
        ///     of the two determines the unique identifier for the monitor.
        /// </summary>
        /// <param name="clientId">The client's id.</param>
        /// <param name="typeScope">
        ///     A type to scope this monitor.
        ///     This is used to build an identifier for the lock.
        /// </param>
        /// <param name="localName">The local name of the lock.</param>
        /// <param name="timeoutSeconds">The timeout seconds.</param>
        /// <param name="gridService"></param>
        public GridMonitor(Guid clientId, Type typeScope, string localName, int timeoutSeconds, IGridService gridService)
        {
            _gridService = gridService;

            if (typeScope == null)
            {
                throw new ArgumentNullException("typeScope");
            }
            if (localName == null)
            {
                throw new ArgumentNullException("localName");
            }

            if (timeoutSeconds != -1)
            {
                _timeoutSeconds = timeoutSeconds;
            }

            GridSync = new GridSync(clientId, typeScope, localName);
            Enter();
        }

        /// <summary>
        ///     Gets or sets a value indicating whether an attempt
        ///     to gain ownership of the monitor failed due to a timeout.
        /// </summary>
        /// <value>
        ///     <c>true</c> if a timeout occured; otherwise, <c>false</c>.
        /// </value>
        public bool TimedOut { get; protected set; }

        protected GridSync GridSync { get; private set; }

        private void InitAliveTimer()
        {
            /* Notify alive every 10 seconds. */
            _aliveTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(10000)
                };
            _aliveTimer.Start();
        }

        private void Enter()
        {
            int countDown = _timeoutSeconds;
            bool ownsLock = false;
            bool threwException = false;
            IGridService gridService = _gridService;

            bool first = true;
            do
            {
                if (!first)
                {
                    Thread.Sleep(TestInterval);
                }
                first = false;

                try
                {
                    ThreadSynchronizer.Current.RaiseExceptionIfOnSameThread();
                    ownsLock = gridService.LockEnter(
                        GridSync.ClientId, GridSync.ScopeTypeName, GridSync.LocalName);

                    countDown -= TestInterval;
                }
                catch (Exception ex)
                {
                    Log.Warn("Unable to Enter lock on server.", ex);
                    threwException = true;
                }

                countDown -= TestInterval;
            } while (!ownsLock && countDown > 0 && !threwException);

            if (!ownsLock && countDown <= 0 && !threwException)
            {
                TimedOut = true;
                Log.Warn("Enter timed out. ClientId " + GridSync.ClientId);
            }

            if (ownsLock)
            {
                ThreadSynchronizer.Current.InvokeAsynchronously(delegate
                    {
                        _aliveTimer.Tick -= TimerTick;
                        _aliveTimer.Tick += TimerTick;
                    }, null);
            }
        }

        private void TimerTick(object sender, EventArgs e)
        {
            lock (_lockUpdateLock)
            {
                if (_performingLockUpdate)
                {
                    return;
                }
                _performingLockUpdate = true;
            }
            try
            {
                PerformLockUpdate();
            }
            finally
            {
                _performingLockUpdate = false;
            }
        }

        private void PerformLockUpdate()
        {
            IGridService gridService = _gridService;

            try
            {
                ThreadSynchronizer.Current.RaiseExceptionIfOnSameThread();
                gridService.LockUpdate(GridSync.ClientId, GridSync.ScopeTypeName, GridSync.LocalName);
            }
            catch (Exception ex)
            {
                Log.Debug("Exception occured while trying to update lock.", ex);
            }
        }

        private void ReleaseLock()
        {
            if (GridSync == null)
            {
                return;
            }

            IGridService gridService = _gridService;

            try
            {
                Log.Info("Releasing lock");
                ThreadSynchronizer.Current.InvokeSynchronously(delegate { _aliveTimer.Tick -= TimerTick; }, null);
                ThreadSynchronizer.Current.RaiseExceptionIfOnSameThread();
                gridService.LockExit(GridSync.ClientId, GridSync.ScopeTypeName, GridSync.LocalName);
            }
            catch (Exception ex)
            {
                Log.Warn("gridService threw exception while trying to release lock.", ex);
            }
        }

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            Dispose(true);
            /* Tell the garbage collector not to call the finalizer 
			 * since all the cleanup will already be done. */
            GC.SuppressFinalize(true);
        }

        ~GridMonitor()
        {
            Dispose(false);
        }

        protected virtual void Dispose(bool isDisposing)
        {
            if (_disposed)
            {
                return;
            }

            if (isDisposing)
            {
                /* Allow other agents to enter using statement. */
                ReleaseLock();
            }

            /* Free any unmanaged resources 
			 * in this section. */
            _disposed = true;
        }

        #endregion
    }
}