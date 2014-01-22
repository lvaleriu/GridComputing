#region

using System;

#endregion

namespace GridAgentSharedLib
{
    public sealed class WrapperSlaveClass : MarshalByRefObject, IWrapperSlaveClass
    {
        private readonly ISlaveTask _slaveTask;

        public WrapperSlaveClass(ISlaveTask slaveTask)
        {
            _slaveTask = slaveTask;
        }

        public ISlaveTask SlaveTask
        {
            get { return _slaveTask; }
        }

        public TaskDescriptor Descriptor { get; private set; }

        public void Initialise(TaskDescriptor taskDescriptor)
        {
            Descriptor = taskDescriptor;
            SlaveTask.Initialise(taskDescriptor);
        }

        public long StepsCompleted
        {
            get { return SlaveTask.StepsCompleted; }
        }

        public long StepsGoal
        {
            get { return SlaveTask.StepsGoal; }
        }

        public string Result
        {
            get { return SlaveTask.Result; }
        }

        public string ExecutionDirectoryPath
        {
            get { return SlaveTask.ExecutionDirectoryPath; }
            set { SlaveTask.ExecutionDirectoryPath = value; }
        }

        public void RunInternal()
        {
            SlaveTask.RunInternal();
        }

        /// <summary>
        ///     Obtains a lifetime service object to control the lifetime policy for this
        ///     instance.
        /// </summary>
        /// <returns>
        ///     An object of type System.Runtime.Remoting.Lifetime.ILease used to control
        ///     the lifetime policy for this instance. This is the current lifetime service
        ///     object for this instance if one exists; otherwise, a new lifetime service
        ///     object initialized to the value of the System.Runtime.Remoting.Lifetime.LifetimeServices.LeaseManagerPollTime
        ///     property.
        ///     null value means this object has to live forever.
        /// </returns>
        public override object InitializeLifetimeService()
        {
            // this object has to live "forever"
            return null;
        }
    }
}