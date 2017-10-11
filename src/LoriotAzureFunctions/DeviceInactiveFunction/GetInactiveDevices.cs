using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Devices;
using System;

namespace LoriotAzureFunctions.DeviceInactiveFunction
{
    public static class GetInactiveDevices
    {

        static RegistryManager registryManager;
      
        [FunctionName("GetInactiveDevices")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
        
            log.Info("Get Inactive Device function called");

            // parse query parameter
            string minutesValue = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "minutes", true) == 0)
                .Value;

            double minutes;

            if (double.TryParse(minutesValue, out minutes))
            {

                var timeWindow = DateTime.Now.AddMinutes(-minutes);

                registryManager = RegistryManager.CreateFromConnectionString(System.Environment.GetEnvironmentVariable("IOT_HUB_OWNER_CONNECTION_STRING"));

                //Currently IoTHub allows only to get 1000 devices. There is no way currently to send a query for the device identity. Would be nice to have the lastactivitytime on the device twins
                var devices = await registryManager.GetDevicesAsync(int.MaxValue);

                var inactiveDevices = devices.Where(d => d.LastActivityTime < timeWindow).Select(d => new
                {
                    deviceId = d.Id,
                    status = d.Status,
                    statusUpdatedTime = d.StatusUpdatedTime,
                    lastActivityTime = d.LastActivityTime,
                    cloudToDeviceMessageCount = d.CloudToDeviceMessageCount
                });


                return req.CreateResponse(HttpStatusCode.OK, inactiveDevices);

            }
            else
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }

        }
    }
}
