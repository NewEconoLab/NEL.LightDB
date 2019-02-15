using System;
using System.Linq;
using System.Text;
using LightDB;
using NEL.Pipeline;
using NEL.Simple.SDK;
using Neo;
using Neo.IO;
using Neo.IO.Json;
using Neo.Ledger;
using System.Collections.Concurrent;

namespace NEL.Cli.ApiServer
{
    public class ClientModule : Module
    {
        public override void Dispose()
        {
            base.Dispose();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void OnStart()
        {

        }

        //private ConcurrentDictionary<string, IModulePipeline> dic = new ConcurrentDictionary<string, IModulePipeline>();
        public override void OnTell(IModulePipeline from, byte[] data)
        {
            if (from != null && from.path != "rpc")//来自服务器
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
                {
                    NetMessage netMessage = NetMessage.Unpack(ms);
                    ProcessGet(netMessage);
                }
            }
        }

        public override void OnTellLocalObj(IModulePipeline from, object obj)
        {
            if (from.path == "rpc") //来自rpcservermodule
            {
                ProcessSend((JObject)obj);
            }
        }




        public override string ToString()
        {
            return base.ToString();
        }
    }
}
