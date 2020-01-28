using System;
using System.IO;
using System.Text;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace Post_Document
{
    public static class Function1
    {
        [Singleton]
        [FunctionName(nameof(PostDocument))]
        public static void PostDocument([ServiceBusTrigger("q.planbutler", Connection = "butlerSend")]Microsoft.Azure.ServiceBus.Message messageHeader, 
            [Blob("{Label}", FileAccess.ReadWrite, Connection = "StorageSend")]out string payload,
            ILogger log)
        {
            // Implement Logging after MVP.
            payload = Encoding.Default.GetString(messageHeader.Body);
        }
    }
}
