using System;

namespace GridAgentSharedLib
{
    // TODO Move this class in the GridSharedLibs
    [Serializable]
    public class GridTaskType
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public string Id { get; set; }

        public ImplementationType ImplementationType { get; set; }
    }
}