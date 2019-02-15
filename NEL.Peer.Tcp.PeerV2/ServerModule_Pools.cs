using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace NEL.Peer.Tcp.PeerV2
{
    partial class ServerModule
    {
        void InitPools()
        {
            _ReadBufferSize = option.ReadBufSize;
            _SendBufferSize = option.WriteBufSize;
            poolEventArgs = new System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs>();
            for (var i = 0; i < option.ListenLinkBufSize*2; i++)
            {
                var args = new SocketAsyncEventArgs();
                args.Completed += OnCompleted;
                poolEventArgs.Push(args);
            }

            poolLinks = new System.Collections.Concurrent.ConcurrentStack<LinkInfo>();
            for (var i = 0; i < option.ListenLinkBufSize; i++)
            {
                poolLinks.Push(GetFreeLink());
            }
        }
        /// <summary>
        /// SocketAsyncEventArgs 池
        /// </summary>
        System.Collections.Concurrent.ConcurrentStack<SocketAsyncEventArgs> poolEventArgs;
        //回收对对象  
        void PushBackEventArgs(SocketAsyncEventArgs item)
        {
            if (item == null)
            {
                throw new ArgumentNullException("Items added to a SocketAsyncEventArgsPool cannot be null");
            }
            poolEventArgs.Push(item);
        }

        //分配对象  
        SocketAsyncEventArgs GetFreeEventArgs()
        {
            var b = poolEventArgs.TryPop(out SocketAsyncEventArgs args);
            if (!b)
            {
                args = new SocketAsyncEventArgs();
                args.Completed += OnCompleted;
            }
            return args;
        }

        System.Collections.Concurrent.ConcurrentStack<LinkInfo> poolLinks;
        void PushBackLinks(LinkInfo link)
        {
            try
            {
                link.Socket.Close();

            }
            catch(Exception err)
            {

            }

            link.Socket = null;
            link.Handle = 0;

            poolLinks.Push(link);
        }

        //16k 大缓存区， 100个链接占用3M多缓存区，还好
         int _ReadBufferSize ;
         int _SendBufferSize;

        LinkInfo GetFreeLink()
        {
            var b = poolLinks.TryPop(out LinkInfo link);
            if (!b)
            {
                link = new LinkInfo();
                if (link.recvArgs == null)
                {
                    link.recvArgs = GetFreeEventArgs();
                    link.recvArgs.SetBuffer(new byte[_ReadBufferSize], 0, _ReadBufferSize);
                }
                if (link.sendArgs == null)
                {
                    link.sendArgs = GetFreeEventArgs();
                    link.sendArgs.SetBuffer(new byte[_SendBufferSize], 0, _SendBufferSize);
                }
            }
            link.recvArgs.UserToken = link;
            link.sendArgs.UserToken = link;

            link.indisconnect = false;
            return link;
        }

    }
}
