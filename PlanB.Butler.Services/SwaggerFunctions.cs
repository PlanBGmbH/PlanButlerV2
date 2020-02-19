// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System.Net.Http;
using System.Threading.Tasks;

using AzureFunctions.Extensions.Swashbuckle;
using AzureFunctions.Extensions.Swashbuckle.Attribute;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace PlanB.Butler.Services
{
    /// <summary>
    /// SwaggerFunctions.
    /// </summary>
    public static class SwaggerFunctions
    {
        /// <summary>
        /// Swaggers the specified req.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="swashBuckleClient">The swash buckle client.</param>
        /// <returns>HttpResponseMessage.</returns>
        [SwaggerIgnore]
        [FunctionName("Swagger")]
        public static Task<HttpResponseMessage> Swagger(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "swagger/json")]
            HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient swashBuckleClient)
        {
            return Task.FromResult(swashBuckleClient.CreateSwaggerDocumentResponse(req));
        }

        /// <summary>
        /// Swaggers the UI.
        /// </summary>
        /// <param name="req">The req.</param>
        /// <param name="swashBuckleClient">The swash buckle client.</param>
        /// <returns>HttpResponseMessage.</returns>
        [SwaggerIgnore]
        [FunctionName("SwaggerUi")]
        public static Task<HttpResponseMessage> SwaggerUi(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "swagger/ui")]
            HttpRequestMessage req,
            [SwashBuckleClient] ISwashBuckleClient swashBuckleClient)
        {
            return Task.FromResult(swashBuckleClient.CreateSwaggerUIResponse(req, "swagger/json"));
        }
    }
}
