#region

using System;
using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class LockExit
    {
        public Guid ClientId { get; set; }
        public string TypeName { get; set; }
        public string LockName { get; set; }
    }

    public class LockUpdateResponse : IHasResponseStatus
    {
        public int Res { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}