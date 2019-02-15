using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace NEL.Simple.SDK
{
    public static class Protocol_GetStorage
    {
        public class message
        {
            public byte[] key;
            public byte[] value;
        }

        public static NetMessage CreateSendMsg(byte[] key,string id)
        {
            NetMessage netMessage = NetMessage.Create("_db.snapshot.getvalue", id);
            netMessage.Params["tableid"] = new byte[] { };
            netMessage.Params["key"] = (new byte[] { Prefixes.ST_Storage }).Concat(key).ToArray();
            return netMessage;
        }

        public static message PraseRecvMsg(NetMessage msg)
        {
            message message = new message();
            message.key = msg.Params["key"];
            message.value = msg.Params["value"];
            return message;
        }
    }
}
