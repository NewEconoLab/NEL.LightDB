using System;
using System.Collections.Generic;

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

        public string Fmd
        {
            get;
            private set;
        }

        public string Host
        {
            get;
            private set;
        }

        public string id
        {
            get;
            private set;
        }

        public Dictionary<string, byte[]> Params
        {
            get;
            private set;
        }

        public static NetMessage Create(string cmd,string fmd="",string host = "",string id = "")
        {
            var msg = new NetMessage();
            msg.Cmd = cmd;
            msg.Fmd = fmd;
            msg.Host = host;
            msg.id = id;
            msg.Params = new Dictionary<string, byte[]>();
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
            var strbuf = System.Text.Encoding.UTF8.GetBytes(this.Cmd);
            var fmdbuf = System.Text.Encoding.UTF8.GetBytes(this.Fmd);
            var hostbuf = System.Text.Encoding.UTF8.GetBytes(this.Host);
            var idbuf = System.Text.Encoding.UTF8.GetBytes(this.id);
            if (strbuf.Length > 255)
                throw new Exception("too long cmd.");
            if (Params.Count > 255)
                throw new Exception("too mush params.");

            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                //emit msg
                {
                    ms.WriteByte((byte)strbuf.Length);
                    ms.Write(strbuf, 0, strbuf.Length);

                    ms.WriteByte((byte)fmdbuf.Length);
                    ms.Write(fmdbuf, 0, fmdbuf.Length);

                    ms.WriteByte((byte)hostbuf.Length);
                    ms.Write(hostbuf, 0, hostbuf.Length);

                    ms.WriteByte((byte)idbuf.Length);
                    ms.Write(idbuf, 0, idbuf.Length);

                    ms.WriteByte((byte)this.Params.Count);
                    foreach (var item in Params)
                    {
                        var keybuf = System.Text.Encoding.UTF8.GetBytes(item.Key);
                        var data = item.Value;
                        var datalenbuf = BitConverter.GetBytes((UInt32)data.Length);
                        ms.WriteByte((byte)keybuf.Length);
                        ms.Write(keybuf, 0, keybuf.Length);
                        ms.Write(datalenbuf, 0, 4);
                        ms.Write(data, 0, data.Length);
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
                var cl = stream.ReadByte();
                var strbuf = new byte[cl];
                stream.Read(strbuf, 0, cl);
                msg.Cmd = System.Text.Encoding.UTF8.GetString(strbuf);

                var fl = stream.ReadByte();
                var fmdbuf = new byte[fl];
                stream.Read(fmdbuf, 0, fl);
                msg.Fmd = System.Text.Encoding.UTF8.GetString(fmdbuf);

                var sl = stream.ReadByte();
                var hostbuf = new byte[sl];
                stream.Read(hostbuf, 0, sl);
                msg.Host = System.Text.Encoding.UTF8.GetString(hostbuf);

                var il = stream.ReadByte();
                var idbuf = new byte[il];
                stream.Read(idbuf, 0, il);
                msg.id = System.Text.Encoding.UTF8.GetString(idbuf);

                msg.Params = new Dictionary<string, byte[]>();
                var pcount = stream.ReadByte();
                for (var i = 0; i < pcount; i++)
                {
                    var keylen = stream.ReadByte();
                    var keybuf = new byte[keylen];
                    stream.Read(keybuf, 0, keylen);
                    var key = System.Text.Encoding.UTF8.GetString(keybuf);
                    var datalenbuf = new byte[4];
                    stream.Read(datalenbuf, 0, 4);
                    var datalen = BitConverter.ToUInt32(datalenbuf, 0);
                    var data = new byte[datalen];
                    stream.Read(data, 0, (int)datalen);
                    msg.Params[key] = data;
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
}
