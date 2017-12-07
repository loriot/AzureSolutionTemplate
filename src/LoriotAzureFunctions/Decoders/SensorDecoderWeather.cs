using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Dynamic;

namespace LoriotAzureFunctions.Decoders
{
    public static class SensorDecoderWeather
    {
        [FunctionName("SensorDecoderWeather")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // Get request body
            string body = await req.Content.ReadAsStringAsync();
            dynamic rawMessageSection = JObject.Parse(body);

            string sensorData = rawMessageSection.data;

            byte[] raw = new byte[sensorData.Length / 2];
            for (int i = 0; i < raw.Length; i++)
            {
                raw[i] = Convert.ToByte(sensorData.Substring(i * 2, 2), 16);
            }

            string encodedData = Encoding.ASCII.GetString(raw);

            var split = encodedData.Split(':');
            dynamic data = new ExpandoObject();            
            data.temperature = Decimal.Parse(split[0]);
            data.humidity = Decimal.Parse(split[1]);

            var jsonValue = JsonConvert.SerializeObject(data);
            log.Info(string.Concat("Response: ", jsonValue));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonValue, Encoding.UTF8, "application/json")
            };
        }
    }
}
