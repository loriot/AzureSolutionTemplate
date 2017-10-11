using System.Net;
using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.Devices;
using System.Collections.Generic;

namespace LoriotAzureFunctions.DeviceProvisioning
{
    public static class ImportDevice
    {
        [FunctionName("ImportDevice")]
        public static async Task<object> Run([HttpTrigger]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("Import started from http trigger");
            long importedItemCount = 0;
            try
            {
                importedItemCount = await IotHubClient.ImportDevice(log);
            }
            catch(HttpRequestException httpRequestEx)
            {
                return req.CreateResponse(HttpStatusCode.InternalServerError, new
                {
                    error = "Internal Server Error"
                });
            }

            return req.CreateResponse(HttpStatusCode.OK, new
            {
                message = $"Imported {importedItemCount} new devices to Azure Iot Hub"
            });
        }
    }
}
