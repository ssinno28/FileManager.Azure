using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Blob;

namespace FileManager.Azure.Helpers
{
    public static class BlobDirectoryHelpers
    {
        public static async Task<List<IListBlobItem>> ListBlobsAsync(this CloudBlobDirectory directory)
        {
            BlobContinuationToken continuationToken = null;
            List<IListBlobItem> results = new List<IListBlobItem>();
            do
            {
                var response = await directory.ListBlobsSegmentedAsync(continuationToken);
                continuationToken = response.ContinuationToken;
                results.AddRange(response.Results);
            }
            while (continuationToken != null);

            return results;
        }

        public static bool IsDirectory(this IListBlobItem item)
        {
            return item.GetType() == typeof(CloudBlobDirectory) ||
                   item.GetType().GetTypeInfo().BaseType == typeof(CloudBlobDirectory);
        }
    }
}