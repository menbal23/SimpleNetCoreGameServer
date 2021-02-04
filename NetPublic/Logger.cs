using System;
using System.Threading;
using System.IO;
using System.Collections.Concurrent;

namespace NetPublic
{
    class Logger
    {
		public static Logger Instance { get; private set; } = new Logger();
		// 쓰레드가 정상적으로 작동하게 하기 위해 volatile 로 선언
		private volatile bool m_bProcess = true;

		private class Log
		{
			public string logString = "";
			public bool bFile = true;
			public ConsoleColor color = ConsoleColor.Gray;
			public string extLogFileName = "";
			public Log() { Reset(); }
			public void Reset() { logString = ""; bFile = true; color = ConsoleColor.Gray; extLogFileName = ""; }
		}

		// Log Pool을 생성한다.
		Pool<Log> m_LogPool = new Pool<Log>(() => new Log(), log => log.Reset(), 32);

		private string m_FileName;
		private ConcurrentQueue<Log> m_Queue = new ConcurrentQueue<Log>();

		public void Init()
		{
			System.Diagnostics.Process proc = System.Diagnostics.Process.GetCurrentProcess();
			m_FileName = proc.ProcessName.Replace(".vshost", "");

			ThreadPool.QueueUserWorkItem(new WaitCallback(Process));
		}

		public void Write(string logString, bool bFile = true, ConsoleColor color = ConsoleColor.Gray, string extLogFileName = "")
		{
			if (string.IsNullOrEmpty(m_FileName) == true)
				return;

			Log log = m_LogPool.Pop();
			log.logString = logString;
			log.bFile = bFile;
			log.color = color;
			log.extLogFileName = extLogFileName;
			m_Queue.Enqueue(log);
		}

		public int GetQueueCount()
		{
			return m_Queue.Count;
		}

		private void WriteLog(Log log)
		{
			if (string.IsNullOrEmpty(log.logString) == true)
				return;

			if (log.logString[0] != '_')
			{
				Console.ForegroundColor = log.color;
				Console.WriteLine(log.logString);
			}

			if (log.bFile)
			{
				string path = string.Format("{0}\\{1}_{2}{3}.log", Environment.CurrentDirectory, m_FileName, DateTime.Now.ToString("yyyyMMdd"), log.extLogFileName);

				using (StreamWriter stream = File.AppendText(path))
				{
					stream.Write(DateTime.Now.ToString("HH:mm:ss "));
					stream.WriteLine(log.logString);
					stream.Flush();
					stream.Close();
				}
			}
		}

		public void Process(object obj)
		{
			while (m_bProcess == true)
			{
				Log log;
				if (m_Queue.TryDequeue(out log) == false)
				{
					Thread.Sleep(1);
					continue;
				}

				WriteLog(log);
				m_LogPool.Push(log);
			}
		}

		public void OnTerminate(string terminateLog)
		{
			m_bProcess = false;
			Write(terminateLog, true, ConsoleColor.Red);
			//아직 처리되지 않은 로그 기록
			Log log;
			while (m_Queue.TryDequeue(out log) == true)
				WriteLog(log);
		}
	}
}
