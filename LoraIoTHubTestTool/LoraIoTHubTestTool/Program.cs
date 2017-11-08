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

            string connectionString ="";
            string deviceId = "BE7A00000000190F";

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
            "Mandatory IoTHub Connection string",
            CommandOptionType.SingleValue);

            var did = app.Option("-d |--device<deviceId>",
            "Optional deviceid loraWan EUI (string)",
            CommandOptionType.SingleValue);

            var m = app.Option("-m |--mcount<messagecount>",
            "Optional no of message to sent (default 1)",
            CommandOptionType.SingleValue);

            var s = app.Option("-s |--s<seconds>",
           "Optional delay between msg in seconds (default 10)",
           CommandOptionType.SingleValue);

            var tmin = app.Option("-tmin |--tmin<mintemperature>",
            "Optional minimum random temperature (double)",
            CommandOptionType.SingleValue);

            var tmax = app.Option("-tmax |--tmax<maxtemperature>",
            "Optional maximum random temperature (double)",
            CommandOptionType.SingleValue);

            var hmin = app.Option("-hmin |--hmin<minhumidity>",
            "Optional minimum random humidity (double)",
            CommandOptionType.SingleValue);

            var hmax = app.Option("-hmax |--hmax<maxhumidity>",
            "Optional maximum random humidity (double)",
            CommandOptionType.SingleValue);

            app.OnExecute(() => {

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
                    tminValue =  double.Parse(tmin.Value());                 
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

                    StringBuilder sb = new StringBuilder();

                    string json = String.Format(@"{{ cmd: 'rx',  seqno: 1854,  EUI: '{0}',  ts: 1507044971381,  fcnt: 27,  port: 1,  freq: 867100000,  rssi: -25,  snr: 10,  toa: 61,  dr: 'SF7',  ack: false,  bat: 255,  data: '{1}' }}",deviceId, hexData);

                    AzureIoTHub.SendDeviceToCloudMessageAsync(connection, json).Wait();

                    Console.WriteLine("Message sent from device {0} data {1}", deviceId, data);

                    if(messageCount>1)
                        Thread.Sleep(delayInSeconds * 1000);


                }

                return 0;
            });


            var result = app.Execute(args);


            Environment.Exit(result);




        }
    }
}
