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

                NetMessage netMsgBack;
                Console.WriteLine(netMsg.Cmd);
                try
                {
                    switch (cmd)
                    {
                        case "_db.usesnapshot":
                            {
                                var snapshot = StorageService.maindb.UseSnapShot();
                                peerSnapshots[peerid + snapshot.DataHeight.ToString()] = StorageService.maindb.UseSnapShot();
                                var p = new Param() { result = true, snapid = snapshot.DataHeight };
                                netMsgBack = NetMessage.Create(cmd,p,id);
                                return netMsgBack;
                            }
                        case "_db.disposeSnapshot":
                            {
                                ISnapShot snapshot;
                                peerSnapshots.TryRemove(peerid + netMsg.ID, out snapshot);
                                snapshot.Dispose();
                                var p = new Param() { result = true};
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.CreateWriteBatch":
                            {
                                var wbid = WriteBatchID;
                                peerWriteBatch[peerid+ wbid.ToString()] = StorageService.maindb.CreateWriteBatch();
                                var p = new Param() { result = true ,wbid = wbid};
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.put":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid+netMsg.Param.wbid.ToString()];
                                writeBatch.Put(netMsg.Param.tableid, netMsg.Param.key, netMsg.Param.value);
                                var p = new Param() { result = true };
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.delete":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid + netMsg.Param.wbid.ToString()];
                                writeBatch.Delete(netMsg.Param.tableid, netMsg.Param.key);
                                var p = new Param() { result = true };
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.write":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[peerid+netMsg.Param.wbid.ToString()];
                                var p = new Param() { result = false };
                                if (writeBatch.wbcount > 0)
                                {
                                    StorageService.maindb.WriteBatch(writeBatch);
                                    peerWriteBatch.Remove(peerid + netMsg.Param.wbid.ToString(), out writeBatch);
                                    p = new Param() { result = true };
                                }
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.getvalue"://使用最新的snapshot 基本就是给apiserver用的
                            {
                                ISnapShot snapshot = StorageService.maindb.UseSnapShot();
                                var tableid = netMsg.Param.tableid;
                                var key = netMsg.Param.key;
                                var value = snapshot.GetValueData(tableid, key);
                                Param param = new Param();
                                param.snapid = snapshot.DataHeight;
                                param.result = true;
                                param.value = value;
                                param.key = key;
                                param.tableid = tableid;
                                netMsgBack = NetMessage.Create(cmd,param,id);
                                return netMsgBack;
                            }
                        case "_db.snapshot.getvalue":
                            {
                                ISnapShot snapshot = peerSnapshots[peerid + netMsg.Param.snapid.ToString()];
                                var tableid = netMsg.Param.tableid;
                                var key = netMsg.Param.key;
                                var value = snapshot.GetValueData(tableid, key);
                                Param param = new Param();
                                param.snapid = snapshot.DataHeight;
                                param.result = true;
                                param.value = value;
                                param.key = key;
                                param.tableid = tableid;
                                netMsgBack = NetMessage.Create(cmd,param,id);
                                return netMsgBack;
                            }
                        case "_db.snapshot.newiterator":
                            {
                                ISnapShot snapshot = peerSnapshots[peerid+netMsg.Param.snapid.ToString()];
                                var beginKey = netMsg.Param.key;
                                var endKey = netMsg.Param.value;
                                var tableid = netMsg.Param.tableid;
                                var iter = snapshot.CreateKeyIterator(tableid, beginKey, endKey);
                                var itid = IteratorID;
                                peerKeyIterator[peerid+ itid.ToString()] = iter;
                                var p = new Param() { result = true,itid = itid};
                                netMsgBack = NetMessage.Create(cmd,p,id);
                                return netMsgBack;
                            }
                        case "_db.iterator.current":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[peerid+netMsg.Param.itid.ToString()];
                                var cur = keyIterator.Current;
                                Param param = new Param();
                                param.itid = netMsg.Param.itid;
                                param.result = true;
                                param.value = cur;
                                netMsgBack = NetMessage.Create(cmd, param, id);
                                return netMsgBack;
                            }
                        case "_db.iterator.next":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[peerid+netMsg.Param.itid.ToString()];
                                var result = keyIterator.MoveNext();
                                Param param = new Param();
                                param.itid = netMsg.Param.itid;
                                param.result = result;
                                netMsgBack = NetMessage.Create(cmd,param,id);
                                return netMsgBack;
                            }
                        case "_db.iterator.seektofirst":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[peerid + netMsg.Param.itid.ToString()];
                                keyIterator.SeekToFirst();
                                var p = new Param() { result = true ,itid = netMsg.Param.itid};
                                netMsgBack = NetMessage.Create(cmd,p,id);
                                return netMsgBack;
                            }
                        case "_db.iterator.reset":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[peerid + netMsg.Param.itid.ToString()];
                                keyIterator.Reset();
                                Param param = new Param();
                                param.result = true;
                                param.itid = netMsg.Param.itid;
                                netMsgBack = NetMessage.Create(cmd,param,id);
                                return netMsgBack;
                            }
                        default:
                            throw new Exception("unknown msg cmd:" + netMsg.Cmd);
                    }
                }
                catch (Exception e)
                {
                    netMsgBack = NetMessage.Create(cmd,new Param() { error = Encoding.UTF8.GetBytes(e.Message) },id);
                    return netMsgBack;
                }

            }
        }
    }
}
