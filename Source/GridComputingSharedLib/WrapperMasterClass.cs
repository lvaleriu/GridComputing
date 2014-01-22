#region

using System;
using GridAgentSharedLib;
using GridAgentSharedLib.Clients;

#endregion

namespace GridComputingSharedLib
{
    public class WrapperMasterClass : MarshalByRefObject, IWrapperMasterClass
    {
        private readonly IMasterTask _masterTask;
        private readonly Guid _masterTaskId;
        private GridTaskElement _gridTaskElement;

        public WrapperMasterClass(IMasterTask masterTask)
        {
            _masterTask = masterTask;
            _masterTaskId = masterTask.Id;
            _gridTaskElement = masterTask.TaskElement;

            _masterTask.Complete += MasterTaskOnComplete;
        }

        public long StepsGoal
        {
            get { return _masterTask.StepsGoal; }
        }

        public GridTaskElement TaskElement
        {
            get { return _gridTaskElement; }
        }

        public bool AllJobsDispatched
        {
            get { return _masterTask.AllJobsDispatched; }
        }

        public int InitialisationStatus { get { return _masterTask.InitialisationStatus; } }

        public bool Completed
        {
            get { return _masterTask.Completed; }
        }

        public string Name
        {
            get { return _masterTask.Name; }
            set { _masterTask.Name = value; }
        }

        public string ExecutionDirectoryPath
        {
            get { return _masterTask.ExecutionDirectoryPath; }
            set { _masterTask.ExecutionDirectoryPath = value; }
        }

        public string Result { get { return _masterTask.Result; } }

        public Guid Id
        {
            get { return _masterTaskId; }
        }

        public string SlaveTypeName
        {
            get { return _masterTask.SlaveTypeName; }
        }

        public long StepsCompleted
        {
            get { return _masterTask.StepsCompleted; }
        }

        public event Action<object, string> Complete;

        public Job GetJob(IAgent agent)
        {
            return _masterTask.GetJob(agent);
        }

        public void Join(IAgent agent, TaskResult taskResult)
        {
            _masterTask.Join(agent, taskResult);
        }

        public void Cancel(IAgent agent, long taskId)
        {
            _masterTask.Cancel(agent, taskId);
        }

        public void LostAgent(IAgent agent)
        {
            _masterTask.LostAgent(agent);
        }

        public void LoadInternal(GridTaskElement taskElement)
        {
            _gridTaskElement = taskElement;
            _masterTask.LoadInternal(taskElement);
        }

        public void SetTraceValue(bool enable)
        {
            _masterTask.SetTraceValue(enable);
        }

        public void RemoveHandlers()
        {
            try
            {
                _masterTask.Complete -= MasterTaskOnComplete;

                var masterTask = _masterTask as IWrapperMasterClass;
                if (masterTask != null)
                {
                    masterTask.RemoveHandlers();
                }
            }
            catch (Exception)
            {
            }
        }

        public void FireFailedCompletion(string ex)
        {
            try
            {
                if (Complete != null)
                    Complete(this, ex);
            }
            catch
            {
            }
        }

// ReSharper disable MemberCanBePrivate.Global
        public void MasterTaskOnComplete(object o, string exception)
// ReSharper restore MemberCanBePrivate.Global
        {
            if (Complete != null)
                Complete(this, exception);
        }
    }
}