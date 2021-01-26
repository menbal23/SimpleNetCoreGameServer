using System;
using NetPublic;
using System.Threading;

namespace Server
{
    class Program
    {
        private static IniUtil m_Config;
        public static long m_CurrentTick { private set; get; }

        static void Main(string[] args)
        {
            m_CurrentTick = Util.GetCurrentTick();

            BufferManager.Instance.Initialize();
            PCManager.Instance.Initialize(1000);
            ServerManager.Instance.Initialize();

            NetworkService.Instance.UseContextDic();

            m_Config = new IniUtil(Environment.CurrentDirectory + @"\server.ini");
            Listener.Instance.Init(int.Parse(m_Config.GetIniValue("Server", "Port")));

            Thread thread = new Thread(Process);
            thread.Start();
            thread.Join();
        }

        private static void Process()
        {
            while(true)
            {
                m_CurrentTick = Util.GetCurrentTick();

                ServerManager.Instance.Process();
                NetworkService.Instance.IncrementFPS();

                Thread.Sleep(1);
            }
        }
    }
}
