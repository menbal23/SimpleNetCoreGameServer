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

        public void ConnectServer(string ip, int port)
        {
            Peer peer = NetworkService.Instance.AllocPeer();
            if (peer == null)
                return;

            m_PeerID = peer.m_PeerID;
            m_AccountID = m_PeerID;
            m_SendTick = Util.GetCurrentTick() + Util.GetTickByMilliSecond(300);
            peer.Connect(ip, port);
        }

        public void ConnectSend()
        {
            m_SendTick = Util.GetCurrentTick() + Util.GetTickByMilliSecond(300);
            NetworkService.Instance.SendPeer(m_PeerID, m_AccountID, new PacketConnectReq());
        }

        public void Send()
        {
            if (Util.GetCurrentTick() > m_SendTick)
                return;

            m_SendTick = Util.GetCurrentTick() + Util.GetTickByMilliSecond(300);

            var packet = new PacketTestReq();
            packet.m_TestMsg = $"Client {m_PeerID}";

            NetworkService.Instance.SendPeer(m_PeerID, m_AccountID, packet);
        }
    }
}
