using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using BotLibraryV2;


namespace Post_Document
{
    public static class GetSalaryDeduction
    {
        [FunctionName("GetSalaryDeduction")]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var connectionString = ButlerBot.Util.Settings.StorageAccountConnectionString;
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference("salarydeduction");
            BlobContinuationToken token = new BlobContinuationToken();
            var context = new OperationContext();
            var options = new BlobRequestOptions();
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var cloudBlobContainer = cloudBlobClient.GetContainerReference("salarydeduction");
            BlobContinuationToken blobContinuationToken = null;
            List<SalaryDeduction> orderBlob = new List<SalaryDeduction>();
            var blobs = await container.ListBlobsSegmentedAsync(null, true, BlobListingDetails.All, null, blobContinuationToken, options, context).ConfigureAwait(false);
            Microsoft.Extensions.Primitives.StringValues month ;
            req.Headers.TryGetValue("user", out month);
            string stringMonth = Convert.ToString(month);
            foreach (var item in blobs.Results)
            {
                CloudBlockBlob blob = (CloudBlockBlob)item;
                await blob.FetchAttributesAsync();
                DateTime date = DateTime.Now;

                if (blob.Metadata.Contains(new KeyValuePair<string, string>("month", stringMonth)))
                {
                    Order order = new Order();
                    await blob.FetchAttributesAsync();
                    var blobDownload = blob.DownloadTextAsync();
                    var blobData = blobDownload.Result;
                    orderBlob.Add(JsonConvert.DeserializeObject<SalaryDeduction>(blobData));
                }
            }

            return JsonConvert.SerializeObject(orderBlob);
        }
    }
}
