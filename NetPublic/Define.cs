using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NetPublic
{
	public delegate Task<ERROR_TYPE> PacketFunc<PacketType>(Context context, PacketType req) where PacketType : Packet;
	public delegate Task<ERROR_TYPE> EventFunc();

	public abstract class PacketInfo
	{
		public short m_Protocol = 0;
		public short m_ProtocolAck = 0;

		public PacketInfo(short protocol, short protocolAck)
		{
			m_Protocol = protocol;
			m_ProtocolAck = protocolAck;
		}

		public abstract Task<ERROR_TYPE> Execute(Context context);
	}

	public class PacketInfoInstance<PacketType> : PacketInfo where PacketType : Packet
	{
		public PacketFunc<PacketType> m_Func = null;

		public PacketInfoInstance(short protocol, short protocolAck, PacketFunc<PacketType> func)
			: base(protocol, protocolAck)
		{
			m_Func = func;
		}

		public override Task<ERROR_TYPE> Execute(Context context)
		{
			return m_Func(context, (PacketType)context.m_Packet);
		}
	}

	public class EventInfo
	{
		public short m_EventID = 0;
		public Int64 m_LastTick = 0;
		public Int64 m_IntervalTick = 0;
		public EventFunc m_Func = null;
	}

	public enum ERROR_TYPE : int
	{
		None = 0,                   //성공
		Error,                      //실패
		Packet,                     //잘못된 패킷
		DBError,                    //DB Error
	}

	class Define
    {
		private const int SIZE_OF_PACKET_SIZE = 4;
		private const int SIZE_OF_PACKET_TYPE = 2;
		private const int SIZE_OF_PACKET_ACCOUNTID = 8;
		public const int PACKET_HEADER_SIZE = SIZE_OF_PACKET_SIZE + SIZE_OF_PACKET_TYPE + SIZE_OF_PACKET_ACCOUNTID;
		public const int PACKET_TYPE_INDEX = SIZE_OF_PACKET_SIZE;
		public const int PACKET_ACCOUNTID_INDEX = PACKET_TYPE_INDEX + SIZE_OF_PACKET_TYPE;
		public const int PACKET_DATA_INDEX = PACKET_HEADER_SIZE;

		public const int MIN_PACKET_SIZE = SIZE_OF_PACKET_SIZE;
		public const int MAX_PACKET_SIZE = 65535;

		public const int SOCKET_RECV_TICK = 45000;
		public const int SOCKET_SEND_TICK = 15000;
		public const int SOCKET_WAIT_TICK = SOCKET_RECV_TICK + SOCKET_SEND_TICK;
	}
}
