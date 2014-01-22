using System;

namespace GridSharedLibs.ServiceModel.Types
{
    public class File
    {
        public string Name { get; set; }

        public string Extension { get; set; }

        public long FileSizeBytes { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string Checksum { get; set; }
    }
}