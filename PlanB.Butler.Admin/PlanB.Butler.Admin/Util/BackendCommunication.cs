// Copyright (c) PlanB. GmbH. All Rights Reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Web;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlanB.Butler.Admin.Util
{
    /// <summary>
    /// BackendCommunication.
    /// </summary>
    public class BackendCommunication
    {
        /// <summary>
        /// Gets the document.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="storageAccountUrl">The storage account URL.</param>
        /// <param name="storageAccountKey">The storage account key.</param>
        /// <returns>Document.</returns>
        public string GetDocument(string container, string resourceName, string storageAccountUrl, string storageAccountKey)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string resource = container + "/" + resourceName;
                string sasToken = this.GenerateStorageSasToken(resource, storageAccountUrl, storageAccountKey);
                var response = httpClient.GetAsync(sasToken).Result;
                return response.Content.ReadAsStringAsync().Result;
            }
        }

        /// <summary>
        /// Generates the storage sas token.
        /// </summary>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="storageAccountUrl">The storage account URL.</param>
        /// <param name="storageAccountKey">The storage account key.</param>
        /// <returns>SasToken.</returns>
        public string GenerateStorageSasToken(string resourceName, string storageAccountUrl, string storageAccountKey)
        {
            var storageAccountName = storageAccountUrl.Remove(0, 8).Split('.')[0];
            var version = "2018-03-28";
            var startTime = DateTime.UtcNow;
            var startTimeIso = startTime.ToString("s") + "Z";
            var endTimeIso = startTime.AddMinutes(10).ToString("s") + "Z";
            var hmacSha256 = new System.Security.Cryptography.HMACSHA256 { Key = Convert.FromBase64String(storageAccountKey) };
            var payLoad = string.Format(
                "{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n\n\n\n\n",
                "r",
                startTimeIso,
                endTimeIso,
                "/blob/" + storageAccountName + "/" + resourceName,
                string.Empty,
                string.Empty,
                "https",
                "2018-03-28");
            var sasToken = storageAccountUrl + resourceName +
                    "?" +
                    "sp=r&st=" + startTimeIso + "&se=" + endTimeIso + "&spr=https" +
                    "&sv=" + version +
                    "&sig=" + Uri.EscapeDataString(Convert.ToBase64String(hmacSha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payLoad)))) +
                    "&sr=b";
            return sasToken;
        }

        /// <summary>
        /// Puts the document.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="body">The body.</param>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="serviceBusConnectionString">The service bus connection string.</param>
        /// <returns>Document.</returns>
        public HttpStatusCode PutDocument(string container, string resourceName, string body, string queueName, string serviceBusConnectionString)
        {
            string label = $"{container}/{resourceName}";
            var brokerProperty = new JObject
            {
                { "Label", label },
            };
            var sasToken = this.GenerateServiceBusSasToken(serviceBusConnectionString);
            var uri = $"https{serviceBusConnectionString.Split(';')[0].ToString().Split('=')[1].Remove(0, 2)}/{queueName}/messages";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Add("Authorization", sasToken);
            request.Headers.Add("BrokerProperties", JsonConvert.SerializeObject(brokerProperty));
            request.Content = new StringContent(body);
            using (HttpClient httpClient = new HttpClient())
            {
                var response = httpClient.SendAsync(request).Result;
                return response.StatusCode;
            }
        }

        /// <summary>
        /// Puts the document stream.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="resourceName">Name of the resource.</param>
        /// <param name="body">The body.</param>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="serviceBusConnectionString">The service bus connection string.</param>
        /// <returns>HttpStatusCode.</returns>
        public HttpStatusCode PutDocumentStream(string container, string resourceName, System.IO.Stream body, string queueName, string serviceBusConnectionString)
        {
            string label = $"{container}/{resourceName}";
            var brokerProperty = new JObject
            {
                { "Label", label },
            };
            var sasToken = this.GenerateServiceBusSasToken(serviceBusConnectionString);
            var uri = $"https{serviceBusConnectionString.Split(';')[0].ToString().Split('=')[1].Remove(0, 2)}/{queueName}/messages";
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Headers.Add("Authorization", sasToken);
            request.Headers.Add("BrokerProperties", JsonConvert.SerializeObject(brokerProperty));
            request.Content = new StreamContent(body);
            using (HttpClient httpClient = new HttpClient())
            {
                var response = httpClient.SendAsync(request).Result;
                return response.StatusCode;
            }
        }

        /// <summary>
        /// Generates the service bus sas token.
        /// </summary>
        /// <param name="serviceBusConnectionString">The service bus connection string.</param>
        /// <returns>SasToken.</returns>
        public string GenerateServiceBusSasToken(string serviceBusConnectionString)
        {
            var connectionString = serviceBusConnectionString;
            var sasKey = connectionString.Split(';')[2].Remove(0, 16);
            var sasKeyName = connectionString.Split(';')[1].Remove(0, 20);
            var servicebusnamespace = connectionString.Split(';')[0].Remove(0, 11);
            var resourceUri = "https" + servicebusnamespace + "q.planbutler/messages";

            TimeSpan sinceEpoch = DateTime.UtcNow - new DateTime(1970, 1, 1);

            var expiry = Convert.ToString((int)sinceEpoch.TotalSeconds + 3600);

            string stringToSign = HttpUtility.UrlEncode(resourceUri) + "\n" + expiry;

            HMACSHA256 hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sasKey));

            var signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));

            // format the sas token
            var sasToken = string.Format(CultureInfo.InvariantCulture, "SharedAccessSignature sr={0}&sig={1}&se={2}&skn={3}", HttpUtility.UrlEncode(resourceUri), HttpUtility.UrlEncode(signature), expiry, sasKeyName);

            return sasToken;
        }
    }
}
