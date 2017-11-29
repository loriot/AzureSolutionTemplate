using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Dynamic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LoriotAzureFunctions.DeviceProvisioning
{
    public class LoriotClient
    {
        public static async Task<dynamic> ListDevices(TraceWriter log, int page = 1)
        {
            log.Info("Starting getting devices from Loriot");
            using (var client = new HttpClient())
            {
                dynamic returnValue = null;
                try
                {


                    string url = $"{SetupApiCall(client, log)}/devices?page={page}";
                    var result = await client.GetAsync(url);
                    string resultContent = await result.Content.ReadAsStringAsync();
                    if (!result.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException(result.ReasonPhrase);
                    }
                    returnValue = JObject.Parse(resultContent);
                }
                catch (ConfigurationException confEx)
                {
                    
                }
                return returnValue;

            }
        }

        public static async Task<string> RegisterNewDevice(dynamic queueItem, TraceWriter log)
        {
            using (var client = new HttpClient())
            {

                string resultContent = null;
                try
                {
                    string url = $"{SetupApiCall(client, log)}/devices";
                    dynamic item = new ExpandoObject();
                    item.deveui = queueItem.deviceId;
                    StringContent httpConent = new StringContent(JsonConvert.SerializeObject(item), Encoding.UTF8, "application/json");
                    var result = await client.PostAsync(url, httpConent);
                    resultContent = await result.Content.ReadAsStringAsync();

                    if (!result.IsSuccessStatusCode)
                    {
                        //TODO: at the moment Loriot doesn't send htis status if the sensor just exists
                        if (result.StatusCode == HttpStatusCode.Conflict)
                        {
                            log.Warning(String.Format("Sensor just exists in Loriot"));
                        }
                        else
                        {
                            //HACK: check if is duplicate record error
                            if (!resultContent.Contains("E11000 duplicate key error"))
                            {
                                throw new HttpRequestException(result.ReasonPhrase);
                            }
                        }
                    }
                }
                catch (ConfigurationException confEx)
                {

                }
                return resultContent;
            }
        }

        public static async Task<string> DeleteDevice(dynamic queueItem, TraceWriter log)
        {
            using (var client = new HttpClient())
            {
                string resultContent = null;
                try
                {
                    string url = $"{SetupApiCall(client, log)}/device/{queueItem.deviceId}";

                    var result = await client.DeleteAsync(url);
                    resultContent = await result.Content.ReadAsStringAsync();

                    if (!result.IsSuccessStatusCode)
                    {
                        //Currently Loriot send MethodNotAllowed if the device is not found, added Notfound for future improvements.
                        if (result.StatusCode == HttpStatusCode.MethodNotAllowed ||
                            result.StatusCode == HttpStatusCode.NotFound)
                        {
                            log.Warning(String.Format("Sensor not exists in Loriot"));
                        }
                        else
                        {
                            throw new HttpRequestException(result.ReasonPhrase);
                        }
                    }
                }
                catch (ConfigurationException confEx)
                {

                }

                return resultContent;
            }
        }

        private static string SetupApiCall(HttpClient client, TraceWriter log)
        {
            string apiKey = System.Environment.GetEnvironmentVariable("LORIOT_API_KEY");
            string appId = System.Environment.GetEnvironmentVariable("LORIOT_APP_ID");
            string baseUrl = System.Environment.GetEnvironmentVariable("LORIOT_API_URL");

            if (String.IsNullOrEmpty(apiKey) || String.IsNullOrEmpty(appId) || String.IsNullOrEmpty(baseUrl))
            {
                log.Warning("Loriot Appsettings missing. Synchronization to and from Loriot will be skipped");
                throw new ConfigurationErrorsException();
            }

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            return baseUrl + appId;
        }
    }
}
