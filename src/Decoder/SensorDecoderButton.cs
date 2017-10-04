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

namespace Decoder
{
    public static class SensorDecoderButton
    {
        [FunctionName("SensorDecoderButton")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // Get request body
            string body = await req.Content.ReadAsStringAsync();
            dynamic payload = JObject.Parse(body);

            Dictionary<string, string> returnMessage = new Dictionary<string, string>();
            try
            {
                int i = payload.data;
                if (i == 1)
                {
                    returnMessage.Add("button", "1");
                }
                else
                    returnMessage.Add("error", "sensor input was not with an expected value");
            }
            catch (InvalidCastException invalidCastException)
            {
                returnMessage.Add("error", "sensor input was not in the expected format");
            }
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(returnMessage), Encoding.UTF8, "application/json")
            };
        }
    }
}
