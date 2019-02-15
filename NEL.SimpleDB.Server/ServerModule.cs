using System;
using NEL.Pipeline.MsgPack;
using NEL.Pipeline;
using Newtonsoft.Json.Linq;
using System.Text;
using NEL.Simple.SDK;

namespace NEL.SimpleDB.Server
{
    public class ServerModule : Module
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
            server = new Server();
        }

        private Server server;

        public override void OnTell(IModulePipeline from, byte[] data)
        {
            if (from != null && from.system.Remote != null)//从远程投递而来
            {
                UInt64 peerid = from.system.PeerID;
                //Console.WriteLine("peerid:"+from.system.PeerID);
                NetMessage netMessage =  server.Process(peerid,data);
                from.Tell(netMessage.ToBytes());
            }
        }

        public override void OnTellLocalObj(IModulePipeline from, object obj)
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }
}
