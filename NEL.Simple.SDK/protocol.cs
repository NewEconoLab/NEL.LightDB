using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Neo.Network.P2P.Payloads;
using Neo.Ledger;
using LightDB;
using Neo.IO;

namespace NEL.Simple.SDK
{
    public static class Protocol_GetStorage
    {
        public class message
        {
            public byte[] key;
            public byte[] value;
        }

        public static NetMessage CreateSendMsg(byte[] tableid,byte[] key, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            Param param = new Param() { tableid = tableid, key = (new byte[] { Prefixes.ST_Storage }).Concat(key).ToArray() };
            netMessage.AddParam(param);
            return netMessage;
        }

        public static message PraseRecvMsg(NetMessage msg)
        {
            //message[] messages = new message[msg.Params.Length];
            //for (var i = 0; i < msg.Params.Length; i++)
            //{
            //    messages[i] = new message() { key = msg.Params[i].key,value = msg.Params[i].value};
            //}
            //return messages;

            //只有第一个
            var value = DBValue.FromRaw(msg.Params[0].value).value.AsSerializable<StorageItem>().Value;
            return new message() { key = msg.Params[0].key, value = value };
        }
    }

    public static class Protocol_GetBlock
    {
        public static NetMessage CreateSendMsg(byte[] tableid, byte[] key, string id, bool useServerSnap = false)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            if (useServerSnap)
                netMessage = NetMessage.Create("_db.getvalue", id);
            Param param = new Param() { tableid = tableid, key = (new byte[] { Prefixes.DATA_Block }).Concat(key).ToArray() };
            netMessage.AddParam(param);
            return netMessage;
        }

        public static BlockState PraseRecvMsg(NetMessage msg)
        {
            var blockstate = DBValue.FromRaw(msg.Params[0].value).value.AsSerializable<BlockState>();
            return blockstate;
        }
    }
}
