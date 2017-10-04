using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json.Linq;
using System;
using Newtonsoft.Json;
using System.Text;

namespace Decoder
{
    public class Status
    {
        public bool Tempinfo { get; set; }
        public bool AccelerometerTransmission { get; set; }
        public bool PushbuttonTransmission { get; set; }
        public bool GPSInformationPresence { get; set; }
        public bool UplinkFrameCounter { get; set; }
        public bool DownlinkFrameCounter { get; set; }
        public bool BatteryLevelInformation { get; set; }
        public bool RSSISNLInfo { get; set; }
    }

    /// <summary>
    /// Class encompassing all GPS information based on https://www.adeunis.com/wp-content/uploads/2017/08/ARF8123AA_ADEUNIS_LORAWAN_FTD_UG_V1.2.0_FR_GB.pdf.
    /// </summary>
    public class GPSData
    {
        public Status status;
        public int temperature;
        public Location location;

        /// <summary>
        /// Constructor parse the input byte and populate the field accordingly.
        /// </summary>
        /// <param name="inputByte">bytes received from the RouterFunction</param>
        public GPSData(byte[] inputByte)
        {
            status = new Status();
            location = new Location();
            this.setStatus(inputByte[0]);
            int offsetTemperatureByte = 0;
            if (this.status.Tempinfo)
            {
                this.setTemp(inputByte[1]);
                offsetTemperatureByte = 1;
            }
            if (this.status.GPSInformationPresence)
            {
                this.setLocation(inputByte.Skip(1 + offsetTemperatureByte).Take(4).ToArray(), inputByte.Skip(5 + offsetTemperatureByte).Take(4).ToArray());
            }
        }

        /// <summary>
        /// Method setting the status information.
        /// </summary>
        /// <param name="statusInput"></param>
        public void setStatus(byte statusInput)
        {
            status.Tempinfo = this.getBit(statusInput, 8);
            status.AccelerometerTransmission = this.getBit(statusInput, 7);
            status.PushbuttonTransmission = this.getBit(statusInput, 6);
            status.GPSInformationPresence = this.getBit(statusInput, 5);
            status.UplinkFrameCounter = this.getBit(statusInput, 4);
            status.DownlinkFrameCounter = this.getBit(statusInput, 3);
            status.BatteryLevelInformation = this.getBit(statusInput, 2);
            status.RSSISNLInfo = this.getBit(statusInput, 1);
        }

        /// <summary>
        /// method populating Location information
        /// </summary>
        /// <param name="inputLatitude"></param>
        /// <param name="inputLongitude"></param>
        public void setLocation(byte[] inputLatitude, byte[] inputLongitude)
        {
            //latitude
            var degree = getValue(inputLatitude[0], 0, 4);
            var tenDegree = getValue(inputLatitude[0], 4, 4);
            this.location.Latitude.degree = tenDegree * 10 + degree;

            var minute = getValue(inputLatitude[1], 0, 4);
            var tenMinute = getValue(inputLatitude[1], 4, 4);
            this.location.Latitude.minute = tenMinute * 10 + minute;

            var decimalSecond = getValue(inputLatitude[2], 0, 4);
            var decimalFirst = getValue(inputLatitude[2], 4, 4);
            var decimalLast = getValue(inputLatitude[3], 0, 4);
            var decimalThird = getValue(inputLatitude[3], 4, 4);
            this.location.Latitude.fraction = decimalFirst * 1000 + decimalSecond * 100 + decimalThird * 10;

            this.location.Latitude.isNorthOrEastHemisphere = getBit(inputLatitude[3], 1);
        }

        /// <summary>
        /// get value contained in a byte between a <paramref name="startIndex"/> and a <paramref name="numberOfBytes"/>.
        /// </summary>
        /// <param name="currentByte">the byte needed to parse.</param>
        /// <param name="startIndex">starting index of the number.</param>
        /// <param name="numberOfBytes">number of bits to parse.</param>
        /// <returns></returns>
        private int getValue(byte currentByte, int startIndex, int numberOfBytes)
        {
            int value = 0;
            for (int i = startIndex + numberOfBytes; i > startIndex; i--)
            {
                var bitValue = Convert.ToInt32(getBit(currentByte, i));
                var power = (int)Math.Pow(2, i - startIndex - 1);
                value += bitValue * power;
            }
            return value;
        }

        /// <summary>
        /// parse and set temperature information.
        /// </summary>
        /// <param name="tempInput"></param>
        public void setTemp(byte tempInput)
        {
            bool indicator = this.getBit(tempInput, 8);
            tempInput = (byte)(tempInput & 0x7F);

            int result;
            if (indicator == true)
            {
                result = tempInput - 128;
            }
            else { result = tempInput; }
            temperature = result;
        }

        /// <summary>
        /// Get a specific bit information.
        /// </summary>
        /// <param name="b">The byte from which to look the bit information.</param>
        /// <param name="bitNumber">The index of the bit you need, starting from the right (with index 1). </param>
        /// <returns></returns>
        public bool getBit(byte b, int bitNumber)
        {
            var bit = (b & (1 << bitNumber - 1)) != 0;
            return bit;
        }
    }

    public class Coordinate
    {
        public int degree { get; set; }
        public int minute { get; set; }
        public int fraction { get; set; }
        public bool isNorthOrEastHemisphere { get; set; }
    }

    public class Location
    {
        public Coordinate Latitude { get; set; }
        public Coordinate Longitude { get; set; }

        public Location()
        {
            Latitude = new Coordinate();
            Longitude = new Coordinate();
        }
    }

    public static class SensorDecoderGps
    {
        /// <summary>
        /// Helper method converting a Hex string to a byte array.
        /// </summary>
        /// <param name="hex">input Hex string</param>
        /// <returns></returns>
        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        [FunctionName("SensorDecoderGps")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            // Get request body
            string body = await req.Content.ReadAsStringAsync();
            dynamic payload = JObject.Parse(body);

            string hexInput = payload.data;
            byte[] bytes = StringToByteArray(hexInput);
            GPSData gpsData = new GPSData(bytes);

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(gpsData), Encoding.UTF8, "application/json")
            };
        }
    }
}
