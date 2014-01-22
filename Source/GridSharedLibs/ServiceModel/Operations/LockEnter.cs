#region

using System;
using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class LockEnter
    {
        public Guid ClientId { get; set; }
        public string TypeName { get; set; }
        public string LockName { get; set; }
    }

    public class LockEnterResponse : IHasResponseStatus
    {
        public bool Res { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}