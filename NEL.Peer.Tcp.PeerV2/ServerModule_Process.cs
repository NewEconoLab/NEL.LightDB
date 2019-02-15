using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NEL.Peer.Tcp.PeerV2
{
    partial class ServerModule
    {
        //Semaphore _maxAcceptedClients;//用信号量控制最大连接数
        System.Collections.Concurrent.ConcurrentDictionary<UInt64, LinkInfo> links;

        void InitProcess()
        {
            //int _maxClient = 10000;
            links = new System.Collections.Concurrent.ConcurrentDictionary<UInt64, LinkInfo>();
            //_maxAcceptedClients = new Semaphore(_maxClient, _maxClient);

        }
        /// <summary>  
        /// 监听Socket接受处理  
        /// </summary>  
        /// <param name="e">SocketAsyncEventArg associated with the completed accept operation.</param>  
        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            lock (this)
            {
                if (e.SocketError == SocketError.Success)
                {
                    Socket s = e.AcceptSocket;//和客户端关联的socket  
                    if (s != null && s.Connected)
                    {
                        try
                        {

                            SocketAsyncEventArgs asyniar = GetFreeEventArgs();
                            var link = GetFreeLink();
                            link.type = LinkType.AcceptedLink;
                            asyniar.UserToken = link;

                            //用户的token操作
                            link.Socket = s;
                            link.Handle = (UInt64)s.Handle.ToInt64();
                            //token.ID = System.Guid.NewGuid().ToString();
                            link.ConnectDateTime = DateTime.Now;
                            link.sendTag = 0;
                            link.lastPackageSize = 0;
                            link.lastPackege = null;
                            link.Remote = link.Socket.RemoteEndPoint as System.Net.IPEndPoint;
                            this.links[link.Handle] = link;

                            //s.Send(Encoding.UTF8.GetBytes("Your GUID:" + token.ID));

                            //Console.WriteLine("client in:" + this.links.Count);
                            //lock (link)
                            {
                                this.OnAccepted(link.Handle, link.Remote);
                                //try
                                {
                                    if (link.indisconnect == false)
                                    {
                                        var asyncr = link.Socket.ReceiveAsync(link.recvArgs);
                                        if (!asyncr)
                                        {
                                            if (!asyncr)
                                            {
                                                bool bEnd = false;
                                                while (!bEnd)
                                                {
                                                    if (link.indisconnect == false)
                                                        bEnd = ProcessReceice(link.recvArgs, link);
                                                }
                                            }
                                        }
                                    }
                                }
                                //catch
                                //{
                                //    //如果這裏出問題就不管了
                                //}
                            }
                        }
                        catch (SocketException ex)
                        {

                            Console.WriteLine(String.Format("接收客户 {0} 数据出错, 异常信息： {1} 。", s.RemoteEndPoint, ex.ToString()));
                            //TODO 异常处理  
                        }
                        //投递下一个接受请求,這裏就直接復用了
                        StartAccept(e);
                    }
                }
                else
                {
                    logger.Error("error accept. continue." + e.SocketError);
                    //ProcessDisConnect(e, e.UserToken as LinkInfo);
                    StartAccept(e);
                }
            }
        }

        private void ProcessConnect(SocketAsyncEventArgs e, LinkInfo link)
        {
            this.OnConnected(link.Handle,link.Remote);

            var asyncr = link.Socket.ReceiveAsync(link.recvArgs);
            if (!asyncr)
            {
                bool bEnd = false;
                while (!bEnd)
                {
                    bEnd = ProcessReceice(link.recvArgs, link);
                }
            }

            //復用一個connect args
            e.UserToken = null;
            this.PushBackEventArgs(e);
        }
        private unsafe bool ProcessReceice(SocketAsyncEventArgs e, LinkInfo link)
        {
            var pi = e.ReceiveMessageFromPacketInfo;
            if (e.BytesTransferred == 0)
            {
                //接收0转过去的,这个e不给他回收
                ProcessRecvZero(link);
                return true;
            }
            if (option.WithPackageLength16M == false)
            {
                byte[] data = new byte[e.BytesTransferred];
                fixed (byte* src = e.Buffer, dest = data)
                {
                    System.Buffer.MemoryCopy(src + e.Offset, dest, e.BytesTransferred, e.BytesTransferred);
                }
                this.OnRecv(link.Handle, data);
            }
            else
            {
                var seek = 0;
                while (seek < e.BytesTransferred)
                {
                    if (link.lastPackageSize == 0)//fill len
                    {
                        if(link.lastPackageSizeSeek==0)
                        {
                            if (e.BytesTransferred - seek >= 4)//长度字节数够了
                            {
                                link.lastPackageSize = BitConverter.ToUInt32(e.Buffer, e.Offset + seek);
                                seek += 4;
                                link.lastPackege = new byte[link.lastPackageSize];
                                link.lastPackegeSeek = 0;
                                if (link.lastPackageSize > 16*1024*1024)
                                {
                                    throw new Exception("err package size is too big");
                                }
                            }
                            else //长度不够，读部分长度
                            {
                                link.lastPackageSizeSeek = (byte)(e.BytesTransferred - seek);
                                link.lastPackageSizeBuf = new byte[4];
                                for(var i=0;i<link.lastPackageSizeSeek;i++)
                                {
                                    link.lastPackageSizeBuf[i] = e.Buffer[seek+i];
                                }
                                seek += (int)link.lastPackageSizeSeek;
                                continue;//不够长跳出逻辑
                            }
                        }
                        else //长度不完整，接着读
                        {
                            var len = (byte)Math.Min(4 - link.lastPackageSizeSeek, e.BytesTransferred - seek);
                            for (var i = 0; i < len; i++)
                            {
                                link.lastPackageSizeBuf[link.lastPackageSizeSeek + i] =
                                e.Buffer[e.Offset + seek + i];
                            }
                            seek += len;
                            link.lastPackageSizeSeek += len;
                            if (link.lastPackageSizeSeek==4)
                            {
                                link.lastPackageSizeSeek = 0;
                                link.lastPackageSize = BitConverter.ToUInt32(link.lastPackageSizeBuf,0);
                                link.lastPackege = new byte[link.lastPackageSize];
                                link.lastPackegeSeek = 0;
                            }
                            else
                            {
                                continue;//不够长跳出逻辑
                            }
                        }
                    }
                    if (seek < e.BytesTransferred)//fill package
                    {
                        var len = (uint)Math.Min(link.lastPackageSize - link.lastPackegeSeek, e.BytesTransferred - seek);
                        fixed (byte* src = e.Buffer, dest = link.lastPackege)
                        {
                            System.Buffer.MemoryCopy(src + e.Offset + seek, dest + link.lastPackegeSeek, e.BytesTransferred, len);
                        }
                        link.lastPackegeSeek +=len;
                        seek += (int)len;
                    }
                    if (link.lastPackegeSeek == link.lastPackageSize )//finish package
                    {
                        link.lastPackageSize = 0;
                        link.lastPackegeSeek = 0;
                        this.OnRecv(link.Handle, link.lastPackege);
                    }
                }
            }
            try
            {
                var asyncr = link.Socket.ReceiveAsync(link.recvArgs);
                return asyncr;
            }
            catch(Exception err)
            {
                logger.Error("error ProcessReceice. continue." + e.SocketError);
                return true;
            }
        }

        private unsafe void SendOnce(LinkInfo link, ArraySegment<byte> data)
        {
            if (data.Count > _SendBufferSize)
                throw new Exception("1buf for once send");

            lock (link)
            {
                //check if queue
                if (link.queueSend == null)
                    link.queueSend = new System.Collections.Generic.Queue<ArraySegment<byte>>();

                if (link.sendTag == 1)
                {
                    link.queueSend.Enqueue(data);
                    return;
                }

                //senddata
                //link.sendArgs.SendPacketsSendSize = oncedata.Count;
                //修改发出尺寸
                if (link.sendArgs.Count != data.Count)
                    link.sendArgs.SetBuffer(link.sendArgs.Offset, data.Count);
                fixed (byte* src = data.Array, dest = link.sendArgs.Buffer)
                {
                    Buffer.MemoryCopy(src + data.Offset,
                        dest + link.sendArgs.Offset,
                        link.sendArgs.Buffer.Length,
                        data.Count);
                }
                bool basync = link.Socket.SendAsync(link.sendArgs);
                if (basync)//操作没有立即完成，标记
                    link.sendTag = 1;
                //从逻辑上讲sendonce 要么处于sendTag=1，进队列，要么处于 第一笔交易 ，只需标记
                //CheckSendQueue(link, basync);
            }
        }
        private unsafe void CheckSendQueue(LinkInfo link, bool basync = false)
        {

            //check if have more data to send
            while (!basync && (link.queueSend != null && link.queueSend.Count > 0))
            {
                var oncedata = link.queueSend.Dequeue();
                //link.sendArgs.SendPacketsSendSize = oncedata.Count;
                //修改发出尺寸
                if (link.sendArgs.Count != oncedata.Count)
                    link.sendArgs.SetBuffer(link.sendArgs.Offset, oncedata.Count);
                fixed (byte* src = oncedata.Array, dest = link.sendArgs.Buffer)
                {
                    Buffer.MemoryCopy(src + oncedata.Offset,
                        dest + link.sendArgs.Offset,
                        _SendBufferSize,
                        oncedata.Count);
                }
                basync = link.Socket.SendAsync(link.sendArgs);
            }
            if (basync)//操作没有立即完成，标记
                link.sendTag = 1;
        }
        private void ProcessSend(SocketAsyncEventArgs e, LinkInfo link)
        {
            lock (link)
            {
                link.sendTag = 0;
                CheckSendQueue(link);
            }
        }
        private void ProcessDisConnect(SocketAsyncEventArgs e, LinkInfo link)
        {//收到这个是主动断线一方
            try
            {
                this.links.TryRemove(link.Handle, out LinkInfo v);

                this.OnClosed(link.Handle);
                this.PushBackLinks(link);

            }
            catch (Exception err)
            {

            }
            PushBackEventArgs(e);
        }
        /// <summary>
        /// 收到零个字节代表这个连接被断开了
        /// </summary>
        /// <param name="link"></param>
        private void ProcessRecvZero(LinkInfo link)
        {
            this.links.TryRemove(link.Handle, out LinkInfo v);


            this.OnClosed(link.Handle);
            this.PushBackLinks(link);

        }
    }
}
