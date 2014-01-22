#region

using System.Collections.Generic;
using GridAgentSharedLib.Clients;
using ServiceStack.ServiceHost;

#endregion

namespace GridSharedLibs.ServiceModel.Operations
{
    [Route("/GetGridSummary")]
    public class GetGridSummary : IReturn<object>
    {
        public Client Client { get; set; }
    }

    [Route("/GetGridTaks")]
    public class GetGridTaks : IReturn<GetGridTasksResponse>
    {
        public string TaskName { get; set; }
    }

    public class GetGridTasksResponse
    {
        public GetGridTasksResponse()
        {
            Tasks = new List<GridTask>();
        }

        public List<GridTask> Tasks { get; set; }
    }
}