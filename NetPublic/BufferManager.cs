using System;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace NetPublic
{
	using BufferQueue = ConcurrentQueue<byte[]>;

	public class BufferManager
	{
		public static BufferManager Instance { get; private set; } = new BufferManager();

		private Dictionary<int, BufferQueue> m_Container = new Dictionary<int, BufferQueue>();
		private int MaxBufferSize = 0;

		public void Initialize()
		{
			for (int n = 0; n < 7; ++n)
			{
				int size = 1 << (10 + n);
				if ((size - 1) > Define.MAX_PACKET_SIZE)
				{
					break;
				}
				m_Container.Add(size, new BufferQueue());
				if (size > MaxBufferSize)
					MaxBufferSize = size;
			}
		}

		public byte[] Pop(int size, bool bForce = false)
		{
			if (size <= 0 || size > MaxBufferSize)
			{
				if (bForce == true && size > 0)
					return new byte[size];

				return null;
			}

			for (int n = 0; n < 7; ++n)
			{
				int bufferSize = 1 << (10 + n);
				if (size <= bufferSize)
				{
					size = bufferSize;
					break;
				}
			}

			BufferQueue queue = null;
			if (m_Container.TryGetValue(size, out queue) == false)
			{
				return null;
			}

			byte[] buffer = null;
			if (queue.TryDequeue(out buffer) == false)
				buffer = new byte[size];

			return buffer;
		}

		public void Push(byte[] buffer)
		{
			if (buffer == null)
				return;

			BufferQueue bufferQueue = null;
			if (m_Container.TryGetValue(buffer.Length, out bufferQueue) == false)
				return;

			Array.Clear(buffer, 0, buffer.Length);
			bufferQueue.Enqueue(buffer);
		}
	}
}
