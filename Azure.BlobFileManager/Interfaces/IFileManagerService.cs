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
        Task<BlobDto> GetFile(string path);
        Task<BlobDto> GetFolder(string path);
        BlobDto GetRootFolder();
        Task<bool> FileExists(string path);
        Task<List<BlobDto>> DeleteFile(string path);
        Task<BlobDto> AddFile(string path, string contentType, string name, byte[] file);
        Task<IEnumerable<BlobDto>> GetFolderFiles(string path);
        Task<IEnumerable<BlobDto>> GetChildFolders(string path);
        bool IsFolder(BlobDto item);
        bool IsFile(BlobDto item);
        Task<BlobDto> RenameFolder(BlobDto folder, string newName);
        Task<BlobDto> RenameFile(BlobDto file, string newName);
        Task<BlobDto> ReplaceFile(BlobDto file, Stream postedFile);
        Task<byte[]> GetFileBytes(string path);
        Task<BlobDto> MoveFolder(BlobDto folder, string path);
        Task<BlobDto> MoveFile(BlobDto file, string path);
        Task<SummaryInfo> GetSummaryInfo();
        Task<CloudBlobContainer> GetContainer();
    }
}