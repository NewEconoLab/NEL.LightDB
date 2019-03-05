using NEL.Simple.SDK;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Concurrent;

namespace NEL.SimpleDB.Server
{
    public class Server
    {
        public ConcurrentDictionary<UInt64, ISnapShot> peerSnapshots = new ConcurrentDictionary<UInt64, ISnapShot>();
        public ConcurrentDictionary<UInt64, IWriteBatch> peerWriteBatch = new ConcurrentDictionary<UInt64, IWriteBatch>();
        public ConcurrentDictionary<UInt64, IKeyIterator> peerKeyIterator = new ConcurrentDictionary<UInt64, IKeyIterator>();

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
                try
                {
                    switch (cmd)
                    {
                        case "_db.usesnapshot":
                            {
                                var snapshot = StorageService.maindb.UseSnapShot();
                                peerSnapshots[snapshot.DataHeight] = StorageService.maindb.UseSnapShot();
                                var p = new Param() { result = true, snapid = snapshot.DataHeight };
                                netMsgBack = NetMessage.Create(cmd,p,id);
                                return netMsgBack;
                            }
                        case "_db.disposeSnapshot":
                            {
                                ISnapShot snapshot;
                                peerSnapshots.TryRemove(netMsg.Param.snapid, out snapshot);
                                snapshot.Dispose();
                                var p = new Param() { result = true};
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.CreateWriteBatch":
                            {
                                var wb = StorageService.maindb.CreateWriteBatch();
                                peerWriteBatch[wb.Wbid] = wb;
                                var p = new Param() { result = true ,wbid = wb.Wbid };
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.put":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[netMsg.Param.wbid];
                                writeBatch.Put(netMsg.Param.tableid, netMsg.Param.key, netMsg.Param.value);
                                var p = new Param() { result = true };
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.delete":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[netMsg.Param.wbid];
                                writeBatch.Delete(netMsg.Param.tableid, netMsg.Param.key);
                                var p = new Param() { result = true };
                                netMsgBack = NetMessage.Create(cmd, p, id);
                                return netMsgBack;
                            }
                        case "_db.write":
                            {
                                IWriteBatch writeBatch = peerWriteBatch[netMsg.Param.wbid];
                                var p = new Param() { result = false };
                                if (writeBatch.wbcount > 0)
                                {
                                    StorageService.maindb.WriteBatch(writeBatch);
                                    peerWriteBatch.Remove(netMsg.Param.wbid, out writeBatch);
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
                                ISnapShot snapshot = peerSnapshots[netMsg.Param.snapid];
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
                                ISnapShot snapshot = peerSnapshots[netMsg.Param.snapid];
                                var beginKey = netMsg.Param.key;
                                var endKey = netMsg.Param.value;
                                var tableid = netMsg.Param.tableid;
                                var iter = snapshot.CreateKeyIterator(tableid, beginKey, endKey);
                                var itid = iter.HandleID;
                                peerKeyIterator[itid] = iter;
                                var p = new Param() { result = true,itid = itid,snapid = netMsg.Param.snapid};
                                netMsgBack = NetMessage.Create(cmd,p,id);
                                return netMsgBack;
                            }
                        case "_db.iterator.current":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[netMsg.Param.itid];
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
                                IKeyIterator keyIterator = peerKeyIterator[netMsg.Param.itid];
                                var result = keyIterator.MoveNext();
                                Param param = new Param();
                                param.itid = netMsg.Param.itid;
                                param.result = result;
                                netMsgBack = NetMessage.Create(cmd,param,id);
                                return netMsgBack;
                            }
                        case "_db.iterator.seektofirst":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[netMsg.Param.itid];
                                keyIterator.SeekToFirst();
                                var p = new Param() { result = true ,itid = netMsg.Param.itid};
                                netMsgBack = NetMessage.Create(cmd,p,id);
                                return netMsgBack;
                            }
                        case "_db.iterator.reset":
                            {
                                IKeyIterator keyIterator = peerKeyIterator[netMsg.Param.itid];
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
