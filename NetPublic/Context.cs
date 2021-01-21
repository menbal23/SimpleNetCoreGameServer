using System;
using System.Collections.Generic;

namespace NetPublic
{
    public class Context
    {
        public int m_PeerID = 0;
        public Int64 m_AccountID = 0;
        public List<Int64> m_AccountIDs = null;
        public short m_RequestID = 0;
        public Packet m_Packet = null;
        public byte[] m_Binary = null;

        public void Reset()
        {
            m_PeerID = 0;
            m_AccountID = 0;
            m_AccountIDs = null;
            m_RequestID = 0;
            m_Packet = null;
            m_Binary = null;
        }

        public bool Empty()
        {
            if (m_Packet == null && m_Binary == null)
                return true;

            return false;
        }
    }
}
