using System;
using System.Collections.Generic;
using System.Threading;
using System.Collections.Concurrent;
using Snappy.Sharp;

namespace NetPublic
{
	using Deserializer = Func<string, Packet>;
	using DeserializerDictionary = Dictionary<int, Func<string, Packet>>;

	public class Receiver
	{
		public static Receiver Instance { get; private set; } = new Receiver();

		public ConcurrentQueue<Context> m_Queue = new ConcurrentQueue<Context>();
		public DeserializerDictionary m_DeserializerDic = new DeserializerDictionary();

		public int m_Count = 0;

		private bool m_bDeserializerLog = true;
		public void DeserializerLogDisable() { m_bDeserializerLog = false; }


		public Receiver()
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(Process));
		}

		public void RegisterDeserializer<PacketType>(PacketType packet) where PacketType : Packet
		{
			if (m_DeserializerDic.ContainsKey(packet.m_Type) == true)
			{
				Console.WriteLine("RegisterDeserializer: exist " + packet.m_Type.ToString());
				return;
			}

			m_DeserializerDic.Add(packet.m_Type, new Deserializer(Util.Deserialize<PacketType>));
		}

		private Packet Deserialize(short type, string json)
		{
			Deserializer deserializer;
			if (m_DeserializerDic.TryGetValue(type, out deserializer))
			{
				return deserializer(json);
			}
			return null;
		}

		public void Push(Context context)
		{
			if (context == null)
				return;

			m_Queue.Enqueue(context);
			ThreadPool.QueueUserWorkItem(new WaitCallback(Decrypt), context);
		}

		public void Push(int peerID, Int64 accountID, Packet packet)
		{
			if (packet == null)
				return;

			Context context = new Context();
			context.m_PeerID = peerID;
			context.m_AccountID = accountID;
			context.m_RequestID = packet.m_Type;
			context.m_Packet = packet;

			m_Queue.Enqueue(context);
			ThreadPool.QueueUserWorkItem(new WaitCallback(Decrypt), context);
		}

		public void Decrypt(object obj)
		{
			Context context = (Context)obj;
			if (context == null)
				return;

			try
			{
				if (context.m_Binary != null)
				{
					//복호화
					Util.Crypt(context.m_Binary, 0, context.m_Binary, 0, context.m_Binary.Length);
					//압축해제
					var snappy = new SnappyDecompressor();
					var result = snappy.Decompress(context.m_Binary, Define.PACKET_HEADER_SIZE, context.m_Binary.Length - Define.PACKET_HEADER_SIZE);

					string json = System.Text.Encoding.UTF8.GetString(result, 0, result.Length);

					bool bResult = false;
					if (context.m_RequestID > 0)
					{
						if (context.m_RequestID == (short)PROTOCOL.ALIVE_REQ || context.m_RequestID == (short)PROTOCOL.ALIVE_ACK)
						{
							// ALIVE_REQ, ALIVE_ACK 예외 처리(Process 참고)
							bResult = true;
						}
						else
						{
							Packet packet = Deserialize(context.m_RequestID, json);
							if (packet != null)
							{
								bResult = true;
								context.m_Packet = packet;
							}
						}
					}

					if (!bResult && m_bDeserializerLog)
					{
						Console.WriteLine("Not Exist Deserializer: " + json);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("ReceiveProcess: " + ex.ToString());
			}

			if (context.m_Packet == null)
			{
				context.Reset();
			}

			Interlocked.Increment(ref m_Count);
		}

		public void Process(object obj)
		{
			Context context = null;

			while (true)
			{
				if (context == null)
				{
					if (m_Queue.TryDequeue(out context) == false)
					{
						Thread.Sleep(1);
						continue;
					}
				}

				if (context.Empty() == true)
				{
					context = null;
					continue;
				}

				if (context.m_Packet == null)
				{
					Thread.Sleep(1);
					continue;
				}

				if (context.m_RequestID == (short)PROTOCOL.ALIVE_REQ || context.m_RequestID == (short)PROTOCOL.ALIVE_ACK)
				{
					context = null;
					continue;
				}

				NetworkService.Instance.EnqueueContext(context);
				context = null;
			}
		}
	}
}
