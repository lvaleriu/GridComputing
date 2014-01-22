using System;

namespace GridSharedLibs
{
    public class TaskLauncherWrapper : MarshalByRefObject
    {
        private readonly ISharedTaskInfo _sharedTaskInfo;

        public TaskLauncherWrapper(ISharedTaskInfo sharedTaskInfo)
        {
            _sharedTaskInfo = sharedTaskInfo;

            //_sharedTaskInfo.CallbackTest += SharedTaskInfoOnCallbackTest;
        }

        public override object InitializeLifetimeService()
        {
            return null;
        }

// ReSharper disable MemberCanBePrivate.Global
        public void SharedTaskInfoOnCallbackTest(string s)
// ReSharper restore MemberCanBePrivate.Global
        {
            if (CallbackTest != null)
                CallbackTest(s);
        }

        public event Action<string> CallbackTest;
    }
}