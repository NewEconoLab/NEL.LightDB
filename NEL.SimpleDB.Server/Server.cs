using LightDB;
using NEL.Simple.SDK;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Collections.Concurrent;

namespace NEL.SimpleDB.Server
{
    public class Server
    {
        public ConcurrentDictionary<UInt64, ISnapShot> peerSnapshots = new ConcurrentDictionary<UInt64, ISnapShot>();
        public ConcurrentDictionary<UInt64, IWriteBatch> peerWriteBatch = new ConcurrentDictionary<UInt64, IWriteBatch>();

        public NetMessage Process(UInt64 peerid,byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                NetMessage netMsg = NetMessage.Unpack(ms);
                string cmd = netMsg.Cmd;
                string id = netMsg.ID;
                byte[] tableid = netMsg.Params.ContainsKey("tableid") ? netMsg.Params["tableid"] : null;
                byte[] key = netMsg.Params.ContainsKey("key") ? netMsg.Params["key"] : null;
                byte[] value = netMsg.Params.ContainsKey("value") ? netMsg.Params["value"] : null;

                NetMessage netMsgBack = NetMessage.Create(cmd + ".back",id);

                try
                {
                    switch (cmd)
                    {
                        case "_db.usesnapshot":
                            peerSnapshots[peerid] = StorageService.maindb.UseSnapShot();
                            netMsgBack.Params["result"] = Encoding.UTF8.GetBytes("succ");
                            return netMsgBack;
                        case "_db.CreateWriteBatch":
                            peerWriteBatch[peerid] = StorageService.maindb.CreateWriteBatch();
                            netMsgBack.Params["result"] = Encoding.UTF8.GetBytes("succ");
                            return netMsgBack;
                        case "_db.put":
                            if (tableid != null && key != null && value != null)
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid];
                                writeBatch.Put(tableid, key, value);
                                StorageService.maindb.WriteBatch(writeBatch);
                            }
                            netMsgBack.Params["result"] = Encoding.UTF8.GetBytes("succ");
                            return netMsgBack;
                        case "_db.write":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid];
                                StorageService.maindb.WriteBatch(writeBatch);
                                netMsgBack.Params["result"] = Encoding.UTF8.GetBytes("succ");
                                return netMsgBack;
                            }
                        case "_db.snapshot.getvalue":
                            if (tableid != null && key != null)
                            {
                                ISnapShot snapshot = peerSnapshots[peerid];
                                value = snapshot.GetValueData(tableid, key);
                                netMsgBack.Params["result"] = Encoding.UTF8.GetBytes("succ");
                                netMsgBack.Params["value"] = value;
                                netMsgBack.Params["tableid"] = tableid;
                                netMsgBack.Params["key"] = key;
                            }
                            return netMsgBack;
                        default:
                            throw new Exception("unknown msg cmd:" + netMsg.Cmd);
                    }
                }
                catch (Exception e)
                {
                    netMsgBack.Params["error"] = Encoding.UTF8.GetBytes(e.Message);
                    return netMsgBack;
                }

            }
        }
    }
}
