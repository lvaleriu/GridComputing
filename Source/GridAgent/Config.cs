#region

using System;
using System.IO;
using System.Linq;
using GridSharedLibs;
using ServiceStack.Configuration;

#endregion

namespace GridAgent
{
    public class Config
    {
        public Config(AppSettings resourceManager)
        {
            RepositoryTasksFolder = "Repository";

            if (!Directory.Exists(RepositoryTasksFolder))
                Directory.CreateDirectory(RepositoryTasksFolder);

            SlaveTasksFolder = resourceManager.GetString("SlaveTasksFolder");

            if (string.IsNullOrWhiteSpace(SlaveTasksFolder))
            {
                if (!Directory.Exists("Tasks"))
                    Directory.CreateDirectory("Tasks");
                else
                {
                    Directory.EnumerateDirectories("Tasks").ToList().ForEach(e =>
                        {
                            try
                            {
                                Directory.Delete(e, true);
                            }
                            catch
                            {
                            }
                        }
                        );
                }

                SlaveTasksFolder = Path.Combine("Tasks", Guid.NewGuid().ToString());
                LibTools.CopyAll(new DirectoryInfo(RepositoryTasksFolder), new DirectoryInfo(SlaveTasksFolder));
            }

            UrlBase = resourceManager.GetString("UrlBase");
        }

        public string RepositoryTasksFolder { get; private set; }
        public string SlaveTasksFolder { get; private set; }
        public string UrlBase { get; private set; }
    }
}