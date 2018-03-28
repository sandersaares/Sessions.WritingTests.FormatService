using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FormatService
{
    public sealed class AzureFormattedImagePublisher : IFormattedImagePublisher
    {
        public async Task<string> PublishAsync(string filePath, string containerName, string publishFilename, CancellationToken cancel)
        {
            var storageAccount = CloudStorageAccount.Parse(Program.Configuration["AzureStorageConnectionString"]);
            var blobClient = storageAccount.CreateCloudBlobClient();
            var storageContainer = blobClient.GetContainerReference(containerName);

            await storageContainer.CreateIfNotExistsAsync(default, default, default, cancel);

            var blob = storageContainer.GetBlockBlobReference(publishFilename);

            await blob.UploadFromFileAsync(filePath, default, default, default, cancel);

            var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
            {
                Permissions = SharedAccessBlobPermissions.Read,
                SharedAccessExpiryTime = DateTimeOffset.UtcNow.AddDays(365 * 25),
                SharedAccessStartTime = DateTimeOffset.UtcNow.AddDays(-1)
            });

            return blob.Uri + sas;
        }
    }
}
