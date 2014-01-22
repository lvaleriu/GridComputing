#region

using System;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;
using Newtonsoft.Json;

#endregion

namespace GridComputingSharedLib
{
    /// <summary>
    ///     The coordinator for <see cref="Agent" /> slave tasks.
    ///     Splits an activity into segments that can then be
    ///     deligated to slave tasks.
    /// </summary>
    public abstract class MasterTask : MarshalByRefObject, IMasterTask
    {
        protected bool EnableTrace
        {
            get { return _enableTrace; }
        }

        #region Fields

        private static readonly IGridLog Log = GridLogManager.GetLogger(typeof(MasterTask));
        private readonly Guid _id;
        private volatile bool _completed;
        private string _name;
        private double _priority;
        private string _slaveTypeName;
        private bool _enableTrace;
        private GridTaskElement _taskElement;

        #endregion

        /// <summary>
        ///     Initializes a new instance of the <see cref="MasterTask" /> class.
        /// </summary>
        protected MasterTask()
        {
            _id = Guid.NewGuid();
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MasterTask" /> class.
        /// </summary>
        /// <param name="name">The unique name this task.</param>
        protected MasterTask(string name) : this()
        {
            Name = name;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

        /// <summary>
        ///     Gets or sets the progress goal.
        ///     This is the point at which the task will be
        ///     deemed to be complete. <seealso cref="StepsCompleted" />
        /// </summary>
        /// <value>The progress goal.</value>
        public long StepsGoal { get; protected set; }

        public GridTaskElement TaskElement
        {
            get { return _taskElement; }
        }

        public virtual bool AllJobsDispatched
        {
            get { return false; }
        }

        public virtual int InitialisationStatus { get { return InitializationStatus.Initialized; } }

        /// <summary>
        ///     Gets or sets a value indicating whether this <see cref="MasterTask" /> is complete.
        /// </summary>
        /// <value>
        ///     <c>true</c> if complete; otherwise, <c>false</c>.
        /// </value>
        public bool Completed
        {
            get { return _completed; }
        }

        /// <summary>
        ///     Gets or sets the unique name of the task.
        /// </summary>
        /// <value>The name of this task.</value>
        public string Name
        {
            get { return _name; }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    throw new ArgumentNullException();
                }
                _name = value;
            }
        }

        public string ExecutionDirectoryPath { get; set; }

        public virtual string Result { get; private set; }

        public Guid Id
        {
            get { return _id; }
        }

        /// <summary>
        ///     Gets the <code>Type</code> name
        ///     of the <see cref="Agent" /> task.
        ///     Must be fully qualified, including publickeytoken,
        ///     version etc. If it is not, client side location
        ///     of the <code>Type</code> will fail.
        /// </summary>
        /// <value>
        ///     The <code>Type</code> name
        ///     of the <see cref="Agent" /> task.
        ///     Must be fully qualified, including publickeytoken,
        ///     version etc.
        /// </value>
        public string SlaveTypeName
        {
            get { return _slaveTypeName; }
        }

        #region Steps Completed

        private long _stepsCompleted;

        /// <summary>
        ///     Gets or sets the progress of the task.
        ///     This value should always be less than
        ///     the <see cref="StepsGoal" />.
        /// </summary>
        /// <value>The progress.</value>
        public long StepsCompleted
        {
            get { return _stepsCompleted; }
            protected set
            {
                _stepsCompleted = value;
            }
        }

        #endregion

        #region event JoinTask

        private event Action<object, string> InternalComplete;

        /// <summary>
        ///     Occurs when the task has finished
        ///     its work.
        /// </summary>
        public event Action<object, string> Complete
        {
            add { InternalComplete += value; }
            remove { InternalComplete -= value; }
        }

        private void FireTriggerRemoving(TaskException ex)
        {
            if (InternalComplete != null)
            {
                InternalComplete(this, ex == null ? null : ex.JsonException);
            }
        }

        #endregion

