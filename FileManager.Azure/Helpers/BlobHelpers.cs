using System;
using System.Reflection;
using Microsoft.Azure.Storage.Blob;

namespace FileManager.Azure.Helpers
{
    public static class BlobHelpers
    {
        public static bool IsBlob(this IListBlobItem item)
        {
            return item.GetType() == typeof(CloudBlob) ||
                    item.GetType().GetTypeInfo().BaseType == typeof(CloudBlob);
        }

        public static DateTime GetDateCreated(this CloudBlob blob)
        {
            if (blob.Metadata.TryGetValue("DateCreated", out var dateCreated) && DateTime.TryParse(dateCreated, out var dateCreatedDt))
            {
                return dateCreatedDt;
            }

            return DateTime.MinValue;
        }
    }
}