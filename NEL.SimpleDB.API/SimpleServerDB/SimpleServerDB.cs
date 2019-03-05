using NEL.Common;
using NEL.Peer.Tcp;
using NEL.Pipeline;
using System.Linq;
using System;
using NEL.Simple.SDK;
using Neo.IO;
using Neo;
using System.Collections.Generic;
using System.IO;
using NEL.Simple.SDK.DataCache;

namespace NEL.SimpleDB.API.DB
{
    public class SimpleServerDB: Module,IDisposable
    {
        public IModulePipeline actor;
        //private ISystem systemC;
        private Setting setting;

        public static DataCache_QueueValue<string, byte[]> K_V= new DataCache_QueueValue<string, byte[]>();
        public static DataCache_QueueValue<UInt64, UInt64> SID_ItId = new DataCache_QueueValue<ulong, ulong>();
        public static DataCache_QueueValue<UInt64, byte[]> ItID_Cur = new DataCache_QueueValue<ulong, byte[]>();
        public static DataCache_QueueValue<UInt64, bool> ItID_Next = new DataCache_QueueValue<ulong, bool>();
        public static DataCache_QueueValue<UInt64, bool> ItID_STF = new DataCache_QueueValue<ulong, bool>();
        public static DataCache_QueueValue<string, UInt64> Utc_SID = new DataCache_QueueValue<string, ulong>();
        //public static Dictionary<string, DataCache<byte[], byte[]>> cache = new Dictionary<string, DataCache<byte[], byte[]>>();


        public SimpleServerDB(Setting _setting)
        {
            setting = _setting;
        }


        public static SimpleServerDB Open(Setting setting)
        {
            var logger = new Logger();
            var systemC = PipelineSystem.CreatePipelineSystemV1(logger);
            var db = new SimpleServerDB(setting);
            systemC.RegistModule("client", db);
            systemC.OpenNetwork(new PeerOption());
            systemC.Start();
            db.actor = systemC.GetPipeline(null,"this/client");
            return db;
        }

        public UInt64 UseSnapshot()
        {
            NetMessage netMessage = Protocol_UseSnapShot.CreateSendMsg(DateTime.UtcNow.ToString());
            actor.Tell(netMessage.ToBytes());
            return Utc_SID.Get(netMessage.ID).Result;
        }

        public T Get<T>(byte[] tableid,byte[] key = null) where T : class, ISerializable,new()
        {
            NetMessage netMessage =  Protocol_GetValue.CreateSendMsg(tableid,key,"",0,true);
            actor.Tell(netMessage.ToBytes());
            var str = key == null ? tableid.ToHexString() : tableid.Concat(key).ToArray().ToHexString();
            var value = K_V.Get(str).Result;
            if (value == null || value.Length == 0)
                return null;
            var a = value.AsSerializable<T>();
            return a;
        }

        public byte[] Get(byte[] tableid, byte[] key)
        {
            NetMessage netMessage = Protocol_GetValue.CreateSendMsg(tableid, key, "", 0, true);
            actor.Tell(netMessage.ToBytes());
            return K_V.Get(tableid.Concat(key).ToArray().ToHexString()).Result;
        }

        public UInt64 CreateIterator(UInt64 snapid,byte[] tableid,byte[] beginKey = null,byte[] endKey = null)
        {
            NetMessage netMessage = Protocol_CreateIterator.CreateSendMsg(snapid, tableid, beginKey,endKey);
            actor.Tell(netMessage.ToBytes());
            return SID_ItId.Get(snapid).Result;
        }

        public byte[] Current(UInt64 itid)
        {
            NetMessage netMessage = Protocol_IteratorCurrent.CreateSendMsg(itid);
            actor.Tell(netMessage.ToBytes());
            return ItID_Cur.Get(itid).Result;
        }

        public bool MoveToNext(UInt64 itid)
        {
            NetMessage netMessage = Protocol_IteratorNext.CreateSendMsg(itid);
            actor.Tell(netMessage.ToBytes());
            return ItID_Next.Get(itid).Result;
        }

        public bool SeekToFirst(UInt64 itid)
        {
            NetMessage netMessage = Protocol_IteratorSeekToFirst.CreateSendMsg(itid);
            actor.Tell(netMessage.ToBytes());
            return ItID_STF.Get(itid).Result;
        }

        public override void Dispose()
        {
            K_V = null;
            SID_ItId = null;
            //systemC.Dispose();
        }

        public override void OnStart()
        {
        }

        public override void OnTell(IModulePipeline from, byte[] data)
        {
            if (from == null)//来自自己
            {
                var actor = this.GetPipeline(string.Format("{0}:{1}/{2}", setting.DBServerAddress, setting.DBServerPort, setting.DBServerPath));
                actor.Tell(data);
            }

            if (from != null && from.system.Remote != null)//来自server
            {
                using (MemoryStream ms = new MemoryStream(data))
                {
                    NetMessage netMsg = NetMessage.Unpack(ms);
                    if (netMsg.Cmd == "_db.usesnapshot")
                    {
                        UInt64 sid = Protocol_UseSnapShot.PraseRecvMsg(netMsg);
                        Utc_SID.Add(netMsg.ID, sid);
                    }
                    if (netMsg.Cmd == "_db.getvalue")
                    {
                        byte[] value = Protocol_GetValue.PraseRecvMsg(netMsg);
                        var str = netMsg.Param.key == null ? netMsg.Param.tableid.ToHexString() : netMsg.Param.tableid.Concat(netMsg.Param.key).ToArray().ToHexString();
                        K_V.Add(str, value);
                    }
                    if (netMsg.Cmd == "_db.snapshot.newiterator")
                    {
                        UInt64 itid = Protocol_CreateIterator.PraseRecvMsg(netMsg);
                        SID_ItId.Add(netMsg.Param.snapid, itid);
                    }
                    if (netMsg.Cmd == "_db.iterator.current")
                    {
                        byte[] value = Protocol_IteratorCurrent.PraseRecvMsg(netMsg);
                        ItID_Cur.Add(netMsg.Param.itid, value);
                    }
                    if (netMsg.Cmd == "_db.iterator.next")
                    {
                        bool result = Protocol_IteratorNext.PraseRecvMsg(netMsg);
                        ItID_Next.Add(netMsg.Param.itid, result);
                    }
                    if (netMsg.Cmd == "_db.iterator.seektofirst")
                    {
                        bool result = Protocol_IteratorSeekToFirst.PraseRecvMsg(netMsg);
                        ItID_STF.Add(netMsg.Param.itid, result);
                    }
                }
            }
        }

        public override void OnTellLocalObj(IModulePipeline from, object obj)
        {
        }
    }
}
