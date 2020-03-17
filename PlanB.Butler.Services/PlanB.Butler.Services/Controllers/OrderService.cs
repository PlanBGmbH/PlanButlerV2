// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using PlanB.Butler.Services.Extensions;
using PlanB.Butler.Services.Models;

namespace PlanB.Butler.Services.Controllers
{
    /// <summary>
    /// OrderService.
    /// </summary>
    public static class OrderService
    {
        /// <summary>
        /// Updates the Restaurant by identifier.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="existingContent">The BLOB.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName("UpdateOrderById")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(RestaurantModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        public static async Task<IActionResult> UpdateOrderById(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "order/{id}")] HttpRequest req,
            string id,
            [Blob("order/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] string existingContent,
            [Blob("order", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            OrderModel orderModel = null;

            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                    trace.Add("id", id);
                    orderModel = JsonConvert.DeserializeObject<OrderModel>(existingContent);

                    var date = orderModel.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    var filename = $"{date}-{orderModel}.json";
                    trace.Add($"filename", filename);
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    trace.Add("requestBody", requestBody);
                    orderModel = JsonConvert.DeserializeObject<OrderModel>(requestBody);

                    req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                    bool isValid = Validate(orderModel, correlationId, log, out ErrorModel errorModel);
                    if (isValid)
                    {
                        CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{filename}");
                        if (blob != null)
                        {
                            blob.Properties.ContentType = "application/json";
                            var restaurant = JsonConvert.SerializeObject(orderModel);
                            trace.Add("restaurant", restaurant);

                            Task task = blob.UploadTextAsync(restaurant);
                            task.Wait();
                            actionResult = new OkObjectResult(orderModel);
                            log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                        }
                    }
                    else
                    {
                        actionResult = new BadRequestObjectResult(errorModel);
                        log.LogInformation(correlationId, $"'{methodName}' - is not valid", trace);
                    }
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
            }

            return actionResult;
        }

        /// <summary>
        /// Deletes the meal by identifier.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>IActionResult.</returns>
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [FunctionName("DeleteOrderById")]
        public static IActionResult DeleteOrderById(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "order/{id}")] HttpRequest req,
            string id,
            [Blob("order/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlockBlob blob,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                    trace.Add("id", id);

                    if (blob != null)
                    {
                        Task task = blob.DeleteIfExistsAsync();
                        task.Wait();

                        actionResult = new OkResult();
                        log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                    }
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
            }

            return actionResult;
        }

        /// <summary>
        /// Gets the daily order overview for user.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="blob">blob.</param>
        /// <param name="log">The log.</param>
        /// <returns>Daily Overview.</returns>
        [ProducesResponseType(typeof(List<OrdersModel>), StatusCodes.Status200OK)]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [FunctionName(nameof(GetOrder))]
        public static async Task<IActionResult> GetOrder(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders")] HttpRequest req,
        [Blob("orders", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlobContainer blob,
        ILogger log)
        {
            IActionResult actionResult;
            string username = req.Query["user"];
            string dateVal = req.Query["date"];

            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            List<IListBlobItem> cloudBlockBlobs = new List<IListBlobItem>();
            List<OrdersModel> orderBlob = new List<OrdersModel>();

            try
            {
                string blobData = string.Empty;
                string connectionString = string.Empty;
                BlobContinuationToken blobContinuationToken = null;
                var options = new BlobRequestOptions();
                var operationContext = new OperationContext();
                BlobResultSegment blobs;

                do
                {
                    if (string.IsNullOrEmpty(dateVal))
                    {
                        blobs = await blob.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);
                    }
                    else
                    {
                        // TODO: Remove the prefix orders_.
                        blobs = await blob.ListBlobsSegmentedAsync(dateVal, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);
                    }

                    blobContinuationToken = blobs.ContinuationToken;
                    cloudBlockBlobs.AddRange(blobs.Results);
                }
                while (blobContinuationToken != null);

                foreach (var item in cloudBlockBlobs)
                {
                    CloudBlockBlob blobitem = (CloudBlockBlob)item;
                    if (string.IsNullOrEmpty(username))
                    {
                        var blobContent = blobitem.DownloadTextAsync();
                        var orderItem = JsonConvert.DeserializeObject<OrdersModel>(await blobContent);
                        OrdersModel tmp = new OrdersModel
                        {
                            Orders = new List<OrderModel>(),
                        };
                        tmp = orderItem;
                        orderBlob.Add(tmp);
                    }
                    else
                    {
                        if (blobitem.Metadata.Contains(new KeyValuePair<string, string>("user", username)))
                        {
                            var blobContent = blobitem.DownloadTextAsync();
                            var orderItem = JsonConvert.DeserializeObject<OrdersModel>(await blobContent);
                            OrdersModel tmp = new OrdersModel
                            {
                                Orders = new List<OrderModel>(),
                            };
                            tmp = orderItem;
                            orderBlob.Add(tmp);
                        }
                    }
                }

                trace.Add("date", dateVal);
                trace.Add("data", blobData);
                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                actionResult = new OkObjectResult(orderBlob);
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
                    Label = $"{stringDate}_{orders.LoginName}.json",
                    CorrelationId = correlationId.ToString(),
                };

                trace.Add("loginname", orders.LoginName);
                trace.Add("label", outputMessage.Label);
                actionResult = new OkObjectResult(outputMessage);
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

        /// <summary>
        /// Gets the order by identifier.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>Order.</returns>
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(OrderModel), StatusCodes.Status200OK)]
        [FunctionName("GetOrderById")]
        public static IActionResult GetOrderById(
             [HttpTrigger(AuthorizationLevel.Function, "get", Route = "orders/{id}")] HttpRequest req,
             string id,
             [Blob("orders/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] string blob,
             ILogger log,
             ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            OrderModel orderModel = null;
            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                    trace.Add("id", id);
                    orderModel = JsonConvert.DeserializeObject<OrderModel>(blob);

                    log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                    actionResult = new OkObjectResult(orderModel);
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
            }

            return actionResult;
        }

        /// <summary>
        /// Validates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="log">The log.</param>
        /// <param name="errorModel">The error model.</param>
        /// <returns><c>True</c> if data is valid; otherwise <c>False</c>.</returns>
        internal static bool Validate(OrderModel model, Guid correlationId, ILogger log, out ErrorModel errorModel)
        {
            bool isValid = true;
            errorModel = null;
            var trace = new Dictionary<string, string>();
            var methodName = MethodBase.GetCurrentMethod().Name;
            trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());

            StringBuilder message = new StringBuilder();

            if (string.IsNullOrEmpty(model.Name))
            {
                message.Append("No restaurant name!");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.CompanyName))
            {
                message.Append("No company name !");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.Restaurant))
            {
                message.Append("No restaurant");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.Meal))
            {
                message.Append("No meal");
                isValid = false;
            }

            if (double.IsNaN(model.Price))
            {
                message.Append("No price");
                isValid = false;
            }

            if (double.IsNaN(model.Benefit))
            {
                message.Append("No benefit!");
                isValid = false;
            }

            if (!isValid)
            {
                errorModel = new ErrorModel()
                {
                    CorrelationId = correlationId,
                    Message = message.ToString(),
                };
                trace.Add("Message", errorModel.Message);
                log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);
            }
            else
            {
                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
            }

            log.LogInformation(correlationId, $"'{methodName}' - finished", trace);

            return isValid;
        }
    }
}
