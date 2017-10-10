using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.ServiceBus.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Dynamic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace LoriotAzureFunctions.DeviceProvisioning
{
    public static class ExportDevice
    {
        //Info: Use "%MyString% to read environment variables https://github.com/Azure/azure-webjobs-sdk/issues/1221"
        [FunctionName("ExportDevice")]
        public static async Task Run([ServiceBusTrigger("%DEVICE_LIFECYCLE_QUEUE_NAME%", AccessRights.Listen, Connection = "DEVICE_LIFECYCLE_CONNECTION_STRING")]BrokeredMessage message, TraceWriter log)
        {           
            log.Info(String.Format("Starting to create/remove device in Loriot"));
            Stream stream = message.GetBody<Stream>();
            StreamReader reader = new StreamReader(stream);
            dynamic queueItem = JObject.Parse(reader.ReadToEnd());

            try
            {
                if (message.Properties["opType"].Equals("createDeviceIdentity"))
                {
                    log.Info(String.Format("Register device {0}", queueItem.deviceId));
                    var results = await LoriotClient.RegisterNewDevice(queueItem, log);
                }
                else if (message.Properties["opType"].Equals("deleteDeviceIdentity"))
                {
                    log.Info(String.Format("Remove device {0}", queueItem.deviceId));
                    var results = await LoriotClient.DeleteDevice(queueItem, log);
                }

                log.Info(String.Format("Action completed"));
                await message.CompleteAsync();
            }
            catch (HttpRequestException httpRequestEx)
            {
                message.Abandon();
                log.Error(httpRequestEx.Message, httpRequestEx);
                throw;
            }
        }

    }
}
