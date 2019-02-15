using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NEL.Peer.Tcp.PeerV2
{
    enum LinkType
    {
        AcceptedLink,
        ConnectedLink,
    }
    class LinkInfo
    {
        public bool indisconnect;
        public LinkType type;
        public Socket Socket;
        public UInt64 Handle;
        public IPEndPoint Remote;
        public DateTime ActiveDateTime;
        public DateTime ConnectDateTime;
        public SocketAsyncEventArgs recvArgs;
        public SocketAsyncEventArgs sendArgs;

        public int sendTag;//此标记为1则有数据在发送中
        public Queue<ArraySegment<byte>> queueSend;

        public UInt32 lastPackageSize;//最后一个包的尺寸
        public byte[] lastPackege;
        public UInt32 lastPackegeSeek;
        public byte[] lastPackageSizeBuf;
        public byte lastPackageSizeSeek;
    }
}
