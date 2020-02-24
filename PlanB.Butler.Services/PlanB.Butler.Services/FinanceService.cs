// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
using PlanB.Butler.Services.Extensions;

namespace PlanB.Butler.Services
{
    /// <summary>
    /// Finance.
    /// </summary>
    public static class FinanceService
    {
        /// <summary>
        /// Gets the salary deduction.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// SalaryDeduction.
        /// </returns>
        [FunctionName(nameof(GetSalaryDeduction))]
        public static async Task<string> GetSalaryDeduction(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
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
            CloudBlobContainer container = blobClient.GetContainerReference("salarydeduction");
            BlobContinuationToken token = new BlobContinuationToken();
            var operationContext = new OperationContext();
            var options = new BlobRequestOptions();
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference("salarydeduction");
            BlobContinuationToken blobContinuationToken = null;
            List<SalaryDeduction> orderBlob = new List<SalaryDeduction>();
            var blobs = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);
            Microsoft.Extensions.Primitives.StringValues month;
            req.Headers.TryGetValue("user", out month);
            string stringMonth = Convert.ToString(month);
            foreach (var item in blobs.Results)
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;
                await blob.FetchAttributesAsync();
                DateTime date = DateTime.Now;

                if (blob.Metadata.Contains(new KeyValuePair<string, string>("month", stringMonth)))
                {
                    Order order = new Order();
                    await blob.FetchAttributesAsync();
                    var blobDownload = blob.DownloadTextAsync();
                    var blobData = blobDownload.Result;
                    orderBlob.Add(JsonConvert.DeserializeObject<SalaryDeduction>(blobData));
                }
            }

            return JsonConvert.SerializeObject(orderBlob);
        }

        /// <summary>
        /// Posts the document money.
        /// </summary>
        /// <param name="messageHeader">The message header.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        [Singleton]
        [FunctionName(nameof(PostDocumentMoney))]
        public static void PostDocumentMoney(
            [ServiceBusTrigger("q.planbutlerupdatemoney", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
            [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]string blob,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Guid.Parse(messageHeader.CorrelationId);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);

            try
            {
                blob = Encoding.Default.GetString(messageHeader.Body);
                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
            }
            catch (Exception e)
            {
                trace.Add(string.Format("{0} - {1}", methodName, "rejected"), e.Message);
                trace.Add(string.Format("{0} - {1} - StackTrace", methodName, "rejected"), e.StackTrace);
                trace.Add("MessageId", messageHeader.MessageId);
                trace.Add("DeliveryCount", messageHeader.SystemProperties.DeliveryCount.ToString());
                if (messageHeader.SystemProperties.DeliveryCount == 1)
                {
                    log.LogError(correlationId, $"'{methodName}' - rejected", trace, e);
                }

                log.LogInformation(correlationId, $"'{methodName}' - {messageHeader.SystemProperties.DeliveryCount} - rejected", trace);

                throw;
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - finished");
                log.LogInformation(correlationId, $"'{methodName}' - {messageHeader.SystemProperties.DeliveryCount} - finished", trace);
            }
        }

        /// <summary>
        /// This Function is executed when a service bus trigger is received.
        /// </summary>
        /// <param name="messageHeader">.</param>
        /// <param name="blob">Blob.</param>
        /// <param name="log">log.</param>
        [Singleton]
        [FunctionName(nameof(PostDocumentSalary))]
        public static async void PostDocumentSalary(
    [ServiceBusTrigger("q.planbutlerupdatesalary", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
    [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
    ILogger log)
        {
            string payload = Encoding.Default.GetString(messageHeader.Body);
            SalaryDeduction orderBlob = new SalaryDeduction();
            orderBlob.Order = new List<Order>();
            orderBlob = JsonConvert.DeserializeObject<SalaryDeduction>(payload);
            string name = string.Empty;
            DateTime date = DateTime.Now;
            foreach (var item in orderBlob.Order)
            {
                date = item.Date;
                break;
            }

            var stringday = date.Day.ToString();
            var stringMonth = date.Month.ToString();

            blob.Metadata.Add("month", stringMonth);
            blob.Metadata.Add("day", stringday);
            await blob.UploadTextAsync(payload);
            await blob.SetMetadataAsync();
        }

        /// <summary>
        /// PostDocumentExcel.
        /// </summary>
        /// <param name="messageHeader">messageHeader.</param>
        /// <param name="payload">payload as byte[].</param>
        /// <param name="log">log.</param>
        [Singleton]
        [FunctionName(nameof(PostDocumentExcel))]
        public static void PostDocumentExcel(
            [ServiceBusTrigger("q.planbutlerupdateexcel", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
            [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]out byte[] payload,
            ILogger log)
        {
            payload = messageHeader.Body;
        }
    }
}
