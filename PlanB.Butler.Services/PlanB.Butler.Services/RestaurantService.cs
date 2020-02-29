// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

namespace PlanB.Butler.Services
{
    /// <summary>
    /// RestaurantService.
    /// </summary>
    public static class RestaurantService
    {
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

            List<RestaurantModel> restaurant = new List<RestaurantModel>();

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

                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                actionResult = new OkObjectResult(restaurant);
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
        /// Creates the restaurant.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName("CreateRestaurant")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                trace.Add("requestBody", requestBody);

                RestaurantModel restaurantModel = JsonConvert.DeserializeObject<RestaurantModel>(requestBody);

                var filename = $"{restaurantModel.Name}-{restaurantModel.City}.json";
                trace.Add($"filename", filename);

                req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{filename}");
                if (blob != null)
                {
                    blob.Properties.ContentType = "application/json";
                    blob.Metadata.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString().Replace("-", string.Empty));
                    var restaurant = JsonConvert.SerializeObject(restaurantModel);
                    trace.Add("restaurant", restaurant);

                    Task task = blob.UploadTextAsync(requestBody);
                    task.Wait();
                }

                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                actionResult = new OkResult();
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
