using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.Cli.ApiServer
{
    class RpcRespose
    {
        public string jsonrpc = "";
        public UInt32 id = 1;
        public object result = "";

        public JObject ToJson()
        {
            var json = new JObject();
            json["jsonrpc"] = jsonrpc;
            json["id"] = id;
            json["result"] = (JToken)result;
            return json;
        }
    }
}
