using System;
using GridAgentSharedLib;

namespace GridSharedLibs
{
    public class GridTask
    {
        /// <summary>
        /// Master or slave task Id
        /// </summary>
        public string Id { get; set; }

        public string Name { get; set; }

        public string DllLocation { get; set; }

        public string AssemblyName { get; set; }

        public TaskType Type { get; set; }

        public string TaskRepositoryName { get; set; }

        public string PlatformTarget { get; set; }

        public ImplementationType ImplementationType { get; set; }

        public string ExecutionPlatform { get; set; }

        public InstanceCreatorType CreatorType { get; set; }

        public GridTaskState State { get; set; }

        public override string ToString()
        {
            return string.Format("{0} : {1} task in {2} repository", Enum.GetName(typeof(TaskType), Type), Name, TaskRepositoryName);
        }
    }
}