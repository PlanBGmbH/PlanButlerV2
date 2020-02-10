using BotLibraryV2;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;

namespace Post_Document
{
    public static class Function1
    {
        [Singleton]
        [FunctionName(nameof(PostDocument))]
        public static async void PostDocument([ServiceBusTrigger("q.planbutler", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
            [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
            ILogger log)
        {
            // Implement Logging after MVP.

            string payload = Encoding.Default.GetString(messageHeader.Body);
            await blob.UploadTextAsync(payload);
        }

        [Singleton]
        [FunctionName(nameof(PostDocumentOrder))]
        public static async void PostDocumentOrder([ServiceBusTrigger("q.planbutlerupdateorder", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
           [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
           ILogger log)
        {
            // Implement Logging after MVP.
            string payload = Encoding.Default.GetString(messageHeader.Body);
            var orderList = JsonConvert.DeserializeObject<OrderBlob>(payload);
            string name = string.Empty;
            DateTime date = DateTime.Now;
            foreach (var item in orderList.OrderList)
            {
                name = item.Name;
                date = item.Date;
                break;
            }

            var stringDate = date.ToString("yyyy-MM-dd");
            blob.Metadata.Add("user", name);
            blob.Metadata.Add("date", stringDate);
            await blob.SetMetadataAsync();
            await blob.UploadTextAsync(payload);
        }
        [Singleton]
        [FunctionName(nameof(PostDocumentMoney))]
        public static async void PostDocumentMoney([ServiceBusTrigger("q.planbutlerupdatemoney", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
       [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
       ILogger log)
        {
            // Implement Logging after MVP.

            string payload = Encoding.Default.GetString(messageHeader.Body);
            await blob.UploadTextAsync(payload);
        }
        [Singleton]
        [FunctionName(nameof(PostDocumentSalary))]
        public static async void PostDocumentSalary([ServiceBusTrigger("q.planbutlerupdatesalary", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
       [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
       ILogger log)
        {
            // Implement Logging after MVP.

            string payload = Encoding.Default.GetString(messageHeader.Body);
            await blob.UploadTextAsync(payload);
        }
        [Singleton]
        [FunctionName(nameof(PostDocumentExcel))]
        public static async void PostDocumentExcel([ServiceBusTrigger("q.planbutlerupdateexcel", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
       [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
       ILogger log)
        {
            // Implement Logging after MVP.

            string payload = Encoding.Default.GetString(messageHeader.Body);
            await blob.UploadTextAsync(payload);
        }
    }
}

