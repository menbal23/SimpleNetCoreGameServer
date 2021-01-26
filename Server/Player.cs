using System;
using System.Collections.Generic;
using System.Text;
using NetPublic;

namespace Server
{
    class Player
    {
        public long m_AccountID = 0;

        public Player() { }

        public void Reset()
        {
            m_AccountID = 0;
        }

        public void Send(Packet packet)
        {
            if (m_AccountID <= 0)
                return;

            NetworkService.Instance.SendPeerByAccountID(m_AccountID, packet);
        }
    }
}
