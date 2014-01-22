#region

using System.IO;
using System.Net;
using GridComputingServices.Services.Support;
using GridSharedLibs;
using GridSharedLibs.ServiceModel.Operations;
using GridSharedLibs.ServiceModel.Types;
using ServiceStack.Common.Web;
using ServiceStack.Logging;
using ServiceStack.ServiceInterface;
using File = GridSharedLibs.ServiceModel.Types.File;

#endregion

namespace GridComputingServices.Services
{
    public class FilesService : Service
    {
        protected static ILog Log = LogManager.GetLogger(typeof (FilesService));
        public Config Config { get; set; }

        public object Any(Files request)
        {
            var targetFile = GetAndValidateExistingPath(request);

            var isDirectory = Directory.Exists(targetFile.FullName);

            if (!isDirectory && request.ForDownload)
                return new HttpResult(targetFile, asAttachment: true);

            var response = isDirectory
                ? new FilesResponse {Directory = GetFolderResult(targetFile.FullName)}
                : new FilesResponse {File = GetFileResult(targetFile)};

            return response;
        }

        #region Private methods

        private FileInfo GetAndValidateExistingPath(Files request)
        {
            var targetFile = GetPath(request);
            if (!targetFile.Exists && !Directory.Exists(targetFile.FullName))
            {
                Log.Error("Could not find: " + request.Path);
                throw new HttpError(HttpStatusCode.NotFound, new FileNotFoundException("Could not find: " + request.Path));
            }

            return targetFile;
        }

        private FileInfo GetPath(Files request)
        {
            return new FileInfo(Path.Combine(Config.TasksRepository, request.Path.GetSafePath()));
        }

        private FileResult GetFileResult(FileInfo fileInfo)
        {
            return new FileResult
            {
                Name = fileInfo.Name,
                Extension = fileInfo.Extension,
                FileSizeBytes = fileInfo.Length,
                ModifiedDate = fileInfo.LastWriteTimeUtc,
                Checksum = Utils.GetMd5HashFromFile(fileInfo.FullName),
            };
        }

        private FolderResult GetFolderResult(string targetPath)
        {
            var result = new FolderResult();

            foreach (var dirPath in Directory.GetDirectories(targetPath))
            {
                var dirInfo = new DirectoryInfo(dirPath);

                result.Folders.Add(new Folder
                {
                    Name = dirInfo.Name,
                    ModifiedDate = dirInfo.LastWriteTimeUtc,
                    FileCount = dirInfo.GetFiles().Length
                });
            }

            foreach (var filePath in Directory.GetFiles(targetPath))
            {
                var fileInfo = new FileInfo(filePath);

                result.Files.Add(new File
                {
                    Name = fileInfo.Name,
                    Extension = fileInfo.Extension,
                    FileSizeBytes = fileInfo.Length,
                    ModifiedDate = fileInfo.LastWriteTimeUtc,
                    Checksum = Utils.GetMd5HashFromFile(filePath),
                });
            }

            return result;
        }

        #endregion
    }
}