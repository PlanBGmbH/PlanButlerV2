// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Mime;
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
        /// The meta date.
        /// </summary>
        private const string MetaDate = "date";

        /// <summary>
        /// The meta restaurant.
        /// </summary>
        private const string MetaRestaurant = "restaurant";

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
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(MealModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
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

            IActionResult actionResult = null;
            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    trace.Add("requestBody", requestBody);

                    MealModel mealModel = JsonConvert.DeserializeObject<MealModel>(requestBody);
                    if (mealModel.CorrelationId == null || mealModel.CorrelationId.Equals(Guid.Empty))
                    {
                        mealModel.CorrelationId = correlationId;
                    }

                    bool isValid = ValidateMeal(mealModel, correlationId, out ErrorModel errorModel);

                    if (isValid)
                    {
                        var date = mealModel.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                        var filename = $"{date}-{mealModel.Restaurant}.json";
                        trace.Add($"filename", filename);

                        req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                        CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{filename}");
                        if (blob != null)
                        {
                            blob.Properties.ContentType = "application/json";
                            var metaDate = mealModel.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                            blob.Metadata.Add(MetaDate, metaDate);
                            blob.Metadata.Add(MetaRestaurant, mealModel.Restaurant);
                            blob.Metadata.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString().Replace("-", string.Empty));
                            var meal = JsonConvert.SerializeObject(mealModel);
                            trace.Add("meal", meal);

                            Task task = blob.UploadTextAsync(requestBody);
                            task.Wait();
                            actionResult = new OkObjectResult(mealModel);
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
        /// Updates the meal by identifier.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="existingContent">The BLOB.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>IActionResult.</returns>
        [FunctionName("UpdateMealById")]
        [Consumes(MediaTypeNames.Application.Json)]
        [ProducesResponseType(typeof(MealModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        public static async Task<IActionResult> UpdateMealById(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "meals/{id}")] HttpRequest req,
            string id,
            [Blob("meals/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] string existingContent,
            [Blob("meals", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            MealModel mealModel = null;
            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                    trace.Add("id", id);
                    mealModel = JsonConvert.DeserializeObject<MealModel>(existingContent);

                    var date = mealModel.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

                    var filename = $"{date}-{mealModel.Restaurant}.json";
                    trace.Add($"filename", filename);
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    trace.Add("requestBody", requestBody);
                    mealModel = JsonConvert.DeserializeObject<MealModel>(requestBody);

                    req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                    bool isValid = ValidateMeal(mealModel, correlationId, out ErrorModel errorModel);
                    if (isValid)
                    {
                        CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{filename}");
                        if (blob != null)
                        {
                            blob.Properties.ContentType = "application/json";
                            var metaDate = mealModel.Date.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
                            blob.Metadata.Add(MetaDate, metaDate);
                            blob.Metadata.Add(MetaRestaurant, mealModel.Restaurant);
                            blob.Metadata.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString().Replace("-", string.Empty));
                            var meal = JsonConvert.SerializeObject(mealModel);
                            trace.Add("meal", meal);

                            Task task = blob.UploadTextAsync(meal);
                            task.Wait();
                            actionResult = new OkObjectResult(mealModel);
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
                    actionResult = new BadRequestObjectResult(mealModel);
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
        /// Reads the meals.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="cloudBlobContainer">The cloud BLOB container.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// All meals.
        /// </returns>
        [ProducesResponseType(typeof(List<MealModel>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [FunctionName("GetMeals")]
        public static async Task<IActionResult> GetMeals(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "meals")] HttpRequest req,
            [Blob("meals", FileAccess.ReadWrite, Connection = "StorageSend")] CloudBlobContainer cloudBlobContainer,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            List<MealModel> meals = new List<MealModel>();

            try
            {
                trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                string startDateQuery = req.Query["startDate"];
                string endDateQuery = req.Query["endDate"];
                string restaurantQuery = req.Query["restaurant"];
                string prefix = string.Empty;

                bool checkForDate = false;
                DateTime start = DateTime.MinValue;
                DateTime end = DateTime.MinValue;

                if (!(string.IsNullOrEmpty(startDateQuery) && string.IsNullOrEmpty(endDateQuery)))
                {
                    checkForDate = true;
                    DateTime.TryParse(startDateQuery, out start);
                    DateTime.TryParse(endDateQuery, out end);
                }

                if (checkForDate)
                {
                    prefix = CreateBlobPrefix(startDateQuery, endDateQuery);
                }

                BlobContinuationToken blobContinuationToken = null;
                var options = new BlobRequestOptions();
                var operationContext = new OperationContext();

                List<IListBlobItem> cloudBlockBlobs = new List<IListBlobItem>();
                do
                {
                    var blobs = await cloudBlobContainer.ListBlobsSegmentedAsync(prefix, true, BlobListingDetails.All, null, blobContinuationToken, options, operationContext).ConfigureAwait(false);
                    blobContinuationToken = blobs.ContinuationToken;
                    cloudBlockBlobs.AddRange(blobs.Results);
                }
                while (blobContinuationToken != null);

                foreach (var item in cloudBlockBlobs)
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;
                    if (checkForDate)
                    {
                        await blob.FetchAttributesAsync();
                        if (blob.Metadata.ContainsKey(MetaDate))
                        {
                            var mealMetaDate = blob.Metadata[MetaDate];
                            DateTime mealDate = DateTime.MinValue;
                            if (DateTime.TryParse(mealMetaDate, out mealDate))
                            {
                                var isDateInRange = IsDateInRange(start, end, mealDate);
                                if (isDateInRange)
                                {
                                    var blobContent = blob.DownloadTextAsync();
                                    var blobMeal = JsonConvert.DeserializeObject<MealModel>(await blobContent);
                                    meals.Add(blobMeal);
                                }
                            }
                        }
                    }
                    else
                    {
                        var content = blob.DownloadTextAsync();
                        var meal = JsonConvert.DeserializeObject<MealModel>(await content);
                        meals.Add(meal);
                    }
                }

                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                actionResult = new OkObjectResult(meals);
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
        /// Gets the meal by id.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        /// <returns>
        /// Meal by id.
        /// </returns>
        [ProducesResponseType(typeof(ErrorModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(MealModel), StatusCodes.Status200OK)]
        [FunctionName("GetMealById")]
        public static IActionResult GetMealById(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "meals/{id}")] HttpRequest req,
            string id,
            [Blob("meals/{id}.json", FileAccess.ReadWrite, Connection = "StorageSend")] string blob,
            ILogger log,
            ExecutionContext context)
        {
            Guid correlationId = Util.ReadCorrelationId(req.Headers);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            IActionResult actionResult = null;

            MealModel mealModel = null;

            try
            {
                trace.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString());
                trace.Add("id", id);
                mealModel = JsonConvert.DeserializeObject<MealModel>(blob);

                log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                actionResult = new OkObjectResult(mealModel);
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
                actionResult = new BadRequestObjectResult(mealModel);
            }
            finally
            {
                log.LogTrace(eventId, $"'{methodName}' - finished");
                log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
            }

            return actionResult;
        }

        /// <summary>
        /// Determines whether the date in range compared to start and end.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <param name="toCheck">To check.</param>
        /// <returns>
        ///   <c>true</c> if date is in range; otherwise, <c>false</c>.
        /// </returns>
        internal static bool IsDateInRange(DateTime start, DateTime end, DateTime toCheck)
        {
            if (toCheck < start)
            {
                return false;
            }

            if (end < toCheck)
            {
                return false;
            }

            if (start.Equals(toCheck))
            {
                return true;
            }

            if (end.Equals(toCheck))
            {
                return true;
            }

            if (start.Equals(end) && start.Equals(toCheck))
            {
                return true;
            }

            long difference = toCheck.Ticks - start.Ticks;
            long sum = start.Ticks + difference;

            if (sum < end.Ticks)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates the BLOB prefix.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <returns>Prefix.</returns>
        internal static string CreateBlobPrefix(string startDate, string endDate)
        {
            string prefix = string.Empty;
            if (string.IsNullOrEmpty(startDate) || string.IsNullOrEmpty(endDate))
            {
                return prefix;
            }

            if (startDate.Length == endDate.Length)
            {
                for (int i = 0; i < startDate.Length; i++)
                {
                    if (startDate[i] == endDate[i])
                    {
                        prefix += startDate[i];
                    }
                    else
                    {
                        break;
                    }
                }
            }

            return prefix;
        }

        /// <summary>
        /// Validates the meal.
        /// </summary>
        /// <param name="mealModel">The meal model.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="errorModel">The error model.</param>
        /// <returns><c>True</c> if data is valid; otherwise <c>False</c>.</returns>
        internal static bool ValidateMeal(MealModel mealModel, Guid correlationId, out ErrorModel errorModel)
        {
            bool isValid = true;
            errorModel = null;
            if (string.IsNullOrEmpty(mealModel.Name))
            {
                errorModel = new ErrorModel()
                {
                    CorrelationId = correlationId,
                    Message = "No meal name!",
                };
                isValid = false;
            }

            if (string.IsNullOrEmpty(mealModel.Restaurant))
            {
                errorModel = new ErrorModel()
                {
                    CorrelationId = correlationId,
                    Message = "No meal restaurant!",
                };
                isValid = false;
            }

            if (mealModel.Date == null || mealModel.Date == DateTime.MinValue)
            {
                errorModel = new ErrorModel()
                {
                    CorrelationId = correlationId,
                    Message = "No meal date!",
                };
                isValid = false;
            }

            return isValid;
        }
    }
}
