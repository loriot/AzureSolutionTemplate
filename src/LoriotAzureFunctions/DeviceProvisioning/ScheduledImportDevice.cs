using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using System.Net.Http;
using System.Threading.Tasks;

namespace LoriotAzureFunctions.DeviceProvisioning
{
    public static class ScheduledImportDevice
    {
        [FunctionName("ScheduledImportDevice")]
        public static async Task Run([TimerTrigger("%DEVICE_LIFECYCLE_IMPORT_TIMER%")]TimerInfo timer, TraceWriter log)
        {            
            log.Info($"Import started at {DateTime.Now}");
            long importedItemCount = 0;
            try
            {
                importedItemCount = await IotHubClient.ImportDevice(log);
            }
            catch (HttpRequestException httpRequestEx)
            {
                
            }

            log.Info($"Imported {importedItemCount} new devices to Azure Iot Hub at {DateTime.Now}");
        }
    }
}
