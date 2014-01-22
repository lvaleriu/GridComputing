#region

using System;
using GridComputingSharedLib;

#endregion

namespace GridComputing
{
    public class MasterTaskEventHandler : IDisposable
    {
        #region Fields

        private readonly TaskInfo _info;
        private readonly IMasterTask _masterTask;

        #endregion

        public MasterTaskEventHandler(string masterId, IMasterTask masterTask)
        {
            _masterTask = masterTask;
            _info = new TaskInfo(masterId, masterTask.Id, masterTask.TaskElement);

            masterTask.Complete += MasterTaskOnComplete;
        }

        #region Private methods

        private void MasterTaskOnComplete(object o, string jsonException)
        {
            var wrapperMasterClass = ((IWrapperMasterClass) _masterTask);

            // Dont call the Result if there is an error since it generates a communication with the other process (which could be the source of error)
            if (OnComplete != null)
                OnComplete(_info, jsonException, string.IsNullOrWhiteSpace(jsonException) ? wrapperMasterClass.Result : null);

            wrapperMasterClass.RemoveHandlers();
            try
            {
                _masterTask.Complete -= MasterTaskOnComplete;
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
        }

        #endregion

        public void Dispose()
        {
        }

        public event Action<TaskInfo, string, string> OnComplete;
    }
}