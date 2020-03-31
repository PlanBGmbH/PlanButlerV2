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
using System.Web;

using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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
    /// RestaurantService.
    /// </summary>
    public static class RestaurantService
    {
        /// <summary>
        /// The meta restaurant.
        /// </summary>
        private const string MetaRestaurant = "restaurant";

        /// <summary>
        /// The meta date.
        /// </summary>
        private const string MetaDate = "date";

        /// <summary>
        /// The meta city.
        /// </summary>
        private const string MetaCity = "city";

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
        [FunctionName("UpdateRestaurantById")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(RestaurantModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        public static async Task<IActionResult> UpdateRestaurantById(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "restaurants/{id}")] HttpRequest req,
            string id,
            [Blob("restaurants/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] string existingContent,
            [Blob("restaurants", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            RestaurantModel restaurantModel = null;

            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                    trace.Add("id", id);
                    restaurantModel = JsonConvert.DeserializeObject<RestaurantModel>(existingContent);

                    //var date = restaurantModel.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    var filename = $"{restaurantModel.Name}-{restaurantModel.City}.json";
                    trace.Add($"filename", filename);
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    trace.Add("requestBody", requestBody);
                    restaurantModel = JsonConvert.DeserializeObject<RestaurantModel>(requestBody);

                    req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                    bool isValid = Validate(restaurantModel, correlationId, log, out ErrorModel errorModel);
                    if (isValid)
                    {
                        CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{filename}");
                        if (blob != null)
                        {
                            blob.Properties.ContentType = "application/json";
                            //var metaDate = restaurantModel.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                            //blob.Metadata.Add(MetaDate, metaDate);
                            blob.Metadata.Add(MetaRestaurant, restaurantModel.ToString());
                            blob.Metadata.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString().Replace("-", string.Empty));
                            var restaurant = JsonConvert.SerializeObject(restaurantModel);
                            trace.Add("restaurant", restaurant);

                            Task task = blob.UploadTextAsync(restaurant);
                            task.Wait();
                            actionResult = new OkObjectResult(restaurantModel);
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
        /// Gets the restaurants.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>All Restaurants.</returns>
        [ProducesResponseType(typeof(List<RestaurantModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [FunctionName("GetRestaurants")]
        public static async Task<IActionResult> GetRestaurants(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "restaurants")] HttpRequest req,
            [Blob("restaurants", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            List<RestaurantModel> restaurants = new List<RestaurantModel>();
            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    BlobContinuationToken blobContinuationToken = null;
                    var options = new BlobRequestOptions();
                    var operationContext = new OperationContext();

                    List<IListBlobItem> cloudBlockBlobs = new List<IListBlobItem>();
                    do
                    {
                        var blobs = await cloudBlobContainer.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);
                        blobContinuationToken = blobs.ContinuationToken;
                        cloudBlockBlobs.AddRange(blobs.Results);
                    }
                    while (blobContinuationToken != null);

                    foreach (var item in cloudBlockBlobs)
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;
                        var blobContent = blob.DownloadTextAsync();
                        var blobRestaurant = JsonConvert.DeserializeObject<RestaurantModel>(await blobContent);
                        restaurants.Add(blobRestaurant);
                    }

                    log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                    actionResult = new OkObjectResult(restaurants);
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
        /// Gets the restaurant by identifier.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>Restaurant.</returns>
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(RestaurantModel), StatusCodes.Status200OK)]
        [FunctionName("GetRestaurantById")]
        public static IActionResult GetRestaurantById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "restaurants/{id}")] HttpRequest req,
            string id,
            [Blob("restaurants/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] string blob,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            RestaurantModel restaurantModel = null;
            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                    trace.Add("id", id);
                    restaurantModel = JsonConvert.DeserializeObject<RestaurantModel>(blob);

                    log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                    actionResult = new OkObjectResult(restaurantModel);
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
        /// Deletes the restaurant by identifier.
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
        [FunctionName("DeleteRestaurantById")]
        public static IActionResult DeleteRestaurantById(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "restaurants/{id}")] HttpRequest req,
            string id,
            [Blob("restaurants/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlockBlob blob,
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
        /// Creates the restaurant.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName("CreateRestaurant")]
        [ProducesResponseType(typeof(RestaurantModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        public static async Task<IActionResult> CreateRestaurant(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "restaurants")]
            [RequestBodyType(typeof(RestaurantModel), "Restaurant request")]HttpRequest req,
            [Blob("restaurants", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
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
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    trace.Add("requestBody", requestBody);

                    RestaurantModel restaurantModel = JsonConvert.DeserializeObject<RestaurantModel>(requestBody);

                    bool isValid = Validate(restaurantModel, correlationId, log, out ErrorModel errorModel);

                    if (isValid)
                    {
                        var fileName = CreateFileName(restaurantModel);
                        trace.Add($"fileName", fileName);
                        restaurantModel.Id = fileName;

                        var fullFileName = $"{fileName}.json";
                        trace.Add($"fullFileName", fullFileName);

                        req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                        CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{fullFileName}");
                        if (blob != null)
                        {
                            blob.Properties.ContentType = "application/json";
                            blob.Metadata.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString().Replace("-", string.Empty));
                            blob.Metadata.Add(MetaRestaurant, HttpUtility.HtmlEncode(restaurantModel.Name));
                            blob.Metadata.Add(MetaCity, HttpUtility.HtmlEncode(restaurantModel.City));
                            var restaurant = JsonConvert.SerializeObject(restaurantModel);
                            trace.Add("restaurant", restaurant);

                            Task task = blob.UploadTextAsync(restaurant);
                            task.Wait();

                            actionResult = new OkObjectResult(restaurantModel);
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
        /// Validates the specified model.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="log">The log.</param>
        /// <param name="errorModel">The error model.</param>
        /// <returns><c>True</c> if data is valid; otherwise <c>False</c>.</returns>
        internal static bool Validate(RestaurantModel model, Guid correlationId, ILogger log, out ErrorModel errorModel)
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

            if (string.IsNullOrEmpty(model.City))
            {
                message.Append("No restaurant city!");
                isValid = false;
            }

            if (string.IsNullOrEmpty(model.PhoneNumber))
            {
                message.Append("No restaurant phone!");
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

        /// <summary>
        /// Creates the name of the file.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>FileName without extension.</returns>
        internal static string CreateFileName(RestaurantModel model)
        {
            string fileName = $"{model.Name}-{model.City}";
            fileName = HttpUtility.UrlEncode(fileName);
            return fileName;
        }
    }
}
