using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.ServiceBus.Messaging;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reflection;

namespace SyslogToEventHub
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
        /*      static string connectionString = "Endpoint=sb://ideapoceh.servicebus.windows.net/;SharedAccessKeyName=send;SharedAccessKey=akqzo+Bs620YxRLebD6SeYDtPHX9xEZMe92FPPXnntc=;EntityPath=ideapoceh"; */
        /* static string connectionString = "Endpoint=sb://ideapoceh03.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=fWTCpgqnfXmGXfP0vsNoB/r1jTWE7IRpyZolKtpwq8g=;EntityPath=ideapoceh03h1"; */
        static string connectionString = "Endpoint=sb://ideapoceh04.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=HlD24Vl14hUC7nfyTtGsEphND00O8DHbhsk4Lsr+n3c=;EntityPath=ideapoceh04h1";

        /* static string connectionString = "Endpoint=sb://ideapoceh02-ns.servicebus.windows.net/;SharedAccessKeyName=write;SharedAccessKey=6eo1v61NiKL2+OJAksN8CBOooYKiw9Mnbjpmq0uy13Q=;EntityPath=ideapoceh02"; */


        static void Main(string[] args)
        {
            Program p = new Program();
            p.sub();
        }
        public void sub()
        {
            int event_count = 0;
            int total_event_count = 0;
            int updowncount = 0;
            string readline; 
            List<EventData> e = new List<EventData>();
            Stopwatch sw = new Stopwatch();

            EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionString);
            string[] regex_file = File.ReadAllLines(@"..\\..\\syslog.regex");




            Regex[] capture = new Regex[regex_file.Count()];
            int n = 0;

            foreach (string s in regex_file)
            {
                capture[n++] = new Regex(s, RegexOptions.Compiled | RegexOptions.ExplicitCapture);
                Console.WriteLine("Got a regex...");
            }

            sw.Start();
            Assembly execAssembly = Assembly.GetCallingAssembly();
            AssemblyName name = execAssembly.GetName();

            Console.SetCursorPosition(0, 0);
            Console.BackgroundColor = ConsoleColor.DarkMagenta;
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(string.Format("{0} {1:0}.{2:0} for .Net ({3})          ARCH.NIS.EIS.CTO.MICROSOFT.COM                     BY: COBY ROBERTS ",
                name.Name,
                name.Version.Major.ToString(),
                name.Version.Minor.ToString(),
                execAssembly.ImageRuntimeVersion
                ));

            Console.ResetColor();
            Console.SetCursorPosition(1, 29);
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.Write("TYPE CTRL+C TO END THIS PROGRAM");
            Console.ResetColor();

            Console.SetCursorPosition(16, 2);
            Console.Write("MEASURE       AVERAGE");
            Console.SetCursorPosition(16, 3);
            Console.Write("------------- ----------------------------");

            bool noteof = true;

            while (noteof)
            {
                 /* using (TextReader tr = File.OpenText(@"c:\Azure\POC\ideapoceh02\mls_syslog")) */  
                               using (TextReader tr = File.OpenText(@"\\mlsnetlog01\d$\syslog\mls_syslog.001"))   
                /*       using (TextReader tr = File.OpenText(@"\\mlsnetlog01\d$\syslog_data\ideapoceh03.txt")) */

                {

                    bool regex_match = false;

                    string line;
                    while ((line = tr.ReadLine()) != null)
                    {
                        string[] items = line.Split('\t');
                        syslog sl = new syslog();

                        sl.time = items[0];
                        sl.facility = items[1];
                        sl.address = items[2];
                        sl.message = items[3];

                        bool testupdown = sl.message.StartsWith("%LINK-3-UPDOWN:");
                        /* Console.WriteLine("Found UPDOWN? {0}", testupdown); */
                        if (testupdown)
                        { updowncount++;
                            string[] messageItems = sl.message.Split(' ');
                            string msgInterface = messageItems[2];
                            /* Console.WriteLine("Interface is {0}", msgInterface); */
                            sl.netInterface = msgInterface;
                        }

                        for (int i = 0; i <= capture.Count() - 1; i++)
                        {
                            if (capture[i].Match(line.Trim()).Success)
                            {
                                regex_match = true;
                                Match matchResults = capture[i].Match(line.Trim());

                                Group catagory = matchResults.Groups[1];
                                Group port = matchResults.Groups[2];

                                sl.message_detail = new syslog.detail(catagory.Value, port.Value);

                                break;
                            }
                            else
                            {
                                regex_match = false;
                            }
                        }

                        if (regex_match)
                        {
                            regex_match = false;
                        }
                        else
                        {
                            regex_match = false;
                        }

                        string json_eh_message = JsonConvert.SerializeObject(sl);

                        if (event_count >= 936)
                        {
                            total_event_count = total_event_count + event_count;
                            event_count = 0;

                            try
                            {
                                eventHubClient.SendBatchAsync(e);
                                Thread.Sleep(50);
                                Console.SetCursorPosition(1, 4);
                                Console.WriteLine("ASYNC SENT   : {0:0,0}", total_event_count);
                                Console.SetCursorPosition(1, 5);
                                Console.WriteLine("ELAPSED TIME : {0:00}:{1:00}:{2:00}.{3:00}", sw.Elapsed.Hours, sw.Elapsed.Minutes, sw.Elapsed.Seconds, sw.Elapsed.Milliseconds / 10);

                                if (sw.Elapsed.TotalMinutes > 1)
                                {
                                    Console.SetCursorPosition(30, 4);
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write("{0:0,0} \\ min.", total_event_count / sw.Elapsed.TotalMinutes);
                                    Console.ResetColor();
                                }
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
                        }
                        else
                        {
                            e.Add(new EventData(Encoding.UTF8.GetBytes(json_eh_message)));
                            event_count++;
                        }
                    }
                    Console.WriteLine("Got a null line\n");
                    noteof = false;
                    Console.WriteLine("Total event count is {0}", total_event_count);
                    Console.WriteLine("Total updown events is {0}", updowncount);
                    Console.ReadKey();

                }

            }
        }

    }
}

