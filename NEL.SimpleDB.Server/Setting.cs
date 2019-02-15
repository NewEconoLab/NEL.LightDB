using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.SimpleDB.Server
{
    public class Setting
    {
        public static int Port { get; private set; }
        public static string BindAddress { get; private set; }
        public static string StoragePath { get; private set; }

        public Setting()
        {
            JObject json = JObject.Parse(System.IO.File.ReadAllText("config.json"));
            Port = (int)json["port"];
            BindAddress = (string)json["bindAddress"];
            StoragePath = (string)json["server_storage_path"];
        }
    }


}
