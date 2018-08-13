using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.BlobFileManager.Dtos;
using Azure.BlobFileManager.Models;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Azure.BlobFileManager.Interfaces
{
    public interface IFileManagerService
    {
        Task<string> AddFolder(string path, string newFolder);
        Task<MediaDto> GetFile(string path);
        Task<MediaDto> GetFolder(string path);
        MediaDto GetRootFolder();
        Task<bool> FileExists(string path);
        Task<List<MediaDto>> DeleteFile(string path);
        Task<MediaDto> AddFile(string path, string contentType, string name, byte[] file);
        Task<IEnumerable<MediaDto>> GetFolderFiles(string path);
        Task<IEnumerable<MediaDto>> GetChildFolders(string path);
        bool IsFolder(MediaDto item);
        bool IsFile(MediaDto item);
        Task<MediaDto> RenameFolder(MediaDto folder, string newName);
        Task<MediaDto> RenameFile(MediaDto file, string newName);
        Task<MediaDto> ReplaceFile(MediaDto file, Stream postedFile);
        Task<byte[]> GetFileBytes(string path);
        Task<MediaDto> MoveFolder(MediaDto folder, string path);
        Task<MediaDto> MoveFile(MediaDto file, string path);
        Task<SummaryInfo> GetSummaryInfo();
        Task<CloudBlobContainer> GetContainer();
    }
}