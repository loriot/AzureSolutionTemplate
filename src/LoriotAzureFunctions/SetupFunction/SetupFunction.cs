using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Data.SqlClient;
using System;
using System.Security;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace LoriotAzureFunctions.InitFunction
{
    public static class SetupFunction
    {
        /// <summary>
        /// Helper function to convert a String to a secureString
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
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

        [FunctionName("Setup")]
        public static async System.Threading.Tasks.Task<HttpResponseMessage> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", Route = "setup")]HttpRequestMessage req, TraceWriter log)
        {
            //Create DocumentDB collection
            DocumentClient client = new DocumentClient(new System.Uri(
                String.Concat("https://", Environment.GetEnvironmentVariable("DOCUMENT_DB_NAME"), ".documents.azure.com:443/")),
                ToSecureString(Environment.GetEnvironmentVariable("DOCUMENT_DB_ACCESS_KEY")));

            await client.CreateDatabaseIfNotExistsAsync(new Microsoft.Azure.Documents.Database
            {
                Id = "db"
            });

            // Collection for device telemetry. Here the JSON property deviceId will be used as the partition key to 
            // spread across partitions. Configured for 10K RU/s throughput and an indexing policy that supports 
            // sorting against any number or string property.
            DocumentCollection myCollection = new DocumentCollection();
            myCollection.Id = "sensordatacollection";
            myCollection.PartitionKey.Paths.Add("/eui");
            await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri("db"),
                myCollection,
                new RequestOptions {
                    OfferThroughput = 400,
                });

            // Collection for device alarming.
            DocumentCollection alarmCollection = new DocumentCollection();
            alarmCollection.Id = "alarmcollection";
            alarmCollection.PartitionKey.Paths.Add("/eui");
            await client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri("db"),
                alarmCollection,
                new RequestOptions
                {
                    OfferThroughput = 400,
                });

            //Create Table in sql
            var str = Environment.GetEnvironmentVariable("SQL_DB_CONNECTION");
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                //check if table was already created
                string checkTableQuery = @"IF EXISTS(SELECT * FROM INFORMATION_SCHEMA.TABLES 
                       WHERE TABLE_NAME='WeatherData') SELECT 1 ELSE SELECT 0";
                int x = -1;
                using (SqlCommand checkTableCmd = new SqlCommand(checkTableQuery, conn))
                {
                    x = Convert.ToInt32(checkTableCmd.ExecuteScalar());
                }
                if (x == 0)
                {
                    //in case the table does not exist we create it.
                    var createTableQuery = @"CREATE TABLE [dbo].[WeatherData] (
                        [MessageGUID] UNIQUEIDENTIFIER NOT NULL,
                        [Eui]         NCHAR (16)       NULL,
                        [Temperature] FLOAT (53)       NULL,
                        [Humidity]    FLOAT (53)       NULL,
                        [ts]          BIGINT           NULL,
                        [time]        DATETIME             NULL
                    );";
                    using (SqlCommand createTableCmd = new SqlCommand(createTableQuery, conn))
                    {
                        // Execute the command and log the # rows affected.
                        var rows = await createTableCmd.ExecuteNonQueryAsync();
                        log.Info($"{ rows} rows were updated");
                    }
                }
                else if (x == -1)
                {
                    log.Error("There was a problem in accessing your database, please check your Firewall access");
                }
            }
            var template = @"{'$schema': 'https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#', 'contentVersion': '1.0.0.0', 'parameters': {}, 'variables': {}, 'resources': []}";
            HttpResponseMessage response = req.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(template, System.Text.Encoding.UTF8, "application/json");
            return response;
        }
    }
}
