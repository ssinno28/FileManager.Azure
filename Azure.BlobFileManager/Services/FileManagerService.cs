using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Azure.BlobFileManager.Dictionary;
using Azure.BlobFileManager.Dtos;
using Azure.BlobFileManager.Helpers;
using Azure.BlobFileManager.Interfaces;
using Azure.BlobFileManager.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.DataMovement;

namespace Azure.BlobFileManager.Services
{
    public class FileManagerService : IFileManagerService
    {
        private const string ContainerName = "filemanager";
        private readonly MediaConfig _mediaConfig;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FileManagerService(IOptions<MediaConfig> mediaConfig, IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
            _mediaConfig = mediaConfig.Value;
        }

        public async Task<string> AddFolder(string path, string newFolder)
        {
            var container = await GetContainer();
            CloudBlobDirectory directory = container.GetDirectoryReference($"{path}{newFolder}");

            string tempFile = Path.GetTempFileName();

            Random random = new Random();
            var uploadedBytes = new byte[0];
            random.NextBytes(uploadedBytes);

            File.WriteAllBytes(tempFile, uploadedBytes);

            string tempFileName = Path.GetFileName(tempFile);
            await AddFile($"{path}{newFolder}/{tempFileName}", "application/pdf", Path.GetFileNameWithoutExtension(tempFileName),
                uploadedBytes);

            return $"{path}{newFolder}/";
        }

        public async Task<MediaDto> GetFile(string path)
        {
            var blob = await GetBlob(path);
            await blob.FetchAttributesAsync();

            return new MediaDto
            {
                ContentType = blob.Properties.ContentType,
                DateModified = blob.Properties.LastModified,
                DateCreated = blob.GetDateCreated(),
                FileSize = blob.Properties.Length,
                Name = HttpUtility.UrlDecode(blob.Metadata["FileName"]),
                FileType = FileType.File,
                StoragePath = blob.Uri.ToString(),
                Path = blob.Name
            };
        }

        public MediaDto GetRootFolder()
        {
            Claim rootFolderClaim = _httpContextAccessor.HttpContext.User.FindFirst("RootFolder");
            if (rootFolderClaim == null)
            {
                return new MediaDto
                {
                    Name = "Root",
                    StoragePath = "/",
                    FileType = FileType.Folder
                };
            }

            return new MediaDto
            {
                Name = rootFolderClaim.Value,
                FileType = FileType.Folder
            };
        }

        public async Task<List<MediaDto>> DeleteFile(string path)
        {
            var container = await GetContainer();

            List<MediaDto> deletedFiles = new List<MediaDto>();
            if (Path.HasExtension(path))
            {
                CloudBlob blob = await GetBlob(path);

                await blob.FetchAttributesAsync();
                deletedFiles.Add(new MediaDto
                {
                    ContentType = blob.Properties.ContentType,
                    DateModified = blob.Properties.LastModified,
                    DateCreated = blob.GetDateCreated(),
                    FileSize = blob.Properties.Length,
                    Name = HttpUtility.UrlDecode(blob.Metadata["FileName"]),
                    FileType = FileType.File,
                    StoragePath = blob.Uri.ToString(),
                    Path = blob.Name
                });

                await blob.DeleteIfExistsAsync();
            }
            else
            {
                var directory = container.GetDirectoryReference(path);
                List<Task> tasks = new List<Task>();
                foreach (IListBlobItem result in await directory.ListBlobsAsync())
                {
                    if (IsCloudBlob(result))
                    {
                        CloudBlob blob = (CloudBlob)result;

                        await blob.FetchAttributesAsync();
                        deletedFiles.Add(new MediaDto
                        {
                            ContentType = blob.Properties.ContentType,
                            DateModified = blob.Properties.LastModified,
                            DateCreated = blob.GetDateCreated(),
                            FileSize = blob.Properties.Length,
                            Name = HttpUtility.UrlDecode(blob.Metadata["FileName"]),
                            FileType = FileType.File,
                            StoragePath = result.Uri.ToString(),
                            Path = blob.Name
                        });

                        tasks.Add(blob.DeleteIfExistsAsync());
                    }
                }

                Task.WaitAll(tasks.ToArray());
            }

            return deletedFiles;
        }

