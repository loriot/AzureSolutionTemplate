using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;

class AzureIoTHub
{
    private static void CreateClient(string deviceConnectionString)
    {
        if (deviceClient == null)
        {
            try
            {
                // create Azure IoT Hub client from embedded connection string
                deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Http1);    
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to connect to IoT Hub using device connection string {deviceConnectionString}", ex);
            }

        }
    }

    static DeviceClient deviceClient = null;

    //
    // Note: this connection string is specific to the device "testdevice". To configure other devices,
    // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
    //
    


    //
    // To monitor messages sent to device "kraaa" use iothub-explorer as follows:
    //    iothub-explorer monitor-events --login HostName=LoraTesthub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=cukOJpH0vUwz+Dx7PTDg31HjmEpZLCvVsME2SKoy3Qg= "testdevice"
    //

    // Refer to http://aka.ms/azure-iot-hub-vs-cs-2017-wiki for more information on Connected Service for Azure IoT Hub

    public static async Task SendDeviceToCloudMessageAsync(string deviceConnectionString, string msg)
    {
        CreateClient(deviceConnectionString);

        var message = new Message(Encoding.ASCII.GetBytes(msg));

        await deviceClient.SendEventAsync(message);
    }

  
}
