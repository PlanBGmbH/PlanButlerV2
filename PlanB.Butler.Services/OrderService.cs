using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace PlanB.Butler.Services
{
    /// <summary>
    /// OrderService.
    /// </summary>
    public static class OrderService
    {
        /// <summary>
        /// Gets the daily order overview.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Daily Overview.
        /// </returns>
        [FunctionName(nameof(GetDailyOrderOverview))]
        public static async Task<string> GetDailyOrderOverview(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)]
            HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("secret.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var connectionString = config["StorageSend"];

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("orders");
            BlobContinuationToken token = new BlobContinuationToken();
            var operationContext = new OperationContext();
            var options = new BlobRequestOptions();
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference("orders");
            BlobContinuationToken blobContinuationToken = null;
            List<OrderBlob> orderBlob = new List<OrderBlob>();
            List<string> blobitems = new List<string>();
            var blobs = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);

            foreach (var item in blobs.Results)
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;
                await blob.FetchAttributesAsync();
                DateTime date = DateTime.Now;
                var stringDate = date.ToString("yyyy-MM-dd");
                if (blob.Metadata.Contains(new KeyValuePair<string, string>("date", stringDate)))
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

        /// <summary>
        /// Gets the daily order overview for user.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>Daily Overview.</returns>
        [FunctionName(nameof(GetDailyOrderOverviewForUser))]
        public static async Task<string> GetDailyOrderOverviewForUser(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        ILogger log,
        ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("secret.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            log.LogInformation("C# HTTP trigger function processed a request.");
            req.Headers.TryGetValue("user", out Microsoft.Extensions.Primitives.StringValues user);

            var connectionString = config["StorageSend"];

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("orders");
            BlobContinuationToken token = new BlobContinuationToken();
            var operationContext = new OperationContext();
            var options = new BlobRequestOptions();
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference("orders");

            BlobContinuationToken blobContinuationToken = null;
            List<OrderBlob> orderBlob = new List<OrderBlob>();
            List<string> blobitems = new List<string>();
            var blobs = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);

            foreach (var item in blobs.Results)
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;

                await blob.FetchAttributesAsync();
                DateTime date = DateTime.Now;
                var stringDate = date.ToString("yyyy-MM-dd");
                string username = user.ToString();
                if (blob.Metadata.Contains(new KeyValuePair<string, string>("date", stringDate)) && blob.Metadata.Contains(new KeyValuePair<string, string>("user", username)))
                {
                    await blob.FetchAttributesAsync();
                    var blobDownload = blob.DownloadTextAsync();
                    var blobData = blobDownload.Result;
                    orderBlob.Add(JsonConvert.DeserializeObject<OrderBlob>(blobData));
                }
            }

            return JsonConvert.SerializeObject(orderBlob);
        }

        /// <summary>
        /// Posts the document order.
        /// </summary>
        /// <param name="messageHeader">The message header.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        [Singleton]
        [FunctionName(nameof(PostDocumentOrder))]
        public static async void PostDocumentOrder(
           [ServiceBusTrigger("q.planbutlerupdateorder", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
           [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
           ILogger log,
           ExecutionContext context)
        {
            string payload = Encoding.Default.GetString(messageHeader.Body);
            OrderBlob orderBlob = new OrderBlob();
            orderBlob.OrderList = new List<Order>();
            orderBlob = JsonConvert.DeserializeObject<OrderBlob>(payload);
            string name = string.Empty;
            DateTime date = DateTime.Now;
            foreach (var item in orderBlob.OrderList)
            {
                name = item.Name;
                date = item.Date;
                break;
            }

            var stringDate = date.ToString("yyyy-MM-dd");

            blob.Metadata.Add("user", name);
            blob.Metadata.Add("date", stringDate);
            await blob.UploadTextAsync(payload);
            await blob.SetMetadataAsync();
        }


    }
}
