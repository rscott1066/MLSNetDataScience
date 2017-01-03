using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Microsoft.Azure;                      // Namespace for CloudConfigurationManager
using Microsoft.WindowsAzure.Storage;       // Namespace for CloudStorageAccount
using Microsoft.WindowsAzure.Storage.Blob;  // Namespace for Blob storage types
using Microsoft.ServiceBus.Messaging;       // Namespace for Event Hub

namespace AzBlob
{

    public class syslog
    {
        public string time { get; set; }
        public string facility { get; set; }
        public string address { get; set; }
        public string netInterface { get; set; }
        public string message { get; set; }
        public detail message_detail { get; set; }

        public class detail
        {

            string _catagory = "";
            string _port = "";

            public detail(string Catagory, string Port)
            {
                _catagory = Catagory;
                _port = Port;
            }
            public string catagory { get { return _catagory; } }

            public string port { get { return _port; } }
        }
    }

    class Program
    {

        static string eventHubName = "syslog";
        static string connectionString = "Endpoint=sb://ideapoceh04.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=HlD24Vl14hUC7nfyTtGsEphND00O8DHbhsk4Lsr+n3c=;EntityPath=ideapoceh04h1";

        static void Main(string[] args)
        {

            int event_count = 0;
            int total_event_count = 0;
            int updowncount = 0;
            List<EventData> e = new List<EventData>();
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionString);

            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("syslogs");

            // Loop over items within the container and output the length and URI.
            foreach (IListBlobItem item in container.ListBlobs(null, false))
            {
                if (item.GetType() == typeof(CloudBlockBlob))
                {
                    CloudBlockBlob blob = (CloudBlockBlob)item;

                    Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);

                }
                else if (item.GetType() == typeof(CloudPageBlob))
                {
                    CloudPageBlob pageBlob = (CloudPageBlob)item;

                    Console.WriteLine("Page blob of length {0}: {1}", pageBlob.Properties.Length, pageBlob.Uri);

                }
                else if (item.GetType() == typeof(CloudBlobDirectory))
                {
                    CloudBlobDirectory directory = (CloudBlobDirectory)item;

                    Console.WriteLine("Directory: {0}", directory.Uri);
                }
                Console.ReadLine();

                // Retrieve reference to a blob named "myblob.txt"
                CloudBlockBlob blockBlob2 = container.GetBlockBlobReference("mls_syslog");

                string line;
                //using (var memoryStream = new MemoryStream())
                using (var stream = blockBlob2.OpenRead())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        //      while ( !reader.EndOfStream)
                        while ((line = reader.ReadLine()) != null)
                        {
                            //   Console.WriteLine(reader.ReadLine());
                            Console.WriteLine(line);
                            //Console.ReadLine();
                            string[] items = line.Split('\t');
                            syslog sl = new syslog();

                            sl.time = items[0];
                            sl.facility = items[1];
                            sl.address = items[2];
                            sl.message = items[3];

                            bool testupdown = sl.message.StartsWith("%LINK-3-UPDOWN:");
                            /* Console.WriteLine("Found UPDOWN? {0}", testupdown); */
                            if (testupdown)
                            {
                                updowncount++;
                                string[] messageItems = sl.message.Split(' ');
                                string msgInterface = messageItems[2];
                                /* Console.WriteLine("Interface is {0}", msgInterface); */
                                sl.netInterface = msgInterface;
                            }

                            string json_eh_message = JsonConvert.SerializeObject(sl);

                            if (event_count >= 936)
                            {
                                total_event_count = total_event_count + event_count;
                                event_count = 0;

                                try
                                {
                                    eventHubClient.SendBatchAsync(e);
                                }
                                catch (Exception exception)
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("{0} > Exception: {1}", DateTime.Now, exception.Message);
                                    Console.ResetColor();
                                }

                                e.Clear();

                                e.Add(new EventData(Encoding.UTF8.GetBytes(json_eh_message)));
                                event_count++;
                                Console.WriteLine(json_eh_message);
                            }
                            else
                            {
                                e.Add(new EventData(Encoding.UTF8.GetBytes(json_eh_message)));
                                event_count++;
                                Console.WriteLine(json_eh_message);
                            }
                        }
                    }
                }
                Console.WriteLine("Port flap event count: {0}", updowncount);
                Console.WriteLine("Total events: {0}", total_event_count);
                Console.ReadLine();
                //using (var memoryStream2 = new BufferedStream(memoryStream))
               /* {
                    blockBlob2.DownloadToStream(memoryStream);
                    //while ((text = memoryStream2.Read)
                    text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray());
                    text = memoryStream.
                    Console.WriteLine(text);
                    Console.ReadLine();
                } */

            }
        }
    }
}
