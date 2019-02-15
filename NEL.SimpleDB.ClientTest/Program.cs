using System;
using NEL.Pipeline.MsgPack;
using NEL.Pipeline;
using NEL.Common;
using MsgPack;
using NEL.Peer.Tcp;

namespace NEL.SimpleDB.ClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            ISystem system = PipelineSystem.CreatePipelineSystemV1(new Logger());
            system.RegistModule("mainloop", new Module_Loop());
            system.Start();
            var pipe = system.GetPipeline(null, "this/mainloop");
            while (pipe.IsVaild)
            {
                System.Threading.Thread.Sleep(100);
            }
        }
    }

    class Module_Loop : Module
    {
        public override void Dispose()
        {
            base.Dispose();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override void OnStart()
        {
            //不要堵死OnStart函數
            System.Threading.ThreadPool.QueueUserWorkItem((s) =>
            {
                TestLoop();
            });
        }

        public override void OnTell(IModulePipeline from, byte[] data)
        {

        }

        async void TestLoop()
        {
            while (true)
            {
                Console.Write(">");
                var line = Console.ReadLine();
                if (line == "1")
                {
                    await Test1.Test();//這個測試創建兩個本地actor，并讓他們通訊
                }
                else if (line == "exit")
                {
                    this.Dispose();//這將會導致這個模塊關閉
                    break;
                }
            }
        }

        public override void OnTellLocalObj(IModulePipeline from, object obj)
        {
            throw new NotImplementedException();
        }
    }
}
