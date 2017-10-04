using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using Microsoft.Azure.Devices;
using System.Threading.Tasks;
using System.Dynamic;
using System.Runtime.Caching;

namespace Route
{
    public static class RouteFunction
    {
        /// <summary>
        /// Local cache sharing Id between methods.
        /// </summary>
        private static MemoryCache localCache = MemoryCache.Default;
        /// <summary>
        /// Caching policy of the cache, change saving time by modifying the TimeSpan object.
        /// </summary>
        private static CacheItemPolicy policy = new CacheItemPolicy()
        {
            SlidingExpiration = new TimeSpan(0, 5, 0)
        };

        private static dynamic GetPayload(byte[] body)
        {
            var json = System.Text.Encoding.UTF8.GetString(body);
            return JObject.Parse(json);
        }

        /// <summary>
        /// Get the device id from the inbound message from the IoT Hub.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static string GetDeviceId(EventData message)
        {
            return message.SystemProperties["iothub-connection-device-id"].ToString();
        }

        /// <summary>
        /// Method contacting the Devices twins from the IoT Hub to get the medata and pass it in the payload.
        /// </summary>
        /// <param name="connectionString">Connection string of the IoT Hub</param>
        /// <param name="id">metadata Device Id</param>
        /// <returns></returns>
        public async static Task<dynamic> GetTags(string connectionString, string id)
        {
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(connectionString);
            var twin = await registryManager.GetTwinAsync(id);
            var result = JsonConvert.DeserializeObject(twin.Tags.ToJson());
            return result;
        }

        /// <summary>
        /// message describing the output of the function.
        /// </summary>
        public class ReturnMessage
        {
            public Guid messageGuid;
            public dynamic raw;
            public dynamic metadata;
            public dynamic decoded;
            public ReturnMessage()
            {
                messageGuid = Guid.NewGuid();
            }
        }

        [FunctionName("RouteFunction")]
        [return: EventHub("outputEventHubMessage", Connection = "OutputRouterEventHub")]
        public async static Task<string> Run([EventHubTrigger("iotHub", Connection = "IotHubConnection", ConsumerGroup = "router")]EventData myEventHubMessage,
              TraceWriter log)
        {
            //section to build up the metadata section
            var deviceId = GetDeviceId(myEventHubMessage);
            dynamic metadataMessageSection;
            if (localCache.Contains(deviceId))
            {
                metadataMessageSection = localCache[deviceId];
            }
            else
            {
                metadataMessageSection = await GetTags(System.Environment.GetEnvironmentVariable("DeviceTwinsConnection"), deviceId);
                localCache.Add(deviceId, metadataMessageSection, policy);
            }

            //section to build up the raw section
            var rawMessageSection = GetPayload(myEventHubMessage.GetBytes());

            //routing
            //Case 1 global specific function
            string functionUrl = System.Environment.GetEnvironmentVariable(String.Concat("DECODER_URL_", metadataMessageSection.sensorDecoder));
            if (String.IsNullOrEmpty(functionUrl))
            {
                //case 2 global default function
                functionUrl = System.Environment.GetEnvironmentVariable(String.Concat("DECODER_URL_DEFAULT_", metadataMessageSection.sensorDecoder));
                if (String.IsNullOrEmpty(functionUrl))
                {
                    //case 3 local function
                    functionUrl = String.Format("https://{0}.azurewebsites.net/api/{1}",
                        System.Environment.GetEnvironmentVariable("WEBSITE_CONTENTSHARE"),
                        System.Environment.GetEnvironmentVariable("SensorDecoder"));
                }
            }

            //Section to build up the decoded section
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(functionUrl);
            req.Method = "POST";
            req.ContentType = "application/json";
            Stream stream = req.GetRequestStream();
            dynamic payload = new ExpandoObject();
            payload.data = rawMessageSection.data;
            string json = JsonConvert.SerializeObject(payload);
            byte[] buffer = Encoding.UTF8.GetBytes(json);
            stream.Write(buffer, 0, buffer.Length);
            string decodedSection = "";
            try
            {
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                using (var sr = new StreamReader(res.GetResponseStream()))
                {
                    decodedSection = sr.ReadToEnd();
                }
            }
            catch (System.Net.WebException exception)
            {
                decodedSection = JsonConvert.SerializeObject(new Dictionary<string, string>() { { "error", "The decoder method was not found" } });
            }

            //build the message outputed to the output eventHub
            ReturnMessage returnMessage = new ReturnMessage();
            returnMessage.decoded = JsonConvert.DeserializeObject(decodedSection);
            returnMessage.raw = rawMessageSection;
            returnMessage.metadata = metadataMessageSection;

            string returnString = JsonConvert.SerializeObject(returnMessage);
            log.Info(returnString);
            return returnString;
        }
    }
}
