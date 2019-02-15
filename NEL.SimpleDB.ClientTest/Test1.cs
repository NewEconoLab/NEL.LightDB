using NEL.Pipeline;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NEL.Common;
using NEL.Peer.Tcp;
using NEL.Pipeline;
using Newtonsoft.Json.Linq;
using NEL.Simple.SDK;
using System.Linq;
using Neo;
using Neo.Ledger;
using Neo.IO;

namespace NEL.SimpleDB.ClientTest
{

    public class Test1
    {
        public static async Task Test()
        {
            var logger = new Logger();

            var systemC = PipelineSystem.CreatePipelineSystemV1(logger);
            systemC.RegistModule("client", new Client());
            systemC.OpenNetwork(new PeerOption());
            systemC.Start();

            var actor = systemC.GetPipeline(null, "this/client");
            while (true)
            {
                Console.Write("1.remote>");
                var line = Console.ReadLine();
                if (line == "exit")
                {
                    systemC.CloseListen();
                    systemC.Dispose();
                    break;
                }
                if (line == "")
                    continue;
                if (line == "put")
                {
                    //UInt160 script_hash = UInt160.Parse("03febccf81ac85e3d795bc5cbd4e84e907812aa3");
                    //byte[] key = "5065746572".HexToBytes();
                    //StorageKey storageKey = new StorageKey
                    //{
                    //    ScriptHash = script_hash,
                    //    Key = key
                    //};
                    //NetMessage netMessage = NetMessage.Create("_db.put");
                    //netMessage.Params["tableid"] = new byte[] { };
                    //netMessage.Params["key"] = (new byte[] { 0x70 }).Concat(storageKey.ToArray()).ToArray();
                    //netMessage.Params["value"] = Encoding.UTF8.GetBytes("value");
                    //actor.Tell(netMessage.ToBytes());
                    //continue;
                }
                if (line == "usesnap")
                {
                    NetMessage netMessage = NetMessage.Create("_db.usesnapshot");
                    actor.Tell(netMessage.ToBytes());
                    continue;
                }
                if (line == "getvalue")
                {
                    UInt160 script_hash = UInt160.Parse("03febccf81ac85e3d795bc5cbd4e84e907812aa3");
                    byte[] key = "5065746572".HexToBytes();
                    StorageKey storageKey = new StorageKey
                    {
                        ScriptHash = script_hash,
                        Key = key
                    };
                    NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue");
                    netMessage.Params["tableid"] = new byte[] { };
                    netMessage.Params["key"] = (new byte[] { 0x70 }).Concat(storageKey.ToArray()).ToArray();
                    actor.Tell(netMessage.ToBytes());
                    continue;
                }


            }
        }


    }

    public class Client : Module
    {
        public override void OnStart()
        {
        }

        public override void OnTell(IModulePipeline from, byte[] data)
        {
            if (from == null)
            {
                var actor = this.GetPipeline("127.0.0.1:8080/server");
                actor.Tell(data);
            }
            else
            {
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream(data))
                {
                    NetMessage netMessage = NetMessage.Unpack(ms);
                    Console.WriteLine(Encoding.UTF8.GetString(netMessage.Params["result"]));
                }
            }
        }

        public override void OnTellLocalObj(IModulePipeline from, object obj)
        {
            throw new NotImplementedException();
        }
    }
}
