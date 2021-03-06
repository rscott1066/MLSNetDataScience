#r "Newtonsoft.Json"
#r "Microsoft.WindowsAzure.Storage"
#r "Microsoft.ServiceBus"
using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using Newtonsoft.Json;
using Microsoft.Azure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.ServiceBus.Messaging;
public static void Run(Stream myBlob, string name, TraceWriter log)
{    
  string json_eh_message;    
  log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");    
  StreamReader reader = new StreamReader(myBlob);    
  string blobline = reader.ReadLine();    
  log.Info($"First line from blob: {blobline}");    
  string[] items = blobline.Split('\t');    
  syslog sl = new syslog();    
  sl.time = items[0];    
  sl.facility = items[1];    
  sl.address = items[2];    
  sl.message = items[3];    
  json_eh_message = JsonConvert.SerializeObject(sl);    
  //outputEventHubMessage = json_eh_message;    
  log.Info($"{json_eh_message}");    
  List<EventData> e = new List<EventData>();    
  //EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(connectionString);    
  //EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(ConfigurationManager.GetSetting("ideapoceh04"));
  EventHubClient eventHubClient = EventHubClient.CreateFromConnectionString(ConfigurationManager.ConnectionStrings["ideapoceh04"].ConnectionString);    
  
  //var str = ConfigurationManager.ConnectionStrings["sqldb_connection"].ConnectionString;    
  e.Add(new EventData(Encoding.UTF8.GetBytes(json_eh_message)));    
  eventHubClient.SendBatch(e);
}    

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
    public string catagory 
    { get { return _catagory; } }            
    public string port { get { return _port; } }        
  }    
}