        protected void TriggerCompletion(bool completed, TaskException exception = null)
        {
            if (EnableTrace)
                Log.Info("TriggerCompletion");

            if (_completed != completed)
            {
                if (_completed)
                    throw new TaskException("Task is already completed!");

                _completed = completed;
                if (completed)
                {
                    FireTriggerRemoving(exception);
                }
            }
        }

        public void LoadInternal(GridTaskElement taskElement)
        {
            if (taskElement == null)
            {
                throw new ArgumentNullException("taskElement");
            }

            _taskElement = taskElement;
            _name = taskElement.Name;
            _slaveTypeName = taskElement.SlaveTypeName;
            _priority = taskElement.Priority;

            FireOnLoad(EventArgs.Empty);
        }

        public void SetTraceValue(bool enable)
        {
            _enableTrace = enable;
        }

        public virtual void LostAgent(IAgent agent)
        {
        }

        protected void Start()
        {
            try
            {
                OnStarting();
                FireOnStarting(EventArgs.Empty);
            }
            catch (Exception exception)
            {
                if (EnableTrace)
                    Log.Error("Start", exception);

                Stop();
                TriggerCompletion(true, new TaskException("Start failed", exception));
            }
        }

        protected void Stop()
        {
            try
            {
                OnStopping();
                FireOnStopping(EventArgs.Empty);
            }
            catch (Exception exception)
            {
                if (EnableTrace)
                    Log.Error("Stop", exception);
            }
        }

        /// <summary>
        ///     Gets the run data for the <see cref="Agent" /> slave task.
        ///     This data should encapsulate the task segment
        ///     that will be worked on by the slave. <seealso cref="Job" />
        /// </summary>
        /// <param name="agent">The agent requesting the run data.</param>
        /// <returns>The job for the agent to work on.</returns>
        public abstract Job GetJob(IAgent agent);

        /// <summary>
        ///     Joins the specified task result. This is called
        ///     when a slave task completes its <see cref="Job" />,
        ///     after having called <see cref="GetJob" />;
        ///     returning the results to be integrated
        ///     by the associated <see cref="MasterTask" />.
        /// </summary>
        /// <param name="agent">The agent joining the results.</param>
        /// <param name="taskResult">The task result.</param>
        public abstract void Join(IAgent agent, TaskResult taskResult);

        /// <summary>
        ///     Cancels the distributed job
        /// </summary>
        /// <param name="agent"></param>
        /// <param name="taskId"></param>
        public virtual void Cancel(IAgent agent, long taskId)
        {
        }

        /// <summary>
        ///     Occurs when the task is starting.
        ///     Allows an opportunity to perform any preprocessing
        ///     activities, such as loading auxiliary data.
        /// </summary>
        protected abstract void OnStarting();
        protected abstract void OnStopping();

        #region event Load

        private event EventHandler InternalLoad;

        /// <summary>
        ///     Occurs when the task has loaded internally.
        /// </summary>
        public event EventHandler Load
        {
            add { InternalLoad += value; }
            remove { InternalLoad -= value; }
        }

        private void FireOnLoad(EventArgs e)
        {
            if (InternalLoad != null)
            {
                InternalLoad(this, e);
            }
        }

        #endregion

        #region event Starting

        private event EventHandler<EventArgs> InternalStarting;

        /// <summary>
        ///     Occurs when the task is starting.
        ///     Allows an opportunity to perform any preprocessing
        ///     activities, such as loading auxiliary data.
        /// </summary>
        public event EventHandler<EventArgs> Starting
        {
            add { InternalStarting += value; }
            remove { InternalStarting -= value; }
        }

        private void FireOnStarting(EventArgs e)
        {
            if (InternalStarting != null)
            {
                InternalStarting(this, e);
            }
        }

        #endregion

        protected void FireSaveTaskResult(string jsonResult)
        {
            Result = jsonResult;
        }

        #region event Stopping

        private event EventHandler<EventArgs> InternalStopping;

        public event EventHandler<EventArgs> Stopping
        {
            add { InternalStopping += value; }
            remove { InternalStopping -= value; }
        }

        private void FireOnStopping(EventArgs e)
        {
            if (InternalStopping != null)
            {
                InternalStopping(this, e);
            }
        }

        #endregion
    }
}