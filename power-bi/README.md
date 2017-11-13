# Power BI Setup Instructions

## Contents

[Introduction](#introduction)

[Realtime Data using Azure Stream Analytics (Power BI Service)](#realtime-data-using-azure-stream-analytics-power-bi-service)

[Historical Analysis using Cosmos DB (Power BI Desktop)](#historical-analysis-using-cosmos-db-power-bi-desktop)

## Introduction

[Power BI](https://powerbi.microsoft.com/en-us/) is Microsoft's interactive data visualisation tool. This template provides two methods of visualising data from sensor devices - realtime analysis using Stream Analytics (requires a Power BI Free subscription or higher), and historical analysis of data stored in Cosmos DB (no subscription required unless you wish to publish/share the dashboard).

These reports are provided as a starting point for specialised analysis and do not contain any sensor-specific visualisations.

## Realtime Data using Azure Stream Analytics (Power BI Service)

If you have deployed the template using the 'Deploy to Azure' button, click on the 'Manage your resources' link once all resources have successfully deployed:

![Manage Resources](Images/ManageResources.PNG)

If you deployed from the command line, open up a browser and navigate to your newly created resource group in the [Azure Portal](https://portal.azure.com).

Once in the resource group blade, select the Stream Analytics Job:

![Resource Group](Images/ResourceGroup.PNG)

Then select the Power BI output:

![Stream Analytics Blade](Images/StreamAnalytics1.png)

Click on 'Renew authorization' to sign in:

![Renew Authorisation](Images/RenewAuth.png)

Once this has completed, choose the Power BI Group Workspace you wish to use for the dashboard from the dropdown at the top of the blade. This must have been created previously in the [Power BI portal](https://powerbi.microsoft.com/). Once you are happy with the workspace/dataset details, Click 'Save'.

Now click 'Start' to start the job:

![Start Job](Images/StreamAnalytics2.PNG)

Once this is complete, a pale blue banner with the text 'Running' will appear at the top of the Stream Analytics blade where it previously read 'Created'.

Now the job is running. You won’t see the dataset in the portal until it has started receiving data from Stream Analytics, at which point it will appear under the ‘Datasets’ menu in the [Power BI portal](https://powerbi.microsoft.com/) as shown below:

![New Dataset](Images/PowerBIStreamingDataset.PNG)

To create visuals using this streaming data, do the following:

1. Select a dashboard from the Dashboards menu on the left (or create a new one).

2. Add a tile from the menu at the top of the page:

![Add Tile](Images/AddTile.PNG)

3. Select 'Custom Streaming Data' as the source:

![Select Source](Images/ConfigureTile1.PNG)

4. Select your dataset (created by Stream Analytics):

![Select Dataset](Images/ConfigureTile2.PNG)

5. Customise tile as required, click 'Apply' once satisfied (this can be edited later):

![Customise Tile](Images/ConfigureTile3.PNG)

6. The final result will look something like the below. If there is no data being streamed to the tile currently it will appear as shown on the left. Otherwise, data will be displayed in near realtime as it is pushed from the Stream Analytics job (as seen on the right):

![Finished Tile](Images/TileDemo.PNG)

## Historical Analysis using Cosmos DB (Power BI Desktop)

This section requires Power BI Desktop to be installed. This is available from Microsoft for free [here](https://powerbi.microsoft.com/en-us/desktop/).

Once installed, open up the Power BI Template [IoTDashboard.pbit](IoTDashboard.pbit) provided with this repo.

You will be prompted to enter your Inactive Devices API URL. This value can be found in the Azure Portal by navigating to the Function App deployed previously, selecting the 'GetInactiveDevices' Function and clicking on '</> Get function URL' in the top right corner as shown:

![Get Function Details](Images/GetFunctionDetails.PNG)

![Get Function URL](Images/GetFunctionURL.PNG)

Copy this value into the prompt and append '&minutes=60' to the end of the URL - replace 60 with your preferred period of time before a device is considered 'inactive'. The result should look like the following:

```
https://iotdeploymenttestfunction.azurewebsites.net/api/GetInactiveDevices?code=<yourkey>&minutes=60
```

![Enter Inactive Devices API URL](Images/APIURL.png)

Click 'Load' to pull data from the API. At this point it will attempt to load data from the two sources specified in the template (Cosmos DB and the Inactive Devices API) - this will fail because we have not yet set up the Cosmos DB connection. Click 'Cancel' on the popup window to skip loading the data until the connection is configured:

![Cancel Data Load](Images/CancelLoad.PNG)

Alternatively, wait for the load to fail and then click 'Close'. If prompted for a Cosmos DB account key, select 'Cancel': 

![Cancel Cosmos Authentication](Images/CancelCosmosAuth.PNG)

![Data Load Failed](Images/LoadFailed.PNG)

Next, open Query Editor from the Home tab in the ribbon:

![Open Query Editor](Images/OpenQueryEditor.PNG)

You will see the following screen:

![Query Editor](Images/QueryEditor1.png)

Scroll to the top of the 'Applied Steps' pane until you see 'Source'. Click on the cog symbol to its right:

![Open Source Settings](Images/OpenSource.PNG)

Edit the source settings to reflect the URL for your newly created Cosmos DB and click 'OK' (Cosmos DB will be initialised for you by the deployment script with default database 'db' and collection 'sensordatacollection', which have been auto-populated for you):

![Edit Source Settings](Images/EditSource.png)

Enter the primary key for the Cosmos DB when prompted. This can be found in the Azure portal:

![Get Cosmos Details](Images/CosmosDetails.PNG)

![Enter Cosmos DB Key](Images/CosmosKey.png)

Repeat this process for the 'Device Data (Latest Readings Only)' query, found in the Queries pane on the left:

![Device Data (Latest Readings Only)](Images/RepeatProcess.PNG)

>**NOTE: This is a temporary workaround to be used until the Cosmos DB Power BI connector is out of preview and parameters are enabled for this connection type (after which the experience will be similar to that for the Inactive Devices API).**

Click ‘Close and Apply’ in the top left of the Query Editor window. You will then be prompted for authentication for the inactive devices API – choose Anonymous and click 'Connect'.

![API Authentication](Images/APIAuth.png)

Now that all the connections are correctly set up and authenticated, the visualisations should populate with your data and look similar to the below. If they don't, manally refresh the data by clicking on the refresh button in the 'Home' tab of the ribbon.

![Final Result](Images/FinalResult.png)

To publish this report to the Power BI Service, click the 'Publish' button at the right of the 'Home' tab in the ribbon. Choose your preferred destination Group Workspace and click 'Select' to deploy to the [Power BI portal](https://powerbi.microsoft.com):

![Publish Report](Images/Publish.PNG)

It will then appear under the 'Reports' menu in the portal:

![Publish Report](Images/ReportsPortal.PNG)