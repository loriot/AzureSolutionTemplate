using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace LoriotAlarmingFunctions
{
    public static class IntervalCheck
    {
        //timer for notification (in minutes)
        public static int intervalCheck = 3; 

        [FunctionName("CheckIfAlreadyNotified")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            using (DocumentClient client = new DocumentClient(new System.Uri(
                String.Concat("https://", Environment.GetEnvironmentVariable("DOCUMENT_DB_NAME"), ".documents.azure.com:443/")),
            ToSecureString(Environment.GetEnvironmentVariable("DOCUMENT_DB_ACCESS_KEY"))))
            {
                string body = await req.Content.ReadAsStringAsync();
                var inputMessage = JsonConvert.DeserializeObject<Message>(body);
                bool responseBool = false;
                var query = new SqlQuerySpec("SELECT TOP 1 * FROM books c WHERE c.eui = @eui ORDER BY c._ts DESC", new SqlParameterCollection(new SqlParameter[] { new SqlParameter { Name = "@eui", Value = inputMessage.eui } }));
                var documentList = client.CreateDocumentQuery<Message>(UriFactory.CreateDocumentCollectionUri("db", "alarmcollection"), query, new FeedOptions { EnableCrossPartitionQuery = true }).AsEnumerable();

                //if a document already exists
                if (documentList.Count() == 1)
                {
                    if ((DateTime.Now - documentList.First().lastAlarm).TotalMinutes > intervalCheck)
                    {
                        responseBool = true;
                        inputMessage.id = documentList.First().id;
                        inputMessage.lastAlarm = DateTime.Now;
                        inputMessage.alarmRang = true;
                        log.Info("Ringing Alarm");
                    }
                    else {
                        inputMessage.lastAlarm = documentList.First().lastAlarm;
                        inputMessage.alarmRang = false;
                    }

                }
                else if (documentList.Count() == 0) {
                    inputMessage.lastAlarm = DateTime.Now;
                    inputMessage.alarmRang = true;
                }
                    

                await client.UpsertDocumentAsync(UriFactory.CreateDocumentCollectionUri("db", "alarmcollection"), inputMessage);
          
                return new HttpResponseMessage()
                {
                    Content = new StringContent(JsonConvert.SerializeObject(new ReturnData(responseBool)), Encoding.UTF8, "application/json")
                };
            }
        }

        private static SecureString ToSecureString(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return null;
            }
            var result = new SecureString();
            foreach (var c in source)
            {
                result.AppendChar(c);
            }
            return result;
        }




    }
    /// <summary>
    /// Helper function to convert a String to a secureString
    /// </summary>
    /// <param name="source"></param>
    /// <returns></returns>


    public class ReturnData
    {
        public bool doAlarmAction;
        public ReturnData(bool input)
        {
            doAlarmAction = input;
        }

    }


    public class Message
    {
        public string eui { get; set; }
        public string messageGuid { get; set; }
        public bool alarmRang { get; set; }
        public Raw raw { get; set; }
        public Metadata metadata { get; set; }
        public Decoded decoded { get; set; }
        public DateTime EventProcessedUtcTime { get; set; }
        public int PartitionId { get; set; }
        public DateTime EventEnqueuedUtcTime { get; set; }
        public string id { get; set; }
        public string _rid { get; set; }
        public string _self { get; set; }
        public string _etag { get; set; }
        public string _attachments { get; set; }
        public int _ts { get; set; }

        public DateTime lastAlarm { get; set; }
    }

    public class Raw
    {
        public string cmd { get; set; }
        public int seqno { get; set; }
        public string EUI { get; set; }
        public long ts { get; set; }
        public int fcnt { get; set; }
        public int port { get; set; }
        public int freq { get; set; }
        public int rssi { get; set; }
        public int snr { get; set; }
        public int toa { get; set; }
        public string dr { get; set; }
        public int ack { get; set; }
        public int bat { get; set; }
        public string data { get; set; }
    }

    public class Metadata
    {
        public string sensorDecoder { get; set; }
        public string sensorName { get; set; }
        public string location { get; set; }
        public string alarm { get; set; }
    }

    public class Decoded
    {
        public float temperature { get; set; }
        public float humidity { get; set; }
    }



}
