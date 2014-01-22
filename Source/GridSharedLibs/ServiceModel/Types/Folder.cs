using System;

namespace GridSharedLibs.ServiceModel.Types
{
    public class Folder
    {
        public string Name { get; set; }

        public DateTime ModifiedDate { get; set; }

        public int FileCount { get; set; }
    }
}