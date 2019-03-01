using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.SimpleDB.Server
{
    public class Setting
    {
        public int Port { get; private set; }
        public string BindAddress { get; private set; }
        public string StoragePath { get; private set; }
        public string Conn_Track { get; private set; }
        public string DataBase_Track { get; private set; }
        public string Coll_Track { get; private set; }

        public Setting()
        {
            JObject json = JObject.Parse(System.IO.File.ReadAllText("config.json"));
            Port = (int)json["port"];
            BindAddress = (string)json["bindAddress"];
            StoragePath = (string)json["server_storage_path"];
            Conn_Track = (string)json["Conn_Track"];
            DataBase_Track = (string)json["DataBase_Track"];
            Coll_Track = (string)json["Coll_Track"];
        }
    }


}