        public async Task<MediaDto> AddFile(string path, string contentType, string name, byte[] file)
        {
            CloudBlockBlob blob = await GetBlob(path);

            blob.Properties.ContentType = contentType;
            blob.Metadata.Add("DateCreated", DateTime.UtcNow.ToString("O"));
            blob.Metadata.Add("FileName", name);

            await blob.UploadFromByteArrayAsync(file, 0, file.Length);
            await blob.SetMetadataAsync();

            await blob.FetchAttributesAsync();
            return new MediaDto
            {
                ContentType = blob.Properties.ContentType,
                DateModified = blob.Properties.LastModified,
                DateCreated = blob.GetDateCreated(),
                FileSize = blob.Properties.Length,
                Name = HttpUtility.UrlDecode(blob.Metadata["FileName"]),
                FileType = FileType.File,
                StoragePath = blob.Uri.ToString(),
                Path = blob.Name
            };
        }

        public async Task<IEnumerable<MediaDto>> GetFolderFiles(string path)
        {
            var container = await GetContainer();
            var directory = container.GetDirectoryReference(path);

            List<MediaDto> files = new List<MediaDto>();
            foreach (IListBlobItem result in await directory.ListBlobsAsync())
            {
                if (IsCloudBlob(result))
                {
                    CloudBlob blob = (CloudBlob)result;

                    await blob.FetchAttributesAsync();
                    files.Add(new MediaDto
                    {
                        ContentType = blob.Properties.ContentType,
                        DateModified = blob.Properties.LastModified,
                        DateCreated = blob.GetDateCreated(),
                        FileSize = blob.Properties.Length,
                        Name = HttpUtility.UrlDecode(blob.Metadata["FileName"]),
                        FileType = FileType.File,
                        StoragePath = result.Uri.ToString(),
                        Path = blob.Name
                    });
                }
            }

            return files;
        }

        public async Task<MediaDto> GetFolder(string path)
        {
            var container = await GetContainer();
            var directory = container.GetDirectoryReference(path);

            return new MediaDto
            {
                FileType = FileType.Folder,
                StoragePath = directory.Uri.ToString(),
                Path = directory.Prefix,
                Name = GetDirectoryName(path)
            };
        }

        public async Task<IEnumerable<MediaDto>> GetChildFolders(string path)
        {
            var container = await GetContainer();
            var directory = container.GetDirectoryReference(path);

            List<MediaDto> folders = new List<MediaDto>();
            foreach (IListBlobItem result in await directory.ListBlobsAsync())
            {
                if (IsCloudDirectory(result))
                {
                    CloudBlobDirectory dir = (CloudBlobDirectory)result;
                    folders.Add(new MediaDto
                    {
                        FileType = FileType.Folder,
                        StoragePath = dir.Uri.ToString(),
                        Path = dir.Prefix,
                        Name = GetDirectoryName(dir.Uri.ToString())
                    });
                }
            }

            return folders;
        }

        private string GetDirectoryName(string uri)
        {
            if (uri[uri.Length - 1] == '/')
            {
                uri = uri.Substring(0, uri.Length - 1);
            }

            return uri.Split('/').Last();
        }

        public bool IsFolder(MediaDto item)
        {
            return item.FileType == FileType.Folder;
        }

        public bool IsFile(MediaDto item)
        {
            return item.FileType == FileType.File;
        }

