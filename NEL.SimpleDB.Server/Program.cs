using NEL.Common;
using NEL.Pipeline;
using NEL.Peer.Tcp;
using System.Net;
using System;

namespace NEL.SimpleDB.Server
{
    class Program
    {
        static void Main(string[] args)
        {
            //初始化配置
            new Setting();
            Logger logger = new Logger();
            //初始化db
            new StorageService();
            Console.WriteLine(StorageService.state_DBOpen);
            //开启服务
            ISystem systemL = PipelineSystem.CreatePipelineSystemV1(logger);
            systemL.OpenNetwork(new PeerOption());
            systemL.OpenListen(new IPEndPoint(IPAddress.Parse(Setting.BindAddress), Setting.Port));
            systemL.RegistModule("server",new ServerModule());
            systemL.Start();
            Console.ReadKey();
        }
    }
}
