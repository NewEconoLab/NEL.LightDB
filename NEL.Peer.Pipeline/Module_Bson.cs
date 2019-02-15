using NEL.Pipeline;
using MsgPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MsgPack;
namespace NEL.Pipeline.NewtonsoftBson
{
    public static class Bson_Helper
    {
        public static byte[] Pack(Newtonsoft.Json.Linq.JToken json)
        {

            using (var ms = new System.IO.MemoryStream())
            {
                using (var bswrite = new Newtonsoft.Json.Bson.BsonWriter(ms))
                {
                    json.WriteTo(bswrite);

                    var bytes = ms.ToArray();
                    return bytes;
                }
            }
        }

        public static Newtonsoft.Json.Linq.JToken UnPack(byte[] bytes)
        {
            using (var ms = new System.IO.MemoryStream(bytes))
            {
                using (var bsreader = new Newtonsoft.Json.Bson.BsonReader(ms))
                {
                    return Newtonsoft.Json.Linq.JToken.ReadFrom(bsreader);
                }
            }
        }
        public static void Tell(this IModulePipeline pipeline, Newtonsoft.Json.Linq.JToken json)
        {
            if (pipeline.IsLocal)
            {
                pipeline.TellLocalObj(json);
            }
            else
            {
                pipeline.Tell(Pack(json).ToArray());
            }
        }
    }
    public abstract class Module_Bson : Module
    {
        public Module_Bson(bool MultiThreadTell = true) : base(MultiThreadTell)
        {
        }
        //public abstract void OnStart();
        public abstract void OnTell(IModulePipeline from, Newtonsoft.Json.Linq.JToken json);
        public sealed override void OnTell(IModulePipeline from, byte[] data)
        {
            this.OnTell(from, Bson_Helper.UnPack(data));

        }
        public sealed override void OnTellLocalObj(IModulePipeline from, object obj)
        {
            this.OnTell(from, obj as Newtonsoft.Json.Linq.JToken);
        }
    }

}
