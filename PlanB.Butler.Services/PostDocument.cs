using BotLibraryV2;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Post_Document
{
    public static class PostDocument
    {
        [Singleton]
        [FunctionName(nameof(PostDocumentOrder))]
        public static async void PostDocumentOrder([ServiceBusTrigger("q.planbutlerupdateorder", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
           [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]CloudBlockBlob blob,
           ILogger log)
        {

            // Implement Logging after MVP.
            string payload = Encoding.Default.GetString(messageHeader.Body);
            OrderBlob orderBlob = new OrderBlob();
            orderBlob.OrderList = new List<Order>();
            orderBlob = JsonConvert.DeserializeObject<OrderBlob>(payload);
            string name = string.Empty;
            DateTime date = DateTime.Now;
            foreach (var item in orderBlob.OrderList)
            {
                name = item.Name;
                date = item.Date;
                break;
            }

            var stringDate = date.ToString("yyyy-MM-dd");

            blob.Metadata.Add("user", name);
            blob.Metadata.Add("date", stringDate);
            await blob.UploadTextAsync(payload);
            await blob.SetMetadataAsync();

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
            SalaryDeduction orderBlob = new SalaryDeduction();
            orderBlob.Order = new List<Order>();
            orderBlob = JsonConvert.DeserializeObject<SalaryDeduction>(payload);
            string name = string.Empty;
            DateTime date = DateTime.Now;
            foreach (var item in orderBlob.Order)
            {
                date = item.Date;
                break;
            }
            var stringday = date.Day.ToString();
            var stringMonth = date.Month.ToString();

            blob.Metadata.Add("month", stringMonth);
            blob.Metadata.Add("day", stringday);
            await blob.UploadTextAsync(payload);
            await blob.SetMetadataAsync();
        }
        [Singleton]
        [FunctionName(nameof(PostDocumentExcel))]
        public static void PostDocumentExcel([ServiceBusTrigger("q.planbutlerupdateexcel", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader,
       [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]out byte[] payload,
       ILogger log)
        {
            // Implement Logging after MVP.
            payload = messageHeader.Body;
        }
    }
}

