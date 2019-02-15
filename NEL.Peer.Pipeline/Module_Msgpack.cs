using NEL.Pipeline;
using MsgPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MsgPack;
namespace NEL.Pipeline.MsgPack
{
    public static class MsgPack_Helper
    {
        public static ArraySegment<byte> Pack(MessagePackObject? obj)
        {
            using (var pack = Packer.Create(new byte[256], true, PackerCompatibilityOptions.None))
            {
                obj.Value.PackToMessage(pack, null);
                return pack.GetResultBytes();
            }
        }
        public static MessagePackObject UnPack(ArraySegment<byte> bytes)
        {
            using (var unpack = Unpacker.Create(bytes.Array, bytes.Offset))
            {
                if (unpack.ReadObject(out MessagePackObject obj))
                {
                    return obj;
                }
                return MessagePackObject.Nil;
            }
        }
        public static MessagePackObject UnPack(byte[] bytes)
        {
            using (var unpack = Unpacker.Create(bytes))
            {
                if (unpack.ReadObject(out MessagePackObject obj))
                {
                    return obj;
                }
                return MessagePackObject.Nil;
            }
        }
        public static void Tell(this IModulePipeline pipeline, MessagePackObject? obj)
        {
            if (pipeline.IsLocal)
            {
                pipeline.TellLocalObj(obj);
            }
            else
            {
                pipeline.Tell(Pack(obj).ToArray());
             }
        }
    }
    public abstract class Module_MsgPack : Module
    {
        public Module_MsgPack(bool MultiThreadTell = true) : base(MultiThreadTell)
        {
        }
        //public abstract void OnStart();
        public abstract void OnTell(IModulePipeline from, MessagePackObject? obj);
        public sealed override void OnTell(IModulePipeline from, byte[] data)
        {
            this.OnTell(from, MsgPack_Helper.UnPack(data));
           
        }
        public sealed override void OnTellLocalObj(IModulePipeline from, object obj)
        {
            this.OnTell(from, obj as MessagePackObject?);
        }
    }

}
