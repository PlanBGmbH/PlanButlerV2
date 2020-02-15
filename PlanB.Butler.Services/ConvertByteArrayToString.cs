using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Runtime.Serialization.Formatters.Binary;

namespace Post_Document
{
    public static class ConvertByteArrayToString
    {
        [FunctionName("ConvertByteArrayToString")]
        public static async Task<Object> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] Byte[] req,
            ILogger log)
        {            
            byte[] tmp = req;
            MemoryStream memStream = new MemoryStream();
            BinaryFormatter binForm = new BinaryFormatter();
            memStream.Write(tmp, 0, tmp.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Object obj = (Object)binForm.Deserialize(memStream);
            return obj;
        }
    }
}
