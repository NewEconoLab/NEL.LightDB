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

                NetMessage netMsgBack = NetMessage.Create(cmd + ".back",id);

                try
                {
                    switch (cmd)
                    {
                        case "_db.usesnapshot":
                            peerSnapshots[peerid] = StorageService.maindb.UseSnapShot();
                            netMsgBack.AddParam(new Param() {result = true });
                            return netMsgBack;
                        case "_db.CreateWriteBatch":
                            peerWriteBatch[peerid] = StorageService.maindb.CreateWriteBatch();
                            netMsgBack.AddParam(new Param() { result = true });
                            return netMsgBack;
                        case "_db.put":
                            for (var i = 0; i < netMsg.Params.Length; i++)
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid];
                                writeBatch.Put(netMsg.Params[i].tableid, netMsg.Params[i].key, netMsg.Params[i].value);
                                StorageService.maindb.WriteBatch(writeBatch);
                            }
                            netMsgBack.AddParam(new Param() { result = true });
                            return netMsgBack;
                        case "_db.write":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid];
                                StorageService.maindb.WriteBatch(writeBatch);
                                netMsgBack.AddParam(new Param() { result = true });
                                return netMsgBack;
                            }
                        case "_db.getvalue"://使用最新的snapshot
                            {
                                ISnapShot snapshot = StorageService.maindb.UseSnapShot();
                                for (var i = 0; i < netMsg.Params.Length; i++)
                                {
                                    var tableid = netMsg.Params[i].tableid;
                                    var key = netMsg.Params[i].key;
                                    var value = snapshot.GetValueData(tableid, key);
                                    Param param = new Param();
                                    param.result = true;
                                    param.value = value;
                                    param.key = key;
                                    param.tableid = tableid;
                                    netMsgBack.AddParam(param);
                                }
                                return netMsgBack;
                            }
                        case "_db.snapshot.getvalue":
                            {
                                ISnapShot snapshot = peerSnapshots[peerid];
                                for (var i = 0; i < netMsg.Params.Length; i++)
                                {
                                    var tableid = netMsg.Params[i].tableid;
                                    var key = netMsg.Params[i].key;
                                    var value = snapshot.GetValueData(tableid, key);
                                    Param param = new Param();
                                    param.result = true;
                                    param.value = value;
                                    param.key = key;
                                    param.tableid = tableid;
                                    netMsgBack.AddParam(param);
                                }
                                return netMsgBack;
                            }
                        default:
                            throw new Exception("unknown msg cmd:" + netMsg.Cmd);
                    }
                }
                catch (Exception e)
                {
                    netMsgBack.AddParam(new Param() { error = Encoding.UTF8.GetBytes(e.Message) });
                    return netMsgBack;
                }

            }
        }
    }
}
