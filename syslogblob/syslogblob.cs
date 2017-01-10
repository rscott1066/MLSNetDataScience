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
using System.Reflection;

// install nuget package WindowsAzure.ServiceBus
// install nuget package WindowsAzure.Storage
// install nuget package Newtonsoft.Json
// install Microsoft.WindowsAzure.ConfigurationManager

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
        //static string connectionString = "Endpoint=sb://ideapoceh04.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=HlD24Vl14hUC7nfyTtGsEphND00O8DHbhsk4Lsr+n3c=;EntityPath=ideapoceh04h1";
        static string json_eh_message;

        static void Main(string[] args)
        {

            Assembly execAssembly = Assembly.GetCallingAssembly();
            AssemblyName assemblyName = execAssembly.GetName();

            int traceLevel = 3;
            int event_count = 0;
            int total_event_count = 0;
            int updowncount = 0;

            List<EventData> e = new List<EventData>();
            //EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionString);
            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(CloudConfigurationManager.GetSetting("EventHubConnectionString"));

            // Parse the connection string and return a reference to the storage account.
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                CloudConfigurationManager.GetSetting("StorageConnectionString"));
            // Create the blob client.
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve reference to a previously created container.
            CloudBlobContainer container = blobClient.GetContainerReference("syslogs");

            // Retrieve reference to a previously created container for output logs
            CloudBlobContainer containerlogs = blobClient.GetContainerReference("logs");
            // Create the logs container if it does not already exist
            containerlogs.CreateIfNotExists();
            CloudAppendBlob logout = containerlogs.GetAppendBlobReference("ideapoceh04out");
            if (!logout.Exists()) { logout.CreateOrReplace(); }
            logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Beginning execution {2}", DateTime.UtcNow, assemblyName, Environment.NewLine));

            if (traceLevel == 4)
            { 
                // Loop over items within the container and output the length and URI.
                foreach (IListBlobItem item in container.ListBlobs(null, false))
                {
                    if (item.GetType() == typeof(CloudBlockBlob))
                    {
                        CloudBlockBlob blob = (CloudBlockBlob)item;

                        //Console.WriteLine("Block blob of length {0}: {1}", blob.Properties.Length, blob.Uri);
                        logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Block blob of length {2}: {3} {4}", DateTime.UtcNow, assemblyName, blob.Properties.Length, blob.Uri, Environment.NewLine));
                    }
                    else if (item.GetType() == typeof(CloudPageBlob))
                    {
                        CloudPageBlob pageBlob = (CloudPageBlob)item;

                        //Console.WriteLine("Page blob of length {0}: {1}", pageBlob.Properties.Length, pageBlob.Uri);
                        logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Page blob of length {2}: {3} {4}", DateTime.UtcNow, assemblyName, pageBlob.Properties.Length, pageBlob.Uri, Environment.NewLine));

                    }
                    else if (item.GetType() == typeof(CloudBlobDirectory))
                    {
                        CloudBlobDirectory directory = (CloudBlobDirectory)item;

                        //Console.WriteLine("Directory: {0}", directory.Uri);
                        logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t Directory {2}: {3}", DateTime.UtcNow, assemblyName, directory.Uri, Environment.NewLine));
                    }
                    else if (item.GetType() == typeof(CloudAppendBlob))
                    {
                        CloudAppendBlob appendBlob = (CloudAppendBlob)item;

                        //Console.WriteLine("Append blob of length: {0}: {1}", appendBlob.Properties.Length, appendBlob.Uri);
                        logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Append blob of length {2}: {3} {4}", DateTime.UtcNow, assemblyName, appendBlob.Properties.Length, appendBlob.Uri, Environment.NewLine));
                    }
                }
            }
            //Console.ReadLine();

            // Retrieve reference to a blob named "myblob.txt"
            CloudBlockBlob blockBlob2 = container.GetBlockBlobReference("mls_syslog");

            string line;
            //string json_eh_message;

            DateTime startTime = DateTime.UtcNow;

            //using (var memoryStream = new MemoryStream())
            using (var stream = blockBlob2.OpenRead())
            {
                using (StreamReader reader = new StreamReader(stream))
                {
                    //      while ( !reader.EndOfStream)
                    while ((line = reader.ReadLine()) != null)
                    {
                        //   Console.WriteLine(reader.ReadLine());
                        //Console.WriteLine(line);
                        if (traceLevel == 4)
                        {
                            logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : {2} {3}", DateTime.UtcNow, assemblyName, line, Environment.NewLine));
                        }
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
                        // string json_eh_message = JsonConvert.SerializeObject(sl);
                        json_eh_message = JsonConvert.SerializeObject(sl);

                        if (event_count > 500)
                        {
                            try
                            {
                                //eventHubClient.SendBatchAsync(e);
                                eventHubClient.SendBatch(e);
                                logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Submitting a batch of {2} events to EventHub {3}", DateTime.UtcNow, assemblyName, event_count, Environment.NewLine));
                                //Console.WriteLine("Submitting batch of {0} events", event_count);
                            }
                            catch (Exception exception)
                            {
                                //Console.ForegroundColor = ConsoleColor.Red;
                                //Console.WriteLine("{0} > Exception: {1}", DateTime.Now, exception.Message);
                                //Console.ResetColor();
                                logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Exception raised: {2} {3}", DateTime.UtcNow, assemblyName, exception.Message, Environment.NewLine));
                            }
                            total_event_count = total_event_count + event_count;
                            event_count = 0;
                            //Console.WriteLine("Total events submitted: {0}", total_event_count);

                            e.Clear();

                            e.Add(new EventData(Encoding.UTF8.GetBytes(json_eh_message)));
                            event_count++;
                            //Console.WriteLine(json_eh_message);
                        }
                        else
                        {
                            e.Add(new EventData(Encoding.UTF8.GetBytes(json_eh_message)));
                            event_count++;
                            //Console.WriteLine(json_eh_message);
                        }
                    }   // EOF -- submit final batch of remaining events
                    try
                    {
                        //eventHubClient.SendBatchAsync(e);
                        eventHubClient.SendBatch(e);
                        //Console.WriteLine("Submitting final batch of {0}", event_count);
                        logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Submitting final batch of {2} events to EventHub {3}", DateTime.UtcNow, assemblyName, event_count, Environment.NewLine));
                    }
                    catch (Exception exception)
                    {
                        //Console.ForegroundColor = ConsoleColor.Red;
                        //Console.WriteLine("{0} > Exception: {1}", DateTime.Now, exception.Message);
                        //Console.ResetColor();
                        logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Exception occurred while submitting batch to EventHub: {2} {3}", DateTime.UtcNow, assemblyName, exception.Message, Environment.NewLine));
                    }
                    total_event_count += event_count;
                }
            }

            DateTime endTime = DateTime.UtcNow;
            var ts = new TimeSpan(startTime.Ticks - endTime.Ticks);
            double delta = Math.Abs(ts.TotalSeconds);

            logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : {2} link up/down events processed {3}", DateTime.UtcNow, assemblyName, updowncount, Environment.NewLine));
            logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : {2} total events processed {3}", DateTime.UtcNow, assemblyName, total_event_count, Environment.NewLine));
            logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : {2} events per second {3}", DateTime.UtcNow, assemblyName, total_event_count/delta, Environment.NewLine));
            logout.AppendText(String.Format("Time: {0:u} : Assembly: {1} \t : Ending execution {2}", DateTime.UtcNow, assemblyName, Environment.NewLine));

            //Console.WriteLine("Port flap event count: {0}", updowncount);
            //Console.WriteLine("Total events: {0}", total_event_count);
            //Console.ReadLine();
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
