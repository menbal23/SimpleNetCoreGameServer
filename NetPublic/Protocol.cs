using System;
using System.Collections.Generic;
using System.Text;

namespace NetPublic
{
	public enum PROTOCOL
	{
		CONNECT_REQ = 1,
		CONNECT_ACK,

		CLOSE_REQ,
		CLOSE_ACK,

		ALIVE_REQ,
		ALIVE_ACK,

		TEST_REQ,
		TEST_ACK,
	}

	public class Packet
	{
		public short m_Type;
		public int m_ErrorType;
	}

	public class PacketConnectReq : Packet
	{
		public PacketConnectReq()
		{
			m_Type = (short)PROTOCOL.CONNECT_REQ;
		}
	}

	public class PacketConnectAck : Packet
	{
		public PacketConnectAck()
		{
			m_Type = (short)PROTOCOL.CONNECT_ACK;
		}
	}

	public class PacketCloseReq : Packet
	{
		public PacketCloseReq()
		{
			m_Type = (short)PROTOCOL.CLOSE_REQ;
		}
	}

	public class PacketCloseAck : Packet
	{
		public PacketCloseAck()
		{
			m_Type = (short)PROTOCOL.CLOSE_ACK;
		}
	}

	public class PacketAliveReq : Packet
	{
		public PacketAliveReq()
		{
			m_Type = (short)PROTOCOL.ALIVE_REQ;
		}
	}

	public class PacketAliveAck : Packet
	{
		public PacketAliveAck()
		{
			m_Type = (short)PROTOCOL.ALIVE_ACK;
		}
	}

	public class PacketTestReq : Packet
	{
		public string m_TestMsg = "";

		public PacketTestReq()
		{
			m_Type = (short)PROTOCOL.TEST_REQ;
		}
	}

	public class PacketTestAck : Packet
	{
		public string m_TestMsg = "";

		public PacketTestAck()
		{
			m_Type = (short)PROTOCOL.TEST_ACK;
		}
	}
}
