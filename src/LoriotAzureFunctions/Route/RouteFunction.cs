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
using System.Collections.Concurrent;

namespace LoriotAzureFunctions.Route
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

        /// <summary>
        /// random generator
        /// </summary>
        private static Random random = new Random();

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
        public async static Task Run([EventHubTrigger("%IOT_HUB_NAME%", Connection = "EVENT_HUB_ROUTER_INPUT", ConsumerGroup = "router")]EventData[] myEventHubMessageInput,
            [EventHub("outputEventHubMessage", Connection = "EVENT_HUB_ROUTER_OUTPUT")]IAsyncCollector<String> output,
              TraceWriter log)
        {
            foreach (var myEventHubMessage in myEventHubMessageInput)
            {
                //section to build up the metadata section
                var deviceId = GetDeviceId(myEventHubMessage);
                dynamic metadataMessageSection;
                //retry logic to avoid the initial message rush to be declined by the IoT hub.
                int retryCount = 0;
                string sensorDecoder = null;
                while (true)
                {
                    try
                    {
                        if (localCache.Contains(deviceId))
                        {
                            metadataMessageSection = localCache[deviceId];
                            sensorDecoder = ((Newtonsoft.Json.Linq.JObject)metadataMessageSection)["sensorDecoder"]?.ToString() ?? string.Empty;
                        }
                        else
                        {
                            metadataMessageSection = await GetTags(System.Environment.GetEnvironmentVariable("IOT_HUB_OWNER_CONNECTION_STRING"), deviceId);
                            sensorDecoder = ((Newtonsoft.Json.Linq.JObject)metadataMessageSection)["sensorDecoder"]?.ToString() ?? string.Empty;
                            if (!string.IsNullOrEmpty(sensorDecoder))
                                localCache.Add(deviceId, metadataMessageSection, policy);
                        }
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount++;
                        if (retryCount > 5)
                            throw new Exception("Could not connect with the IoT Hub device manager", ex);
                        await Task.Delay(random.Next(1000,2000));
                    }
                }

                //section to build up the raw section
                var rawMessageSection = GetPayload(myEventHubMessage.GetBytes());

                //routing
                //Case 1 route to a global specific function
                var decodedMessageContents = new Dictionary<string, string>();
                string decodedSection = null;
                if (string.IsNullOrEmpty(sensorDecoder))
                {
                    decodedMessageContents.Add("error", "Could not resolve decoder");
                    decodedMessageContents.Add("details", $"Verify that the device twin has been properly configured for deviceId {deviceId} ");
                }
                else
                {
                    string functionUrl = System.Environment.GetEnvironmentVariable(String.Concat("DECODER_URL_", sensorDecoder));
                    if (String.IsNullOrEmpty(functionUrl))
                    {
                        //case 2 route to a global default function
                        functionUrl = System.Environment.GetEnvironmentVariable(String.Concat("DECODER_URL_DEFAULT_", sensorDecoder));
                        if (String.IsNullOrEmpty(functionUrl))
                        {
                            //case 3 route to the default function
                            functionUrl = String.Format("https://{0}.azurewebsites.net/api/{1}",
                                System.Environment.GetEnvironmentVariable("WEBSITE_CONTENTSHARE"),
                                metadataMessageSection.sensorDecoder);
                        }
                    }

                    //Section to build up the decoded section
                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(functionUrl);
                    req.Method = "POST";
                    req.ContentType = "application/json";
                    Stream stream = await req.GetRequestStreamAsync();

                    string json = JsonConvert.SerializeObject(rawMessageSection);
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    await stream.WriteAsync(buffer, 0, buffer.Length);
                    try
                    {
                        var res = await req.GetResponseAsync();
                        using (var sr = new StreamReader(res.GetResponseStream()))
                        {
                            decodedSection = await sr.ReadToEndAsync();
                        }
                    }
                    catch (System.Net.WebException exception)
                    {
                        decodedMessageContents.Add("error", "The decoder method was not found");
                        decodedMessageContents.Add("details", exception.Message);
                        decodedMessageContents.Add(nameof(functionUrl), functionUrl);
                        decodedMessageContents.Add(nameof(sensorDecoder), sensorDecoder);
                    }
                }                

                //build the message outputed to the output eventHub
                ReturnMessage returnMessage = new ReturnMessage();
                if (decodedMessageContents.Count > 0)
                    returnMessage.decoded = decodedMessageContents;
                else if (!string.IsNullOrEmpty(decodedSection))
                    returnMessage.decoded = JsonConvert.DeserializeObject(decodedSection);
                returnMessage.raw = rawMessageSection;
                returnMessage.metadata = metadataMessageSection;

                string returnString = JsonConvert.SerializeObject(returnMessage);
                log.Info(returnString);
                await output.AddAsync(returnString);
            }
            await output.FlushAsync();
        }
    }
}
