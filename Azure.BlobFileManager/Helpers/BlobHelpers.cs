using System;
using System.Reflection;
using Microsoft.WindowsAzure.Storage.Blob;

namespace Azure.BlobFileManager.Helpers
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
            string dateCreated;
            DateTime dateCreatedDt;
            if (blob.Metadata.TryGetValue("DateCreated", out dateCreated) && DateTime.TryParse(dateCreated, out dateCreatedDt))
            {
                return dateCreatedDt;
            }

            return DateTime.MinValue;
        }
    }
}