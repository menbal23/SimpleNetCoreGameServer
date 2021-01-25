using System;
using System.Collections.Generic;
using System.Text;
using NetPublic;

namespace Client
{
    public class Client
    {
        public int m_PeerID { private set; get; }
        public long m_AccountID { private set; get; }
        public long m_SendTick { private set; get; }

        public void ConnectServer()
        {
            Peer peer = NetworkService.Instance.AllocPeer();
            if (peer == null)
                return;

            m_PeerID = peer.m_PeerID;
            m_AccountID = m_PeerID;
            m_SendTick = Util.GetCurrentTick() + Util.GetTickByMilliSecond(300);
            peer.Connect("127.0.0.1", 8080);
        }

        public void Send()
        {
            if (Util.GetCurrentTick() > m_SendTick)
                return;

            m_SendTick = Util.GetCurrentTick() + Util.GetTickByMilliSecond(300);
            NetworkService.Instance.SendPeer(m_PeerID, m_AccountID, new PacketConnectReq());
        }
    }
}
