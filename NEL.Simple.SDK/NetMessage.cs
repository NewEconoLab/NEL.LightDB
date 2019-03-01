using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;

namespace NEL.Simple.SDK
{
    public class NetMessage
    {
        private NetMessage()
        {

        }

        public string Cmd
        {
            get;
            private set;
        }

        public string ID //加一个id 用来附带发送者的一些身份信息
        {
            get;
            private set;
        }

        public Param Param
        {
            get;
            private set;
        }

        public static NetMessage Create(string cmd,Param _p = null,string identity = "")
        {
            var msg = new NetMessage();
            msg.Cmd = cmd;
            msg.ID = identity;
            msg.Param = _p?? new Param();
            return msg;
        }

        public byte[] ToBytes()
        {
            using (var ms = new System.IO.MemoryStream())
            {
                this.Pack(ms);
                return ms.ToArray();
            }
        }

        public void Pack(System.IO.Stream stream)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                //emit msg
                {
                    if (this.Cmd == null)
                    {
                        ms.WriteByte((byte)0);
                    }
                    else
                    {
                        ms.WriteByte((byte)1);
                        var strbuf = System.Text.Encoding.UTF8.GetBytes(this.Cmd);
                        ms.WriteByte((byte)strbuf.Length);
                        ms.Write(strbuf, 0, strbuf.Length);
                        if (strbuf.Length > 255)
                            throw new Exception("too long cmd.");
                    }

                    if (this.ID == null)
                    {
                        ms.WriteByte((byte)0);
                    }
                    else
                    {
                        var idbuf = System.Text.Encoding.UTF8.GetBytes(this.ID);
                        ms.WriteByte((byte)1);
                        ms.WriteByte((byte)idbuf.Length);
                        ms.Write(idbuf, 0, idbuf.Length);
                    }

                    if (this.Param == null)
                    {
                        ms.WriteByte((byte)0);
                    }
                    else
                    {
                        ms.WriteByte((byte)1);
                        var itembuf = Param.Serialize(Param);
                        //stream.Write(BitConverter.GetBytes(itembuf.Length), 0, 4);
                        ms.Write(itembuf, 0, itembuf.Length);
                    }
                }
                var len = (UInt32)ms.Length;
                stream.Write(BitConverter.GetBytes(len), 0, 4);
                var msgdata = ms.ToArray();
                stream.Write(msgdata, 0, msgdata.Length);
            }
        }

        public static NetMessage Unpack(System.IO.Stream stream)
        {
            var msglenbuf = new byte[4];
            stream.Read(msglenbuf, 0, 4);
            UInt32 msglen = BitConverter.ToUInt32(msglenbuf, 0);
            var posstart = stream.Position;
            NetMessage msg = new NetMessage();
            {//read msg
                var isnull = stream.ReadByte();
                if (isnull != 0)
                {
                    var cl = stream.ReadByte();
                    var strbuf = new byte[cl];
                    stream.Read(strbuf, 0, cl);
                    msg.Cmd = System.Text.Encoding.UTF8.GetString(strbuf);
                }
                else
                {
                    msg.Cmd = null;
                }

                isnull = stream.ReadByte();
                if (isnull != 0)
                {
                    var il = stream.ReadByte();
                    var idbuf = new byte[il];
                    stream.Read(idbuf, 0, il);
                    msg.ID = System.Text.Encoding.UTF8.GetString(idbuf);
                }
                else
                {
                    msg.ID = null;
                }

                isnull = stream.ReadByte();
                if (isnull != 0)
                {
                    msg.Param = Param.Deserialize(stream);
                }
                else
                {
                    msg.Param = null;
                }

            }
            var posend = stream.Position;
            if (posend - posstart != msglen)
            {
                throw new Exception("bad msg.");
            }
            return msg;
        }
    }

    public class Param
    {
        public UInt64 snapid = 0;
        public UInt64 wbid = 0;
        public UInt64 itid = 0;
        public byte[] tableid = new byte[] { };
        public byte[] key = new byte[] { };
        public byte[] value = new byte[] { };
        public bool result = false; //查询的结果
        public byte[] error = new byte[] { }; //可能出现的错误信息

        public static byte[] Serialize(Param _param)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                //写入snapid
                if (_param.snapid == null)
                {
                    ms.WriteByte((byte)0);

                }
                else
                {
                    ms.WriteByte((byte)1);
                    var bytes = BitConverter.GetBytes(_param.snapid);
                    ms.WriteByte((byte)bytes.Length);
                    ms.Write(bytes, 0, bytes.Length);
                }

                //写入wbid
                if (_param.wbid == null)
                {
                    ms.WriteByte(0);
                }
                else
                {
                    ms.WriteByte(1);
                    var bytes = BitConverter.GetBytes(_param.wbid);
                    ms.WriteByte((byte)bytes.Length);
                    ms.Write(bytes, 0, bytes.Length);
                }

                //写入itid
                if (_param.itid == null)
                {
                    ms.WriteByte(0);
                }
                else
                {
                    ms.WriteByte(1);
                    var bytes = BitConverter.GetBytes(_param.itid);
                    ms.WriteByte((byte)bytes.Length);
                    ms.Write(bytes, 0, bytes.Length);
                }

                //写入tableid
                if (_param.tableid == null)
                {
                    ms.WriteByte(0);
                }
                else
                {
                    ms.WriteByte(1);
                    ms.WriteByte((byte)_param.tableid.Length);
                    ms.Write(_param.tableid, 0, _param.tableid.Length);
                }

                //写入key
                if (_param.key == null)
                {
                    ms.WriteByte(0);
                }
                else
                {
                    ms.WriteByte(1);
                    ms.WriteByte((byte)_param.key.Length);
                    ms.Write(_param.key, 0, _param.key.Length);
                }
                //写入value  value的最大长度是 UInt32
                if (_param.value == null)
                {
                    ms.WriteByte(0);
                }
                else
                {
                    ms.WriteByte(1);
                    var valuelenbuf = BitConverter.GetBytes((UInt32)_param.value.Length);
                    ms.Write(valuelenbuf, 0, 4);
                    ms.Write(_param.value, 0, _param.value.Length);
                }

                //写入result
                if (_param.result == null)
                {
                    ms.WriteByte(0);
                }
                else
                {
                    ms.WriteByte(1);
                    byte[] _result = BitConverter.GetBytes(_param.result);
                    ms.WriteByte((byte)_result.Length);
                    ms.Write(_result, 0, _result.Length);
                }
                //写入error 最大长度是 UInt32
                if (_param.error == null)
                {
                    ms.WriteByte(0);
                }
                else
                {
                    ms.WriteByte(1);
                    var errorlenbuf = BitConverter.GetBytes((UInt32)_param.error.Length);
                    ms.Write(errorlenbuf, 0, 4);
                    ms.Write(_param.error, 0, _param.error.Length);
                }

                return ms.ToArray();
            }
        }

        public static Param Deserialize(System.IO.Stream stream)
        {
            Param param = new Param();
            //snapid
            var isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var snapidlen = stream.ReadByte();
                var snapidbuf = new byte[snapidlen];
                stream.Read(snapidbuf, 0, snapidlen);
                param.snapid = BitConverter.ToUInt64(snapidbuf, 0);
            }
            else
            {//不可能为null
            }

            //wbid
            isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var wbidlen = stream.ReadByte();
                var wbidbuf = new byte[wbidlen];
                stream.Read(wbidbuf, 0, wbidlen);
                param.wbid = BitConverter.ToUInt64(wbidbuf, 0);
            }
            else
            {
            }
            //itid
            isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var itidlen = stream.ReadByte();
                var itidbuf = new byte[itidlen];
                stream.Read(itidbuf, 0, itidlen);
                param.itid = BitConverter.ToUInt64(itidbuf, 0);
            }
            else
            {
            }

            //tableid
            isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var tableidlen = stream.ReadByte();
                var tableidbuf = new byte[tableidlen];
                stream.Read(tableidbuf, 0, tableidlen);
                param.tableid = tableidbuf;
            }
            else
            {
                param.tableid = null;
            }
            //key
            isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var keylen = stream.ReadByte();
                var keybuf = new byte[keylen];
                stream.Read(keybuf, 0, keylen);
                param.key = keybuf;
            }
            else
            {
                param.key = null;
            }

            //value value的长度定义的是四字节
            isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var valuelenbuf = new byte[4];
                stream.Read(valuelenbuf, 0, 4);
                var valuelen = BitConverter.ToUInt32(valuelenbuf, 0);
                var valuebuf = new byte[valuelen];
                stream.Read(valuebuf, 0, (int)valuelen);
                param.value = valuebuf;
            }
            else
            {
                param.value = null;
            }

            //result
            isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var resultlen = stream.ReadByte();
                var resultbuf = new byte[resultlen];
                stream.Read(resultbuf, 0, resultlen);
                param.result = BitConverter.ToBoolean(resultbuf, 0);
            }
            else
            {//bool类型不可能为null

            }

            //error
            isnull = stream.ReadByte();
            if (isnull != 0)
            {
                var errorlenbuf = new byte[4];
                stream.Read(errorlenbuf, 0, 4);
                var errorlen = BitConverter.ToUInt32(errorlenbuf, 0);
                var errorbuf = new byte[errorlen];
                stream.Read(errorbuf, 0, (int)errorlen);
                param.error = errorbuf;
            }
            else
            {
                param.error = null;
            }

            return param;
        }
    }
}
