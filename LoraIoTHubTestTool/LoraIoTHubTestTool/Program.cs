using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Text;
using System.Threading;

namespace LoraIoTHubTestTool
{
    class Program
    {
        static void Main(string[] args)
        {


            Random rnd = new Random();

            string connectionString = "";
            string deviceId = "BE7A00000000999F";

            int messageCount = 1;
            int delayInSeconds = 10;

            double tminValue = 20;
            double tmaxValue = 40;

            double hminValue = 40;
            double hmaxValue = 90;


            var app = new CommandLineApplication();
            app.Name = "LoraTestTool";
            app.Description = "Lora IoT Hub device emulator.";

            app.HelpOption("-?|-h|--help");

            var conString = app.Option("-c |--con<connectionString>",
                                       "Mandatory IoTHub Connection string (HostName=xxxx.azure-devices.net;SharedAccessKeyName=device;SharedAccessKey=xxxx)",
                                       CommandOptionType.SingleValue);

            var did = app.Option("-d |--device<deviceId>",
                                 "Optional deviceid loraWan EUI (string)",
                                 CommandOptionType.SingleValue);

            var m = app.Option("-m |--mcount<messagecount>",
                               "Optional no of message to send (default 1)",
                               CommandOptionType.SingleValue);

            var s = app.Option("-s |--s<seconds>",
                               "Optional delay between msg in seconds (default 10)",
                               CommandOptionType.SingleValue);

            var tmin = app.Option("-tmin |--tmin<mintemperature>",
                                  "Optional minimum random temperature (double)",
                                  CommandOptionType.SingleValue);

            var tmax = app.Option("-tmax |--tmax<maxtemperature>",
                                  $"Optional maximum random temperature (double). Default: between {tminValue} and {tmaxValue}",
                                  CommandOptionType.SingleValue);

            var hmin = app.Option("-hmin |--hmin<minhumidity>",
                                  "Optional minimum random humidity (double)",
                                  CommandOptionType.SingleValue);

            var hmax = app.Option("-hmax |--hmax<maxhumidity>",
                                  $"Optional maximum random humidity (double). Default: between {hminValue} and {hmaxValue}.",
                                  CommandOptionType.SingleValue);


            app.OnExecute( async () => 
            {
                if (conString.HasValue())
                {
                    connectionString = conString.Value();
                }
                else
                {
                    app.ShowHelp();
                    return 1;
                }

                if (did.HasValue())
                {
                    deviceId = did.Value();
                }

                if (m.HasValue())
                {
                    messageCount = int.Parse(m.Value());
                }

                if (s.HasValue())
                {
                    delayInSeconds = int.Parse(s.Value());
                }

                if (tmin.HasValue())
                {
                    tminValue = double.Parse(tmin.Value());
                }

                if (tmax.HasValue())
                {
                    tmaxValue = double.Parse(tmax.Value());
                }

                if (hmin.HasValue())
                {
                    hminValue = double.Parse(hmin.Value());
                }

                if (hmax.HasValue())
                {
                    hmaxValue = double.Parse(hmax.Value());
                }



                string connection = string.Format(@"DeviceId={0};{1}", deviceId, connectionString);

                for (int i = 0; i < messageCount; i++)
                {
                    double temperature = rnd.NextDouble(tminValue, tmaxValue);
                    double humidity = rnd.NextDouble(hminValue, hmaxValue);

                    string data = temperature.ToString() + ":" + humidity.ToString();

                    string hexData = StringToHex.ConvertToHex(data);

                    var unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                    string json = String.Format(@"{{ cmd: 'rx',  seqno: 1854,  EUI: '{0}',  ts: {1},  fcnt: 27,  port: 1,  freq: 867100000,  rssi: -25,  snr: 10,  toa: 61,  dr: 'SF7',  ack: false,  bat: 255,  data: '{2}' }}", deviceId, unixTimestamp, hexData);

                    try
                    {
                        await AzureIoTHub.SendDeviceToCloudMessageAsync(connection, json);    

                        app.Out.WriteLine("Message sent from device {0} data {1}", deviceId, data);

                        if (messageCount > 1)
                            Thread.Sleep(delayInSeconds * 1000);

                    }
                    catch (Exception ex)
                    {                        
                        app.Out.WriteLine("Could not send message");
                        app.Out.WriteLine(ex.ToString());
  
                        // failed sending message, stop here
                        return 1;
                    }
                }

                return 0;
            });


            var result = app.Execute(args);

            Environment.Exit(result);
        }
    }
}
