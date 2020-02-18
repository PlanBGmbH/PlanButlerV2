// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace PlanB.Butler.Services.GetServiceInfo
{
    /// <summary>
    /// GetServiceInfo.
    /// </summary>
    public static class GetServiceInfo
    {
        /// <summary>
        /// Runs the specified req.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="log">The log.</param>
        /// <returns>ServiceInfo.</returns>
        [FunctionName(nameof(GetServiceInfo))]
        public static IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            var serviceInfo = new
            {
                status = "OK",
            };

            return (ActionResult)new OkObjectResult(serviceInfo);
        }
    }
}
