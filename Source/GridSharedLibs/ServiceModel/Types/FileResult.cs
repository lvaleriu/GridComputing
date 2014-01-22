using System;

namespace GridSharedLibs.ServiceModel.Types
{
    public class FileResult
    {
        public string Name { get; set; }

        public string Extension { get; set; }

        public long FileSizeBytes { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string Checksum { get; set; }

        public override string ToString()
        {
            return string.Format("Name : {0}", Name);
        }
    }
}