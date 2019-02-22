using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NEL.Peer.Tcp.PeerV2
{
    enum LogType
    {
        Message,
        Warning,
        Error
    }

    partial class ServerModule : NEL.Peer.Tcp.IPeer
    {

        public static UInt64 moduleID = 0;
        public string Ver => "PeerV2 0.1";

        /// <summary>  
        /// 监听Socket，用于接受客户端的连接请求  
        /// </summary>  
        private Socket socketListen;

        public event Action<ulong, IPEndPoint> OnAccepted;
        public event Action<ulong, IPEndPoint> OnConnected;
        public event Action<ulong, Exception> OnLinkError;
        public event Action<ulong, byte[]> OnRecv;
        public event Action<ulong> OnClosed;

        public NEL.Common.ILogger logger;

        public UInt64 ID
        {
            get;
            private set;
        }
        public ServerModule(NEL.Common.ILogger logger)
        {
            this.ID = moduleID++;
            this.logger = logger;
        }
        NEL.Peer.Tcp.PeerOption option;
        public void Start(NEL.Peer.Tcp.PeerOption option)
        {

            logger.Info("Module Start==");
            this.option = option;
            InitPools();
            InitProcess();

            logger.Info("==Module Start");
        }
        public void Close()
        {
            this.OnAccepted = null;
            this.OnConnected = null;
            this.OnLinkError = null;
            this.OnRecv = null;
            this.OnClosed = null;
        }
        public void Dispose()
        {
            this.Close();
        }
        //监听
        public void Listen(IPEndPoint endPoint)
        {
            if (this.OnAccepted == null)
            {
                throw new Exception("need set event OnAccepted");
            }
            if (this.OnClosed == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnRecv == null)
            {
                throw new Exception("need set event OnRecv");
            }
            logger.Warn("Module listen==" + endPoint.ToString());

            if (this.socketListen != null)
            {
                throw new Exception("already in listen");
            }
            socketListen = new Socket(SocketType.Stream, ProtocolType.Tcp);
            if (endPoint.AddressFamily == AddressFamily.InterNetworkV6)
            {
                socketListen.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                socketListen.Bind(new IPEndPoint(IPAddress.IPv6Any, endPoint.Port));
            }
            else
            {
                socketListen.Bind(endPoint);
            }
            socketListen.Listen(10000);

            StartAccept(null);

            logger.Info("==Module listen");



        }
        public void StopListen()
        {
            if (socketListen != null)
            {
                socketListen.Close();
                socketListen = null;
            }
        }
        void StartAccept(SocketAsyncEventArgs args)
        {
            if (args == null)
            {
                args = GetFreeEventArgs();
            }
            args.AcceptSocket = null;
            if (!socketListen.AcceptAsync(args))
            {
                ProcessAccept(args);
            }

            //_maxAcceptedClients.WaitOne();

            //不断执行检查是否有无效连接
            //var thread = new System.Threading.Thread(_DaemonThread);
            //thread.IsBackground = true;
            //thread.Start();
        }
        private void OnCompleted(object sender, SocketAsyncEventArgs e)
        {
            //try
            {

                //logger.Log("got complete state:" + e.LastOperation + "|" + e.SocketError);

                switch (e.LastOperation)
                {

                    case SocketAsyncOperation.Accept:
                        ProcessAccept(e);
                        break;
                    case SocketAsyncOperation.Connect:
                        ProcessConnect(e, e.UserToken as LinkInfo);
                        break;
                    case SocketAsyncOperation.Disconnect:
                        //lock (e.UserToken)
                        {
                            ProcessDisConnect(e, e.UserToken as LinkInfo);
                        }
                        break;
                    case SocketAsyncOperation.Receive:
                        {
                            //lock(e.UserToken)
                            {
                                if (e.SocketError != SocketError.Success)
                                {
                                    OnLinkError((e.UserToken as LinkInfo).Handle, new Exception(e.SocketError.ToString()));
                                    //throw new Exception("receive error.");
                                    ProcessRecvZero(e.UserToken as LinkInfo);
                                }
                                else
                                {
                                    bool bEnd = false;
                                    while (!bEnd)
                                    {
                                        bEnd = ProcessReceice(e, e.UserToken as LinkInfo);
                                    }

                                }//ProcessReceice(e, e.UserToken as LinkInfo);
                            }
                        }
                        break;
                    case SocketAsyncOperation.Send:
                        ProcessSend(e, e.UserToken as LinkInfo);
                        break;
                }
            }
            //catch (Exception Err)
            //{
            //    Console.WriteLine("error:" + Err.Message + "|" + Err.StackTrace);
            //}
        }

        //void _DaemonThread()
        //{
        //    while (true)
        //    {
        //        //加上超时检测代码

        //        for (int i = 0; i < 60 * 1000 / 10; i++) //每分钟检测一次
        //        {
        //            //if (!m_thread.IsAlive)
        //            //    break;
        //            Thread.Sleep(10);
        //        }
        //    }
        //}

        public UInt64 Connect(IPEndPoint linktoEndPoint)
        {
            if (this.OnConnected == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnLinkError == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnClosed == null)
            {
                throw new Exception("need set event OnClosed");
            }
            if (this.OnRecv == null)
            {
                throw new Exception("need set event OnRecv");
            }
            var eventArgs = GetFreeEventArgs();
            LinkInfo link = GetFreeLink();
            eventArgs.UserToken = link;
            link.type = LinkType.ConnectedLink;
            link.Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            link.Handle = (UInt64)link.Socket.Handle;
            link.sendTag = 0;
            link.lastPackageSize = 0;
            link.lastPackege = null;
            link.ConnectDateTime = DateTime.Now;
            link.Remote = linktoEndPoint;
            this.links[link.Handle] = link;

            eventArgs.RemoteEndPoint = linktoEndPoint;
            if (!link.Socket.ConnectAsync(eventArgs))
            {
                ProcessConnect(eventArgs, link);
            }

            return link.Handle;
        }

        public void Send(ulong linkid, byte[] data)
        {
            var link = this.links[linkid];

            lock (link)
            {
                if (option.WithPackageLength16M)
                {
                    if (data.Length >= 255 * 255 * 255)
                        throw new Exception("too long for packet mode.");
                    var lendata = BitConverter.GetBytes((UInt32)(data.Length));
                    SendOnce(link, new ArraySegment<byte>(lendata));
                }
                var oncecount = _SendBufferSize;

                var last = data.Length % oncecount;
                var splitcount = data.Length / oncecount;
                for (var i = 0; i < splitcount; i++)
                {
                    SendOnce(link, new ArraySegment<byte>(data, i * oncecount, oncecount));
                }
                if (last != 0)
                    SendOnce(link, new ArraySegment<byte>(data, splitcount * oncecount, last));
            }
        }


        public void Disconnect(ulong linkid)
        {
            var link = this.links[linkid];
            link.indisconnect = true;
            try
            {
                link.Socket.Shutdown(SocketShutdown.Both);

                var e = GetFreeEventArgs();
                e.UserToken = link;
                var b = link.Socket.DisconnectAsync(e);
                if (!b)
                {
                    ProcessDisConnect(e, link);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Disconnect error:" + ex.Message + "|" + ex.StackTrace);
            }
            finally
            {
                //link.Socket.Close();
                //link.Socket = null;
            }
        }
    }
}
