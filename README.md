# AzureSolutionTemplate

[![Deploy to Azure](https://azuredeploy.net/deploybutton.svg)](https://azuredeploy.net/)

To run locally, ensure you have the latest Azure CLI installed from [here](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest). 
Run with the following command (after logging in with ```Login-AzureRmAccount``` and creating the resource group with ```New-AzureRmResourceGroup```):
```powershell
az group deployment create --name ExampleDeployment --resource-group YourResourceGroup --template-file azuredeploy.json --parameters azuredeploy.parameters.json
```
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
https://eu1.loriot.io/1/nwk/app/
```

### IOT_HUB_OWNER_CONNECTION_STRING

The connection string to the IoT Hub used for device syncing and reading the device registry.

```
HostName=something.azure-devices.net;SharedAccessKeyName=iothubowner;SharedAccessKey=fU3Kw5M5J5QXP1QsFLRVjifZ1TeNSlFEFqJ7Xa5jiqo=
```

### EVENT_HUB_ROUTER_INPUT

The connection string of the IoT Hub's Event Hub, used as trigger on the RouterFunction to send the messages to the appropriate decoders.

```
Endpoint=Endpoint=sb://something.servicebus.windows.net/;SharedAccessKeyName=iothubowner;SharedAccessKey=UDEL1prJ9THqLJel+uk8UeU8fZVkSSi2+CMrp5yrrWM=;EntityPath=iothubname;
SharedAccessKeyName=iothubowner;SharedAccessKey=2n/TlIoLJbMjmJOmadPU48G0gYfRCU28HeaL0ilkqMU=
```

### EVENT_HUB_ROUTER_OUTPUT

Connection string defining the output of the router function to the enriched and decoded message Event Hub.

```
Endpoint=sb://something.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=Ei8jNFRlH/rAjYKTTNxh7eIHlgeleffFekHhnyAxrZ4=
```

### DOCUMENT_DB_NAME

Document Database name

### DOCUMENT_DB_ACCESS_KEY

Key of the Document Database

### SQL_DB_CONNECTION

Connection String of the SQL Database

```
Server=tcp:something.database.windows.net,1433;Initial Catalog=testdbmikou;Persist Security Info=False;
User ID=username;Password=password;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

### DEVICE_LIFECYCLE_CONNECTION_STRING

### DEVICE_LIFECYCLE_QUEUE_NAME

### DEVICE_LIFECYCLE_IMPORT_TIMER
