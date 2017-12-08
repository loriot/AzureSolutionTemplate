# Lora IoT Hub testing tool

This tool allows you to simulate a lorawan based device sending temperature and humidity without any lorawan hw and gateway. 

## Get Started

Please ensure you have added a device to IoTHub and you have added the correct device twins data.

By default the testing tool is using this device id: "BE7A00000000999F" that can be overridden  by the -d parameter

The default device twins data is the following:

```json
 "tags": {
    "sensorDecoder": "SensorDecoderWeather",
    "sensorName": "Weather Shield",
    "location": "60.1098678,24.7385115"
  },
```


![Device Twin - Add Tags](../images/DeviceTwinAddTags.png)


## Usage

This command will send 1 message with the device id BE7A00000000999F with a random temperature and humidity. You can copy the connection string from the Azure IoThub portal under Settings -> Shared access policies -> device

```cmd
dotnet LoraIoTHubTestTool.dll -c "HostName=xxxxxxxxx.azure-devices.net;SharedAccessKeyName=device;SharedAccessKey=XXXXXXXXXXXXXXXXXXXXXXXXXXXX"
```

Here the list of all the parameters:

```cmd
Usage: LoraTestTool [options]

Options:
  -?|-h|--help                   Show help information
  -c |--con<connectionString>    Mandatory IoTHub Connection string
  -d |--device<deviceId>         Optional deviceid loraWan EUI (string)
  -m |--mcount<messagecount>     Optional no of message to send (default 1)
  -s |--s<seconds>               Optional delay between msg in seconds (default 10)
  -tmin |--tmin<mintemperature>  Optional minimum random temperature (double)
  -tmax |--tmax<maxtemperature>  Optional maximum random temperature (double)
  -hmin |--hmin<minhumidity>     Optional minimum random humidity (double)
  -hmax |--hmax<maxhumidity>     Optional maximum random humidity (double)
```





