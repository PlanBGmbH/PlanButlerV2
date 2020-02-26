// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text;

using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlanB.Butler.Admin
{
    /// <summary>
    /// Util.
    /// </summary>
    internal class Util
    {
        /// <summary>
        /// Creates the contract resolver.
        /// </summary>
        /// <returns>
        /// the JsonSerializerSettings. <seealso cref="JsonSerializerSettings" />
        /// </returns>
        internal static JsonSerializerSettings CreateContractResolver()
        {
            var serializerSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                CheckAdditionalContent = true,
            };

            return serializerSettings;
        }

        /// <summary>
        /// Creates the content of the string.
        /// </summary>
        /// <param name="payloadIn">The payload in.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="log">The log.</param>
        /// <returns>StringContent.</returns>
        internal static StringContent CreateStringContent(string payloadIn, Guid correlationId, ILogger log)
        {
            var methodName = MethodBase.GetCurrentMethod().Name;
            IDictionary<string, string> trace = new Dictionary<string, string>();
            StringContent returnValue = null;
            trace.Add($"{methodName}.payloadIn", payloadIn);
            JObject obj = JObject.Parse(payloadIn);
            string content = JsonConvert.SerializeObject(obj, Util.CreateContractResolver());
            trace.Add($"{methodName}.content", content);
            returnValue = new StringContent(content, Encoding.UTF8, "application/json");

            return returnValue;
        }

        /// <summary>
        /// Adds the default headers.
        /// </summary>
        /// <param name="httpRequestMessage">The HTTP request message.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="functionsKey">The Functions Key.</param>
        internal static void AddDefaultEsbHeaders(HttpRequestMessage httpRequestMessage, Guid correlationId, string functionsKey)
        {
            httpRequestMessage.Headers.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());
            httpRequestMessage.Headers.Add(Constants.FunctionsKeyHeader, functionsKey);
        }
    }
}
