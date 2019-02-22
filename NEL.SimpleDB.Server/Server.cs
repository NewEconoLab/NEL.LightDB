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
        public ConcurrentDictionary<string, ISnapShot> peerSnapshots = new ConcurrentDictionary<string, ISnapShot>();
        public ConcurrentDictionary<string, IWriteBatch> peerWriteBatch = new ConcurrentDictionary<string, IWriteBatch>();
        public ConcurrentDictionary<string, IKeyIterator> peerKeyIterator = new ConcurrentDictionary<string, IKeyIterator>();

        private UInt64 writeBatchID = 0;

        public UInt64 WriteBatchID
        {
            get
            {
                lock (this)
                {
                    return writeBatchID++;
                }
            }
        }

        private UInt64 iteratorID = 0;

        public UInt64 IteratorID
        {
            get
            {
                lock (this)
                {
                    return iteratorID++;
                }
            }
        }

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
                            {
                                var snapshot = StorageService.maindb.UseSnapShot();
                                peerSnapshots[peerid + snapshot.DataHeight.ToString()] = StorageService.maindb.UseSnapShot();
                                netMsgBack.AddParam(new Param() { result = true, value = BitConverter.GetBytes(snapshot.DataHeight) });
                                return netMsgBack;
                            }
                        case "_db.CreateWriteBatch":
                            {
                                var wid = WriteBatchID;
                                peerWriteBatch[peerid + wid.ToString()] = StorageService.maindb.CreateWriteBatch();
                                netMsgBack.AddParam(new Param() { result = true ,value = BitConverter.GetBytes(wid) });
                                return netMsgBack;
                            }
                        case "_db.put":
                            {
                                for (var i = 0; i < netMsg.Params.Length; i++)
                                {
                                    IWriteBatch writeBatch = peerWriteBatch[peerid + netMsg.ID];    
                                    writeBatch.Put(netMsg.Params[i].tableid, netMsg.Params[i].key, netMsg.Params[i].value);
                                }
                                netMsgBack.AddParam(new Param() { result = true });
                                return netMsgBack;
                            }
                        case "_db.delete":
                            {
                                for (var i = 0; i < netMsg.Params.Length; i++)
                                {
                                    IWriteBatch writeBatch = peerWriteBatch[peerid + netMsg.ID];
                                    writeBatch.Delete(netMsg.Params[i].tableid, netMsg.Params[i].key);
                                }
                                netMsgBack.AddParam(new Param() { result = true });
                                return netMsgBack;
                            }
                        case "_db.write":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid + netMsg.ID];
                                StorageService.maindb.WriteBatch(writeBatch);
                                netMsgBack.AddParam(new Param() { result = true });
                                return netMsgBack;
                            }
                        case "_db.getvalue"://使用最新的snapshot 基本就是给apiserver用的
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
                                ISnapShot snapshot = peerSnapshots[peerid + netMsg.ID];
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
                        case "_db.snapshot.newiterator":
                            {
                                ISnapShot snapshot = peerSnapshots[peerid + netMsg.ID];
                                var beginKey = netMsg.Params[0].key;
                                var endKey = netMsg.Params[1].key;
                                var tableid = netMsg.Params[0].tableid;
                                var iter = snapshot.CreateKeyIterator(tableid, beginKey, endKey);
                                var itid = IteratorID;
                                peerKeyIterator[peerid + itid.ToString()] = iter;
                                netMsgBack.AddParam(new Param() { result = true,value = BitConverter.GetBytes(itid)});
                                return netMsgBack;
                            }
                        case "_db.iterator.current":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[peerid + netMsg.ID];
                                var cur = keyIterator.Current;
                                Param param = new Param();
                                param.result = true;
                                param.value = cur;
                                netMsgBack.AddParam(param);
                                return netMsgBack;
                            }
                        case "_db.iterator.next":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[peerid + netMsg.ID];
                                keyIterator.MoveNext();
                                var cur = keyIterator.Current;
                                Param param = new Param();
                                param.result = true;
                                param.value = cur;
                                netMsgBack.AddParam(param);
                                return netMsgBack;
                            }
                        case "_db.iterator.reset":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[peerid+netMsg.ID];
                                keyIterator.Reset();
                                Param param = new Param();
                                param.result = true;
                                netMsgBack.AddParam(param);
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
