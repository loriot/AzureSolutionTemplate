# AzureSolutionTemplate

[![Deploy to Azure](https://azuredeploy.net/deploybutton.svg)](https://azuredeploy.net/)

## Environment variables

### LORIOT_APP_ID

The LORIOT App Id used to identify under which app the devices are synced.

```
BA7B0CF5
```

### LORIOT_APP_TOKEN

Token used to authenticate requests towards LORIOT servers.

```
afvM9AAAAA1ldTEubG9yaW90Lmlvm-GeKaLbzA4zn_ZA95Iv1w==
```

### LORIOT_API_URL

The base URL of the Network Server Management API used to sync device information between Azure IoT Hub and LORIOT servers.

```
https://eu1.loriot.io/1/nwk/
```

### IOT_HUB_OWNER_CONNECTION_STRING

The connection string to the IoT Hub used for device syncing and reading the device registry.

```
HostName=something.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=fU3Kw5M5J5QXP1QsFLRVjifZ1TeNSlFEFqJ7Xa5jiqo=
```
