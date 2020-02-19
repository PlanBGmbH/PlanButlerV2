// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
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
        [FunctionName(nameof(Meals))]
        public static async Task<IActionResult> Meals(
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

                string filename = correlationId.ToString() + ".json";
                trace.Add($"filename", filename);
                req.HttpContext.Response.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());

                CloudBlockBlob blob = cloudBlobContainer.GetBlockBlobReference($"{filename}");
                if (blob != null)
                {
                    blob.Properties.ContentType = "application/json";
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
    }
}
