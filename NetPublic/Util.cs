using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace NetPublic
{
	public class Util
	{
		static ConcurrentDictionary<int, Random> g_Random = new ConcurrentDictionary<int, Random>();

		public static void Crypt(byte[] Dst, int DstOffset, byte[] Src, int SrcOffset, int Size)
		{
			byte Key = 0;
			int DstIndex = DstOffset;
			int SrcIndex = SrcOffset;

			for (int Count = 0; Count < Define.PACKET_HEADER_SIZE; ++Count)
			{
				if (Count < Define.MIN_PACKET_SIZE)
					Key += Src[SrcIndex];

				Dst[DstIndex] = Src[SrcIndex];
				++DstIndex;
				++SrcIndex;
			}

			for (int Count = Define.PACKET_HEADER_SIZE; Count < Size; ++Count)
			{
				Key = (byte)(Key * 253 + 195);
				Dst[DstIndex] = (byte)(Src[SrcIndex] ^ Key);

				++DstIndex;
				++SrcIndex;
			}
		}

		public static Int64 GetCurrentTick()
		{
			return DateTime.Now.Ticks;
		}
		//1000분의 1초
		public static Int64 GetTotalTick()
		{
			return (Int64)(GetCurrentTick() * 0.0001);
		}

		public static Int64 GetTickByMilliSecond(int millisecond) { return (Int64)millisecond * TimeSpan.TicksPerMillisecond; }
		public static Int64 GetTickBySecond(int second) { return (Int64)second * TimeSpan.TicksPerSecond; }
		public static Int64 GetTickByMinute(int minute) { return (Int64)minute * TimeSpan.TicksPerMinute; }
		public static Int64 GetTickByHour(int hour) { return (Int64)hour * TimeSpan.TicksPerHour; }
		public static Int64 GetTickByDay(int day) { return (Int64)day * TimeSpan.TicksPerDay; }
		public static int GetSecondByTick(Int64 tick) { return (int)TimeSpan.FromTicks(tick).TotalSeconds; }

		private static Random GetRandomInstance()
		{
			int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;   //ThreadID 별로 생성
			Random currentRandom = null;
			if (g_Random.TryGetValue(threadID, out currentRandom) == false)
			{
				currentRandom = new Random((int)DateTime.Now.Ticks);
				if (g_Random.TryAdd(threadID, currentRandom) == false)
				{
					if (g_Random.TryGetValue(threadID, out currentRandom) == false)
						return new Random((int)DateTime.Now.Ticks);
				}
			}
			return currentRandom;
		}

		public static int GetRandom()
		{
			Random random = GetRandomInstance();
			return random.Next();
		}

		//최대값보다 작은 난수 발생
		public static int GetRandom(int max)
		{
			Random random = GetRandomInstance();
			return random.Next(max);
		}

		//지정된 범위 내의 난수 발생
		public static int GetRandom(int min, int max)
		{
			Random random = GetRandomInstance();
			return random.Next(min, max);
		}
		
		public byte[] ToBinary(string hex)
		{
			int numberChars = hex.Length;
			byte[] bytes = new byte[numberChars / 2];

			for (int i = 0; i < numberChars; i += 2)
			{
				bytes[i / 2] = System.Convert.ToByte(hex.Substring(i, 2), 16);
			}

			return bytes;
		}

		public static Task<T> EmptyTaskFunction<T>(T result)
		{
			return Task.Run(() => { return result; });
		}

		public static string Serialize(object data)
		{
			string result;

			try
			{
				result = JsonConvert.SerializeObject(data);
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("Serialize : {0}, Exception : {1}", data.ToString(), ex.ToString()));
				result = default(string);
			}

			return result;
		}

		public static Task<string> AsyncSerialize(object data)
		{
			return Task.Run(() =>
			{
				return Serialize(data);
			});
		}

		public static T Deserialize<T>(string data)
		{
			T result;

			try
			{
				result = JsonConvert.DeserializeObject<T>(data);
			}
			catch (Exception ex)
			{
				Console.WriteLine(string.Format("Deserialize : {0}, Exception : {1}" + data, ex.ToString()));
				result = default(T);
			}

			return result;
		}

		public static Task<T> AsyncDeserialize<T>(string data)
		{
			return Task.Run(() =>
			{
				return Deserialize<T>(data);
			});
		}

		static public object[] Params(params object[] objects)
		{
			return objects;
		}
	}

	public class ConcurrentHashSet<T>
	{
		private ConcurrentDictionary<T, byte> Base = new ConcurrentDictionary<T, byte>();

		public int Count { get { return Base.Count; } }
		public IEnumerator<T> GetEnumerator() { return Base.Keys.GetEnumerator(); }
		public bool TryAdd(T value) { return Base.TryAdd(value, 0); }
		public bool TryRemove(T value) { byte temp = 0; return Base.TryRemove(value, out temp); }
		public bool Contains(T value) { return Base.ContainsKey(value); }
		public List<T> ToList() { return Base.Keys.ToList(); }
		public void Clear() { Base.Clear(); }
	}
}
