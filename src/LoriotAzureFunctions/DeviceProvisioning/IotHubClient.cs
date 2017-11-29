using Microsoft.Azure.Devices;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LoriotAzureFunctions.DeviceProvisioning
{
    public static class IotHubClient
    {
        public static async Task<long> ImportDevice(TraceWriter log)
        {
            log.Info("Getting starting import");
            long importedItemCount = 0;
            try
            {
                importedItemCount = await ImportDevices(log, importedItemCount);
            }
            catch (HttpRequestException httpRequestEx)
            {
                log.Error("Import failed", httpRequestEx);
                throw;
            }

            return importedItemCount;
        }

        private static async Task<long> ImportDevices(TraceWriter log, long importedItemCount, int page = 1)
        {
            var devices = await LoriotClient.ListDevices(log, page);

            log.Info("Getting existing azure iot devices");
            var registryManager = RegistryManager.CreateFromConnectionString(System.Environment.GetEnvironmentVariable("IOT_HUB_OWNER_CONNECTION_STRING"));
            int currentPage = devices.page;
            

            if (devices != null )
            {
                JArray devicesList = devices.devices;
                if (devicesList != null && devicesList.Count > 0)
                {
                    foreach (var device in devices.devices)
                    {
                        string eui = device._id;
                        Device azureDevice = await registryManager.GetDeviceAsync(eui);
                        if (azureDevice == null)
                        {
                            log.Info($"Device not found in azure iot hub: {eui}");
                            Device createdDevice = await registryManager.AddDeviceAsync(new Device(eui));
                            importedItemCount++;
                            log.Info($"Created a new device: {eui}");
                        }
                        else
                        {
                            log.Info($"Device found in azure iot hub: {eui}");
                        }
                    }
                    importedItemCount = await ImportDevices(log, importedItemCount, currentPage + 1);
                }
            }

            return importedItemCount;
        }
    }
}
