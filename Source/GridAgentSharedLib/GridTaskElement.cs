using System;

namespace GridAgentSharedLib
{
    [Serializable]
    public class GridTaskElement
    {
        public string Name { get; set; }
        public string SlaveTypeAssemblyName { get; set; }

        public double Priority { get; set; }
        public string SlaveTypeName { get; set; }
        public string CustomProviderData { get; set; }

        public string MasterId { get; set; }
    }
}