        public async Task<MediaDto> RenameFolder(MediaDto folder, string newName)
        {
            var container = await GetContainer();

            string oldPath = folder.Path;
            string newPath = oldPath.Replace(folder.Name, newName);

            var sourceBlobDir = container.GetDirectoryReference(folder.Path);
            var destBlobDir = container.GetDirectoryReference(newPath);

            TransferManager.Configurations.ParallelOperations = 64;
            // Setup the transfer context and track the upoload progress
            var context = new DirectoryTransferContext
            {
                ProgressHandler = new Progress<TransferStatus>((progress) =>
                {
                    Console.WriteLine("Bytes uploaded: {0}", progress.BytesTransferred);
                })
            };

            var copyDirOptions = new CopyDirectoryOptions
            {
                Recursive = true,
                IncludeSnapshots = true
            };

            await TransferManager.CopyDirectoryAsync(sourceBlobDir, destBlobDir, true, copyDirOptions, context);
            await DeleteFile(folder.Path);

            folder.Path = newPath;
            folder.StoragePath = destBlobDir.Uri.ToString();
            folder.DateModified = DateTime.UtcNow;

            return folder;
        }

        public async Task<MediaDto> RenameFile(MediaDto file, string newName)
        {
            string oldPath = file.StoragePath;
            string newPath = oldPath.Replace(file.Name, newName);

            var blob = await UpdateFilePath(oldPath, newPath);

            blob.Metadata.Add("FileName", newName);
            await blob.SetMetadataAsync();

            file.Name = newName;
            file.StoragePath = newPath;

            return file;
        }

        public async Task<MediaDto> ReplaceFile(MediaDto file, Stream postedFile)
        {
            CloudBlockBlob blob = await GetBlob(file.StoragePath);
            if (_mediaConfig.TakeSnapshots)
            {
                await blob.CreateSnapshotAsync();
            }

            await blob.UploadFromStreamAsync(postedFile);

            await blob.FetchAttributesAsync();
            file.FileSize = blob.Properties.Length;
            file.DateModified = DateTime.UtcNow;

            return file;
        }

        public async Task<byte[]> GetFileBytes(string path)
        {
            CloudBlockBlob blob = await GetBlob(path);

            await blob.FetchAttributesAsync();
            long fileByteLength = blob.Properties.Length;

            byte[] myByteArray = new byte[fileByteLength];
            await blob.DownloadToByteArrayAsync(myByteArray, 0);

            return myByteArray;
        }

        public async Task<MediaDto> MoveFolder(MediaDto folder, string path)
        {
            var container = await GetContainer();

            if (path[path.Length - 1] != '/')
            {
                path = path + "/";
            }

            string newPath = $"{path}{folder.Name}";

            var sourceBlobDir = container.GetDirectoryReference(folder.Path);
            var destBlobDir = container.GetDirectoryReference(newPath);

            TransferManager.Configurations.ParallelOperations = 64;
            // Setup the transfer context and track the upoload progress
            DirectoryTransferContext context = new DirectoryTransferContext
            {
                ProgressHandler = new Progress<TransferStatus>((progress) =>
                {
                    Console.WriteLine("Bytes uploaded: {0}", progress.BytesTransferred);
                })
            };

            var copyDirOptions = new CopyDirectoryOptions
            {
                Recursive = true,
                IncludeSnapshots = true
            };

            await TransferManager.CopyDirectoryAsync(sourceBlobDir, destBlobDir, true, copyDirOptions, context);
            await DeleteFile(folder.Path);

            folder.Path = newPath;
            folder.StoragePath = destBlobDir.Uri.ToString();
            folder.DateModified = DateTime.UtcNow;

            return folder;
        }

        public async Task<MediaDto> MoveFile(MediaDto file, string path)
        {
            string newPath = $"{path}{file.Name}";
            file.StoragePath = newPath;
            file.DateModified = DateTime.UtcNow;

            await UpdateFilePath(file.StoragePath, newPath);

            return file;
        }

