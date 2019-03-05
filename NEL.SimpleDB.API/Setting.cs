using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.SimpleDB.API
{
    public class Setting
    {
        public int Port { get; private set; }
        public string BindAddress { get; private set; }
        public int DBServerPort { get; private set; }
        public string DBServerAddress { get; private set; }
        public string DBServerPath { get; private set; }

        public Setting()
        {
            JObject json = JObject.Parse(System.IO.File.ReadAllText("config.json"));
            Port = (int)json["port"];
            BindAddress = (string)json["bindAddress"];
            DBServerPort = (int)json["dbServerPort"];
            DBServerAddress = (string)json["dbServerAddress"];
            DBServerPath = (string)json["dbServerPath"];
        }
    }
}
