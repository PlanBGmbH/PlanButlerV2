// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using PlanB.Butler.Services.Extensions;
using PlanB.Butler.Services.Models;

namespace PlanB.Butler.Services.ServiceBus
{
    /// <summary>
    /// ButlerBus.
    /// </summary>
    public static class ButlerBus
    {
        /// <summary>
        /// The meta date.
        /// </summary>
        private const string MetaDate = "date";

        /// <summary>
        /// The meta user.
        /// </summary>
        private const string MetaUser = "user";

        /// <summary>
        /// Saves the order.
        /// </summary>
        /// <param name="messageHeader">The message header.</param>
        /// <param name="blob">The BLOB.</param>
        /// <param name="log">The log.</param>
        /// <param name="context">The context.</param>
        [Singleton]
        [FunctionName(nameof(PushOrder))]
        public static void PushOrder(
           [ServiceBusTrigger("q.planbutlerupdateorder", Connection = "butlerSend")]Message messageHeader,
           [Blob("orders/{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
           ILogger log,
           ExecutionContext context)
        {
            Guid correlationId = new Guid(messageHeader.CorrelationId);
            var methodName = MethodBase.GetCurrentMethod().Name;
            var trace = new Dictionary<string, string>();
            EventId eventId = new EventId(correlationId.GetHashCode(), Constants.ButlerCorrelationTraceName);
            using (log.BeginScope("Method:{methodName} CorrelationId:{CorrelationId} Label:{Label}", methodName, correlationId.ToString(), context.InvocationId.ToString()))
            {
                try
                {
                    trace.Add(Constants.ButlerCorrelationTraceHeader, correlationId.ToString());
                    string payload = Encoding.Default.GetString(messageHeader.Body);
                    trace.Add("payload", payload);
                    OrdersModel ordersModel = new OrdersModel
                    {
                        Orders = new List<OrderModel>(),
                    };
                    ordersModel = JsonConvert.DeserializeObject<OrdersModel>(payload);

                    trace.Add("ordersModel.LoginName", ordersModel.LoginName);
                    string name = System.Web.HttpUtility.UrlEncode(ordersModel.LoginName);
                    trace.Add("name", name);

                    DateTime date = ordersModel.Date;
                    var formatedDate = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    trace.Add("formatedDate", formatedDate);

                    log.LogInformation(correlationId, $"'{methodName}' - success", trace);

                    blob.Metadata.Add(MetaUser, name);
                    blob.Metadata.Add(MetaDate, formatedDate);
                    blob.Metadata.Add(Constants.ButlerCorrelationTraceName, correlationId.ToString().Replace("-", string.Empty));

                    Task upload = blob.UploadTextAsync(payload);
                    upload.Wait();
                    trace.Add("upload", "success");

                    Task metaData = blob.SetMetadataAsync();
                    metaData.Wait();
                    trace.Add("metaData", "success");

                    log.LogInformation(correlationId, $"'{methodName}' - success", trace);
                }
                catch (Exception e)
                {
                    trace.Add(string.Format("{0} - {1}", methodName, "rejected"), e.Message);
                    trace.Add(string.Format("{0} - {1} - StackTrace", methodName, "rejected"), e.StackTrace);
                    log.LogInformation(correlationId, $"'{methodName}' - rejected", trace);
                    log.LogError(correlationId, $"'{methodName}' - rejected", trace);
                }
                finally
                {
                    log.LogTrace(eventId, $"'{methodName}' - finished");
                    log.LogInformation(correlationId, $"'{methodName}' - finished", trace);
                }
            }
        }
    }
}
