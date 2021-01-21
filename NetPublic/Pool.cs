using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace JNetwork
{
	public class Pool<T>
	{
		private ConcurrentQueue<T> m_Pool;
		private Func<T> m_CreateFunction;
		private Action<T> m_ResetFunction;

		public Pool(Func<T> createFunction, Action<T> resetFunction, int capacity)
		{
			if (createFunction == null)
			{
				throw new ArgumentNullException("createFunction");
			}

			m_CreateFunction = createFunction;
			m_ResetFunction = resetFunction;

			m_Pool = new ConcurrentQueue<T>();
		}

		public T Pop()
		{
			T item;
			if (m_Pool.TryDequeue(out item) == false)
				item = m_CreateFunction();
			return item;
		}

		public void Push(T item)
		{
			if (item == null)
			{
				System.Diagnostics.Debug.WriteLine("null item.");
				return;
			}

			if (m_ResetFunction != null)
			{
				m_ResetFunction(item);
			}

			m_Pool.Enqueue(item);
		}

		public int GetSize()
		{
			return m_Pool.Count;
		}
	}
}
