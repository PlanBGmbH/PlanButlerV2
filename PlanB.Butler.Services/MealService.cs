// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
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
    /// MealService.
    /// </summary>
    public static class MealService
    {
        /// <summary>
        /// Create meal.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// IActionResult.
        /// </returns>
        [FunctionName("CreateMeal")]
        public static async Task<IActionResult> CreateMeal(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "meals")]
            [RequestBodyType(typeof(MealModel), "Meal request")]HttpRequest req,
            [Blob("meals", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                trace.Add("requestBody", requestBody);

                MealModel mealModel = JsonConvert.DeserializeObject<MealModel>(requestBody);
                if (mealModel.Id == null || mealModel.Id.Equals(Guid.Empty))
                {
                    mealModel.Id = correlationId;
                }

                var date = mealModel.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                var filename = $"{date}-{mealModel.Restaurant}-{correlationId}.json";
                trace.Add($"filename", filename);

                req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{filename}");
                if (blob != null)
                {
                    blob.Properties.ContentType = "application/json";
                    blob.Metadata.Add("date", date);
                    blob.Metadata.Add("restaurant", mealModel.Restaurant);
                    blob.Metadata.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                    var meal = JsonConvert.SerializeObject(mealModel);
                    trace.Add("meal", meal);

                    Task task = blob.UploadTextAsync(requestBody);
                }

                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
            }
            catch (Exception e)
            {
                trace.Add(string.Format("{0} - {1}", MethodBase.GetCurrentMethod().Name, "rejected"), e.Message);
                trace.Add(string.Format("{0} - {1} - StackTrace", MethodBase.GetCurrentMethod().Name, "rejected"), e.StackTrace);
                log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);

                throw;
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - busobjkey finished");
                log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
            }

            return new OkResult();
        }

        /// <summary>
        /// Reads the meals.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>All meals.</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [FunctionName("GetMeals")]
        public static async Task<IActionResult> ReadMeals(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "meals")] HttpRequest req,
            [Blob("meals", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);

            BlobContinuationToken blobContinuationToken = null;
            var options = new BlobRequestOptions();
            var operationContext = new OperationContext();

            var blobs = await cloudBlobContainer.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);
            List<MealModel> meals = new List<MealModel>();
            foreach (var item in blobs.Results)
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;
                var content = blob.DownloadTextAsync();
                var meal = JsonConvert.DeserializeObject<MealModel>(await content);
                meals.Add(meal);
            }

            return (ActionResult)new OkObjectResult(meals);
        }
    }
}
