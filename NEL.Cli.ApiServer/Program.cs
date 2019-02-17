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

            bool loop = true;
            while (loop)
            {
                var str = Console.ReadLine();
                string[] cmds = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                loop = ProcessCommand(cmds);
                System.Threading.Tasks.Task.Delay(1);
            }
        }

        static bool ProcessCommand(string[] cmds)
        {
            var command = cmds[0].ToLower();
            switch (command)
            {
                case "exit":
                    return false;
                case "help":
                default:
                    return ShowHelpCommand();
            }
        }

        static bool ShowHelpCommand()
        {
            Console.WriteLine("exit");
            return true;
        }
    }
}
