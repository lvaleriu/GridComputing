#region

using System;
using ServiceStack.ServiceInterface.ServiceModel;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    public class RegisterResponse : IHasResponseStatus
    {
        public Guid Res { get; set; }
        public ResponseStatus ResponseStatus { get; set; }
    }
}