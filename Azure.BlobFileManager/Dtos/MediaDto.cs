using System;
using Azure.BlobFileManager.Dictionary;

namespace Azure.BlobFileManager.Dtos
{
    public class MediaDto
    {
        public string Name { get; set; }
        public string ContentType { get; set; }
        public string StoragePath { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTimeOffset? DateModified { get; set; }
        public long FileSize { get; set; }
        public FileType FileType { get; set; }
        public string Path { get; set; }
    }
}