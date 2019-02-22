using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Neo.Network.P2P.Payloads;
using Neo.Ledger;
using LightDB;
using Neo.IO;
using Neo;

namespace NEL.Simple.SDK
{
    public static class Protocol_UseSnapShot
    {
        public static NetMessage CreateSendMsg(string id)
        {
            NetMessage netMessage = NetMessage.Create("_db.usesnapshot",id);
            return netMessage;
        }

        public static UInt64 PraseRecvMsg(NetMessage netMessage)
        {
            return BitConverter.ToUInt64(netMessage.Params[0].value,0);
        }
    }

    public static class Protocol_CreateWriteBatch
    {
        public static NetMessage CreateSendMsg(string id)
        {
            NetMessage netMessage = NetMessage.Create("_db.CreateWriteBatch",id);
            return netMessage;
        }

        public static UInt64 PraseRecvMsg(NetMessage netMessage)
        {
            return BitConverter.ToUInt64(netMessage.Params[0].value,0);
        }
    }

    public static class Protocol_Put
    {
        public static NetMessage CreateSendMsg(string wbId)
        {
            NetMessage netMessage = NetMessage.Create("_db.put", wbId);
            return netMessage;
        }
    }

    public static class Protocol_Delete
    {
        public static NetMessage CreateSendMsg(string wbId)
        {
            NetMessage netMessage = NetMessage.Create("_db.delete", wbId);
            return netMessage;
        }
    }

    public static class Protocol_Write
    {
        public static NetMessage CreateSendMsg(string wbId)
        {
            NetMessage netMessage = NetMessage.Create("_db.write", wbId);
            return netMessage;
        }
    }

    public static class Protocol_CreateIterator
    {
        public static NetMessage CreateSendMsg(string snapshotId)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.newiterator", snapshotId);
            return netMessage;
        }

        public static UInt64 PraseRecvMsg(NetMessage netMessage)
        {
            return BitConverter.ToUInt64(netMessage.Params[0].value,0);
        }
    }

    public static class Protocol_IteratorCurrent
    {
        public static NetMessage CreateSendMsg(string iteratorId)
        {
            NetMessage netMessage = NetMessage.Create("_db.iterator.current", iteratorId);
            return netMessage;
        }

        public static byte[] PraseRecvMsg(NetMessage netMessage)
        {
            return netMessage.Params[0].value;
        }
    }

    public static class Protocol_IteratorNext
    {
        public static NetMessage CreateSendMsg(string iteratorId)
        {
            NetMessage netMessage = NetMessage.Create("_db.iterator.next", iteratorId);
            return netMessage;
        }
    }

    public static class Protocol_IteratorReset
    {
        public static NetMessage CreateSendMsg(string iteratorId)
        {
            NetMessage netMessage = NetMessage.Create("_db.iterator.reset", iteratorId);
            return netMessage;
        }
    }


    public static class Protocol_GetValue
    {
        public static NetMessage CreateSendMsg(byte[] key,string snapshotId,bool useServerSnap =false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", snapshotId);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", snapshotId);
            Param param = new Param() { tableid = new byte[] { }, key = key };
            netMessage.AddParam(param);
            return netMessage;
        }

        public static NetMessage CreateSendMsg(byte[][] keys, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            for (var i = 0; i < keys.Length; i++)
            {
                Param param = new Param() { tableid = new byte[] { }, key = keys[i] };
                netMessage.AddParam(param);
            }
            return netMessage;
        }

        public static byte[][] PraseRecvMsg(NetMessage msg)
        {
            return msg.Params.Select(p => p.value).ToArray();
        }
    }


    public static class Protocol_GetStorage
    {
        public class message
        {
            public byte[] key;
            public byte[] value;
        }

        public static NetMessage CreateSendMsg(byte[] key, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            Param param = new Param() { tableid = new byte[] { }, key = (new byte[] { Prefixes.ST_Storage }).Concat(key).ToArray() };
            netMessage.AddParam(param);
            return netMessage;
        }

        public static NetMessage CreateSendMsg(byte[][] keys, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            for (var i = 0;i< keys.Length; i++)
            {
                Param param = new Param() { tableid = new byte[] { }, key = (new byte[] { Prefixes.ST_Storage }).Concat(keys[i]).ToArray() };
                netMessage.AddParam(param);
            }
            return netMessage;
        }

        public static message[] PraseRecvMsg(NetMessage msg)
        {
            return msg.Params.Select(p => new message() { key =p.key, value = p.value }).ToArray();
        }
    }

    public static class Protocol_GetBlock
    {
        public static NetMessage CreateSendMsg(byte[] key, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            Param param = new Param() { tableid = new byte[] { }, key = (new byte[] { Prefixes.DATA_Block }).Concat(key).ToArray() };
            netMessage.AddParam(param);
            return netMessage;
        }

        public static NetMessage CreateSendMsg(byte[][] keys, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            for (var i = 0; i < keys.Length; i++)
            {
                Param param = new Param() { tableid = new byte[] { }, key = (new byte[] { Prefixes.DATA_Block }).Concat(keys[i]).ToArray() };
                netMessage.AddParam(param);
            }
            return netMessage;
        }

        public static BlockState[] PraseRecvMsg(NetMessage msg)
        {
            return msg.Params.Select(p=> DBValue.FromRaw(p.value).value.AsSerializable<BlockState>()).ToArray();
        }
    }

    public static class Protocol_GetTransaction
    {
        public static NetMessage CreateSendMsg(UInt256 key, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            Param param = new Param() { tableid = new byte[] { }, key = (new byte[] { Prefixes.DATA_Transaction }).Concat(key.ToArray()).ToArray() };
            netMessage.AddParam(param);
            return netMessage;
        }

        public static NetMessage CreateSendMsg(UInt256[] keys, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            for (var i = 0; i < keys.Length; i++)
            {
                Param param = new Param() { tableid = new byte[] { }, key = (new byte[] { Prefixes.DATA_Transaction }).Concat(keys[i].ToArray()).ToArray() };
                netMessage.AddParam(param);
            }
            return netMessage;
        }

        public static Transaction[] PraseRecvMsg(NetMessage msg)
        {
            return msg.Params.Select(p => DBValue.FromRaw(p.value).value.AsSerializable<TransactionState>().Transaction).ToArray();
        }
    }
}
