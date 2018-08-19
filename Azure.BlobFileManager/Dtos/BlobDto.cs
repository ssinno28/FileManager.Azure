using System;
using Azure.BlobFileManager.Dictionary;

namespace Azure.BlobFileManager.Dtos
{
    public class BlobDto
    {
        public string Name { get; set; }
        public string ContentType { get; set; }
        public string StoragePath { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTimeOffset? DateModified { get; set; }
        public long FileSize { get; set; }
        public BlobType BlobType { get; set; }
        public string Path { get; set; }
    }
}