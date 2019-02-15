using NEL.Common;
using NEL.Peer.Tcp;
using NEL.Pipeline;
using System;
using System.Net;

namespace NEL.Cli.ApiServer
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new Logger();
            var systemC = PipelineSystem.CreatePipelineSystemV1(logger);
            systemC.RegistModule("client", new RpcServerModule());
            systemC.OpenNetwork(new PeerOption());
            systemC.Start();

            Console.WriteLine("按任意键结束");
            Console.ReadLine();
        }
    }
}