        public async Task<SummaryInfo> GetSummaryInfo()
        {
            var container = await GetContainer();
            var folders = new List<MediaDto>();
            var files = new List<MediaDto>();

            foreach (var listBlobItem in await container.ListBlobsAsync())
            {
                if (IsCloudDirectory(listBlobItem))
                {
                    CloudBlobDirectory blob = (CloudBlobDirectory)listBlobItem;
                    folders.Add(new MediaDto
                    {
                        FileType = FileType.Folder,
                        StoragePath = blob.Uri.ToString()
                    });
                }

                if (IsCloudBlob(listBlobItem))
                {
                    CloudBlob blob = (CloudBlob)listBlobItem;

                    await blob.FetchAttributesAsync();
                    files.Add(new MediaDto
                    {
                        ContentType = blob.Properties.ContentType,
                        DateModified = blob.Properties.LastModified,
                        DateCreated = blob.GetDateCreated(),
                        FileSize = blob.Properties.Length,
                        Name = HttpUtility.UrlDecode(blob.Metadata["FileName"]),
                        FileType = FileType.File,
                        StoragePath = listBlobItem.Uri.ToString(),
                        Path = blob.Name
                    });
                }
            }

            return new SummaryInfo
            {
                Folders = folders.Count,
                Size = files.Sum(x => x.FileSize),
                Files = files.Count,
                SizeLimit = GetContainerSizeLimit()
            };
        }

        public async Task<bool> FileExists(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return true;
            }

            var container = await GetContainer();
            if (path[0].Equals('/'))
            {
                path = path.Substring(1);
            }

            return await container.GetBlockBlobReference(path).ExistsAsync();
        }

        private async Task<CloudBlockBlob> UpdateFilePath(string oldFilePath, string newFilePath)
        {
            CloudBlockBlob source = await GetBlob(oldFilePath);
            CloudBlockBlob target = await GetBlob(newFilePath);

            await target.StartCopyAsync(source);

            while (target.CopyState.Status == CopyStatus.Pending)
                await Task.Delay(100);

            if (target.CopyState.Status != CopyStatus.Success)
                throw new Exception("Rename failed: " + target.CopyState.Status);

            await source.DeleteAsync();

            return target;
        }

        public async Task<CloudBlobContainer> GetContainer()
        {
            var client = GetClient();
            var container = client.GetContainerReference(GetBlobContainerName());

            await container.CreateIfNotExistsAsync();

            // set access level to "blob", which means user can access the blob 
            // but not look through the whole container
            // this means the user must have a URL to the blob to access it
            BlobContainerPermissions permissions = new BlobContainerPermissions();
            permissions.PublicAccess = BlobContainerPublicAccessType.Blob;
            await container.SetPermissionsAsync(permissions);

            return container;
        }

        private CloudBlobClient GetClient()
        {
            return CloudStorageAccount.Parse(_mediaConfig.StorageConnStr).CreateCloudBlobClient();
        }

        private async Task<CloudBlockBlob> GetBlob(string path)
        {
            if (path[0].Equals('/'))
            {
                path = path.Substring(1);
            }

            var container = await GetContainer();
            return container.GetBlockBlobReference(path);
        }

        private bool IsCloudBlob(IListBlobItem item)
        {
            return item.GetType() == typeof(CloudBlob) ||
                   item.GetType().GetTypeInfo().BaseType == typeof(CloudBlob);
        }

        private bool IsCloudDirectory(IListBlobItem item)
        {
            return item.GetType() == typeof(CloudBlobDirectory) ||
                   item.GetType().GetTypeInfo().BaseType == typeof(CloudBlobDirectory);
        }

        private string GetBlobContainerName()
        {
            Claim containerClaim =
                _httpContextAccessor.HttpContext.User.FindFirst("BlobContainer");

            string name = containerClaim == null ? ContainerName : containerClaim.Value;
            if (name.Length < 3 || name.Length > 63 || !Regex.IsMatch(name, @"^[a-z0-9]+(-[a-z0-9]+)*$"))
            {
                throw new Exception("Container name is not valid!!");
            }

            return name;
        }

        private long GetContainerSizeLimit()
        {
            Claim sizeLimitClaim =
                _httpContextAccessor.HttpContext.User.FindFirst("BlobContainerSizeLimit");

            long maxLimit = (long)2.5e+17;
            long limit = sizeLimitClaim == null ? (long)2.5e+17 : Convert.ToInt64(sizeLimitClaim.Value);

            if (limit > maxLimit)
            {
                throw new Exception("Container size limit exceeds what azure supports (500TB^2)");
            }

            return limit;
        }
    }
}