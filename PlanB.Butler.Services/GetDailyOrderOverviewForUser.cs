using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using BotLibraryV2;

namespace Post_Document
{
    public static class GetDailyOrderOverviewForUser
    {
        [FunctionName("GetDailyOrderOverviewForUser")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            Microsoft.Extensions.Primitives.StringValues user;
            req.Headers.TryGetValue("user",out user);
            var connectionString = string.Empty;// TODO: ButlerBot.Util.Settings.StorageAccountConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("orders");
            BlobContinuationToken token = new BlobContinuationToken();
            var context = new OperationContext();
            var options = new BlobRequestOptions();
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference("orders");
            BlobContinuationToken blobContinuationToken = null;
            List<OrderBlob> orderBlob = new List<OrderBlob>();
            List<string> blobitems = new List<string>();
            var blobs = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, context).ConfigureAwait(false);

            foreach (var item in blobs.Results)
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;

                await blob.FetchAttributesAsync();
                DateTime date = DateTime.Now;
                var stringDate = date.ToString("yyyy-MM-dd");
                string username = user.ToString();
                if (blob.Metadata.Contains(new KeyValuePair<string, string>("date", stringDate)) && blob.Metadata.Contains(new KeyValuePair<string, string>("user", username)))
                {
                    Order order = new Order();
                    await blob.FetchAttributesAsync();
                    var blobDownload = blob.DownloadTextAsync();
                    var blobData = blobDownload.Result;
                    orderBlob.Add(JsonConvert.DeserializeObject<OrderBlob>(blobData));
                }
            }

            return JsonConvert.SerializeObject(orderBlob);
        }
    }
}
