using System;
using NetPublic;
using System.Threading;

namespace Server
{
    class Program
    {
        public static long m_CurrentTick { private set; get; }

        static void Main(string[] args)
        {
            m_CurrentTick = Util.GetCurrentTick();

            BufferManager.Instance.Initialize();
            ServerManager.Instance.Initialize();

            NetworkService.Instance.UseContextDic();
            Listener.Instance.Init(8080);

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
