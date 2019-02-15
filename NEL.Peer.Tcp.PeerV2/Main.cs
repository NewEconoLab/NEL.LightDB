using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.Peer.Tcp.PeerV2
{
    public class PeerV2
    {
        public static IPeer CreatePeer(NEL.Common.ILogger logger)
        {
            return new ServerModule(logger);
        }
    }
}
