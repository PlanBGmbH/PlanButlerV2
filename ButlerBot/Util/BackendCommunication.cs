using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ButlerBot.Util
{
    public class BackendCommunication
    {
        public async Task<string> GetDocument(string container, string resourceName)
        {
            using (HttpClient httpClient = new HttpClient())
            {
                string resource = container + "/" + resourceName;
                string sasToken = GenerateStorageSasToken(resource, Settings.StorageAccountUrl, Settings.StorageAccountKey);
                var response = await httpClient.GetAsync(sasToken);
                return (response.Content.ReadAsStringAsync().Result);
            }
        }

        public string GenerateStorageSasToken(string resourceName, string storageAccountUrl, string storageAccountKey)
        {
            var storageAccountName = storageAccountUrl.Remove(0, 8).Split('.')[0];
            var version = "2018-03-28";
            var startTime = DateTime.UtcNow;
            var startTimeIso = startTime.ToString("s") + "Z";
            var EndTimeIso = startTime.AddMinutes(10).ToString("s") + "Z";
            var hmacSha256 = new System.Security.Cryptography.HMACSHA256 { Key = Convert.FromBase64String(storageAccountKey) };
            var payLoad = string.Format("{0}\n{1}\n{2}\n{3}\n{4}\n{5}\n{6}\n{7}\n\n\n\n\n",
                "r",
                startTimeIso,
                EndTimeIso,
                "/blob/" + storageAccountName + "/" + resourceName,
                "",
                "",
                "https",
                "2018-03-28");
            var sasToken = storageAccountUrl + resourceName +
                    "?" +
                    "sp=r&st=" + startTimeIso + "&se=" + EndTimeIso + "&spr=https" +
                    "&sv=" + version +
                    "&sig=" + Uri.EscapeDataString(Convert.ToBase64String(hmacSha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(payLoad)))) +
                    "&sr=b";
            return sasToken;
        }

        //public async Task<string> PutDocument(string queueName, string body)
        //{
        //    var connectionString = Settings.serviceBusConnectionString;
        //    var sasToken = GenerateServiceBusSasToken(connectionString);
        //    var uri = $"https{connectionString.Split(';')[0].ToString().Split('=')[1].Remove(0, 2)}/{queueName}/messages";
        //    HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, uri);
        //    request.Headers.Add("Authorization", GenerateServiceBusSasToken(connectionString));
        //    request.Content = new StringContent(body);
        //    using (HttpClient httpClient = new HttpClient())
        //    {
        //        var response = httpClient.SendAsync(request).Result;
        //        return response.Content.ReadAsStringAsync().Result;
        //    }
        //}

        //public string GenerateServiceBusSasToken(string connectionString)
        //{
        //    var unixTimestamp = ((Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds) + 60;
        //    var connectionStringArray = connectionString.Split(';');
        //    var serviceBusNamespace = $"https{connectionStringArray[0].ToString().Split('=')[1].Remove(0, 2)}";
        //    var stringToSign = $"{Uri.EscapeDataString(serviceBusNamespace)}\n{unixTimestamp}";
        //    var serviceBusPolicy = connectionStringArray[1].ToString().Split('=')[1];
        //    var serviceBusKey = $"{connectionStringArray[2].ToString().Split('=')[1]}=";
        //    var hmacSha256 = new System.Security.Cryptography.HMACSHA256 { Key = Convert.FromBase64String(serviceBusKey) };
        //    var signature = Convert.ToBase64String(hmacSha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(stringToSign)));

        //    var sasToken = $"SharedAccessSignature sr={Uri.EscapeDataString(serviceBusNamespace)}&sig={Uri.EscapeDataString(signature)}&se={unixTimestamp}&skn={serviceBusPolicy}";
        //    return sasToken;
        //}
    }
}
