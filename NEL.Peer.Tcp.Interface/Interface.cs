using System;
using System.Net;

namespace NEL.Peer.Tcp
{
    public class PeerOption
    {
        public int maxSleepTimer = 10000;//默认10秒无响应的连接就自动断开
        public int ReadBufSize = 1024 * 16;
        public int WriteBufSize = 1024 * 16;
        public int ListenLinkBufSize = 100;
        public bool WithPackageLength16M = true;
    }

    /// <summary>
    ///
    /// </summary>
    public interface IPeer : IDisposable
    // 继承IDisposable 明确表示此类型含有非托管资源，需妥善释放
    {
        UInt64 ID
        {
            get;
        }
        string Ver
        {
            get;
        }
        //初始化Peer模块用
        void Start(PeerOption option);
        //结束Peer模块用，Dispose也调用Close，Close多次调用不应崩溃
        void Close();

        //一个Peer可选择是否监听
        void Listen(IPEndPoint endpoint);
        //结束监听
        void StopListen();

        //发起连接
        //一个Peer可以发起多个连接
        //发起连接可立即得到一个linkid
        UInt64 Connect(IPEndPoint linktoEndPoint);

        //无论是accept 的 和 connect的,统一管理 用一个UINT64标识他

        event Action<UInt64, IPEndPoint> OnAccepted;//Listen->When Connect In->OnAccepted(linkid,remoteEndPoint);
        event Action<UInt64, IPEndPoint> OnConnected;//Connect->When Linked->OnConnected(linkid);
        //连接发生错误时触发
        event Action<UInt64, Exception> OnLinkError;
        event Action<UInt64, byte[]> OnRecv;
        event Action<UInt64> OnClosed;//When Closed->OnCloseed(linkid);

        void Send(UInt64 link, byte[] data);
        void Disconnect(UInt64 linkid);
    }


}
