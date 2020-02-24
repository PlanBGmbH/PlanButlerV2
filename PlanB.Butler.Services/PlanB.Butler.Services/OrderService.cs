// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlanB.Butler.Services.Extensions;

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
           [ServiceBusTrigger("q.planbutlerupdateorder", Connection = "butlerSend")]Message messageHeader,
           [Blob("orders/{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
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

        /// <summary>
        /// Reads the meals.
        /// </summary>
        /// <param name="input">The Input from the Trigger.</param>
        /// <param name="log">The log.</param>
        /// <returns>
        /// All meals.
        /// </returns>
        [Singleton]
        [FunctionName("CreateOrder")]
        [return: ServiceBus("q.planbutlerupdateorder", Connection = "ServiceBusConnection")]
        public static Message PostOrderToQueue([HttpTrigger(AuthorizationLevel.Function, "POST", Route = "orders")] HttpRequest input, ILogger log)
        {
            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }
            
            HttpRequest tmp = input;
            Message msg = new Message();
            Guid correlationId = Util.ReadCorrelationId(input.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            try
            {
                byte[] result;
                using (var streamReader = new MemoryStream())
                {
                    input.Body.CopyTo(streamReader);
                    result = streamReader.ToArray();
                }

                msg.Body = result;

                string json = System.Text.Encoding.Default.GetString(result);
                OrderBlob orderBlob = JsonConvert.DeserializeObject<OrderBlob>(json);
                string name = string.Empty;
                DateTime date = DateTime.Now;
                foreach (var item in orderBlob.OrderList)
                {
                    name = item.Name;
                    date = item.Date;
                    break;
                }

                var stringDate = date.ToString("yyyy-MM-dd");
                msg.Label = $"{name}_{stringDate}";

                
            }
            catch (Exception e)
            {
                trace.Add(string.Format("{0} - {1}", MethodBase.GetCurrentMethod().Name, "rejected"), e.Message);
                trace.Add(string.Format("{0} - {1} - StackTrace", MethodBase.GetCurrentMethod().Name, "rejected"), e.StackTrace);
                log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);
                log.LogError(correlationId, $"'{methodName}' - rejected", trace);
                throw;
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - busobjkey finished");
                log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
            }

            return msg;
        }
    }
}
