// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using BotLibraryV2;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlanB.Butler.Services.Extensions;
using PlanB.Butler.Services.Models;

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
        /// <param name="blob">binding to the blob.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Daily Overview.
        /// </returns>
        [ProducesResponseType(typeof(List<OrderBlob>), StatusCodes.Status200OK)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [FunctionName(nameof(GetDailyOrderOverview))]
        public static async Task<string> GetDailyOrderOverview(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            [Blob("orders", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlobContainer blob,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            List<OrderBlob> orders = new List<OrderBlob>();
            List<IListBlobItem> cloudBlockBlobs = new List<IListBlobItem>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddJsonFile("secret.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            List<OrderBlob> orderBlob = new List<OrderBlob>();
            try
            {
                string stringDate = string.Empty;
                string blobData = string.Empty;
                Microsoft.Extensions.Primitives.StringValues date = string.Empty;

                req.Headers.TryGetValue(date, out date);

                BlobContinuationToken blobContinuationToken = null;
                var options = new BlobRequestOptions();
                var operationContext = new OperationContext();

                do
                {
                    var blobs = await blob.ListBlobsSegmentedAsync(date.ToString(), true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);
                    blobContinuationToken = blobs.ContinuationToken;
                    cloudBlockBlobs.AddRange(blobs.Results);
                }
                while (blobContinuationToken != null);

                foreach (var item in cloudBlockBlobs)
                {
                    CloudBlockBlob blobs = (CloudBlockBlob)item;
                    var blobContent = blobs.DownloadTextAsync();
                    var orderItem = JsonConvert.DeserializeObject<OrderBlob>(await blobContent);
                    orders.Add(orderItem);
                }

                trace.Add("date", stringDate);
                trace.Add("data", blobData);
                trace.Add("requestbody", req.Body.ToString());
            }
            catch (Exception e)
            {
                trace.Add(string.Format("{0} - {1}", methodName, "rejected"), e.Message);
                trace.Add(string.Format("{0} - {1} - StackTrace", methodName, "rejected"), e.StackTrace);
                log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);
                log.LogError(correlationId, $"'{methodName}' - rejected", trace);
                throw;
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - finished");
                log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
            }

            return JsonConvert.SerializeObject(cloudBlockBlobs);
        }

        /// <summary>
        /// Gets the daily order overview for user.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="blob">blob.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>Daily Overview.</returns>
        [ProducesResponseType(typeof(List<OrderBlob>), StatusCodes.Status200OK)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [FunctionName(nameof(GetDailyOrderOverviewForUser))]
        public static async Task<string> GetDailyOrderOverviewForUser(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        [Blob("orders/{Label}.json", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
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
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);

            List<OrderBlob> orderBlob = new List<OrderBlob>();
            try
            {
                string stringDate = string.Empty;
                string blobData = string.Empty;
                string username = string.Empty;
                string connectionString = string.Empty;

                req.Headers.TryGetValue("user", out Microsoft.Extensions.Primitives.StringValues user);

                await blob.FetchAttributesAsync();

                connectionString = config["StorageSend"];
                DateTime date = DateTime.Now;
                stringDate = date.ToString("yyyy-MM-dd");
                username = user.ToString();

                if (blob.Metadata.Contains(new KeyValuePair<string, string>("date", stringDate)) && blob.Metadata.Contains(new KeyValuePair<string, string>("user", username)))
                {
                    await blob.FetchAttributesAsync();
                    var blobDownload = blob.DownloadTextAsync();
                    blobData = blobDownload.Result;
                    orderBlob.Add(JsonConvert.DeserializeObject<OrderBlob>(blobData));
                }

                trace.Add("date", stringDate);
                trace.Add("data", blobData);
                trace.Add("requestbody", req.Body.ToString());
            }
            catch (Exception e)
            {
                trace.Add(string.Format("{0} - {1}", methodName, "rejected"), e.Message);
                trace.Add(string.Format("{0} - {1} - StackTrace", methodName, "rejected"), e.StackTrace);
                log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);
                log.LogError(correlationId, $"'{methodName}' - rejected", trace);
                throw;
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - finished");
                log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
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
        [ProducesResponseType(typeof(List<OrderBlob>), StatusCodes.Status200OK)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [FunctionName(nameof(PostDocumentOrder))]
        public static async void PostDocumentOrder(
           [ServiceBusTrigger("q.planbutlerupdateorder", Connection = "butlerSend")]Message messageHeader,
           [Blob("orders/{Label}.json", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
           ILogger log,
           ExecutionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            Guid correlationId = new Guid($"orderqueue {messageHeader.Label}");
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            try
            {
                string payload = Encoding.Default.GetString(messageHeader.Body);
                OrderBlob orderBlob = JsonConvert.DeserializeObject<OrderBlob>(payload);
                orderBlob.OrderList = new List<Order>();
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
                trace.Add("data", payload);
                trace.Add("date", date.ToString());
                trace.Add("name", name);
                trace.Add("requestbody", messageHeader.Body.ToString());
            }
            catch (Exception e)
            {
                trace.Add(string.Format("{0} - {1}", methodName, "rejected"), e.Message);
                trace.Add(string.Format("{0} - {1} - StackTrace", methodName, "rejected"), e.StackTrace);
                log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);
                log.LogError(correlationId, $"'{methodName}' - rejected", trace);
                throw;
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - finished");
                log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
            }
        }

        /// <summary>
        /// Creates the order.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="outputMessage">The output message.</param>
        /// <param name="log">The log.</param>
        /// <returns>IActionResult.</returns>
        /// <exception cref="ArgumentNullException">log.</exception>
        [Singleton]
        [FunctionName("CreateOrder")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        public static IActionResult CreateOrder(
            [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "orders")] HttpRequest input,
            [ServiceBus("q.planbutlerupdateorder", Connection = "ServiceBusConnection")] out Message outputMessage,
            ILogger log)
        {
            if (log is null)
            {
                throw new ArgumentNullException(nameof(log));
            }

            Guid correlationId = Util.ReadCorrelationId(input.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;
            outputMessage = null;

            try
            {
                trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                var body = input.ReadAsStringAsync().Result;
                trace.Add("body", body);
                OrdersModel orders = JsonConvert.DeserializeObject<OrdersModel>(body);
                var stringDate = orders.Date.ToString("yyyy-MM-dd");
                trace.Add("date", stringDate);

                byte[] bytes = Encoding.ASCII.GetBytes(body);
                outputMessage = new Message(bytes)
                {
                    Label = $"{orders.LoginName}_{stringDate}",
                    CorrelationId = correlationId.ToString(),
                };

                trace.Add("loginname", orders.LoginName);
                trace.Add("label", outputMessage.Label);
                actionResult = new AcceptedResult();
                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
            }
            catch (Exception e)
            {
                trace.Add(string.Format("{0} - {1}", methodName, "rejected"), e.Message);
                trace.Add(string.Format("{0} - {1} - StackTrace", methodName, "rejected"), e.StackTrace);
                log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);
                log.LogError(correlationId, $"'{methodName}' - rejected", trace);
                ErrorModel errorModel = new ErrorModel()
                {
                    CorrelationId = correlationId,
                    Details = e.StackTrace,
                    Message = e.Message,
                };
                actionResult = new BadRequestObjectResult(errorModel);
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - finished");
                log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
            }

            return actionResult;
        }
    }
}
