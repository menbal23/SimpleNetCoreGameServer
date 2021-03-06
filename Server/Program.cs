﻿using System;
using NetPublic;
using System.Threading;

namespace Server
{
    class Program
    {
        private static IniUtil m_Config;
        public static long m_CurrentTick { private set; get; }
        public static string m_DBInfo { private set; get; }

        static void Main(string[] args)
        {
            m_CurrentTick = Util.GetCurrentTick();

            BufferManager.Instance.Initialize();
            PlayerManager.Instance.Initialize(1000);
            ServerManager.Instance.Initialize();

            m_Config = new IniUtil(Environment.CurrentDirectory + @"\server.ini");
            Listener.Instance.Init(int.Parse(m_Config.GetIniValue("Server", "Port")));

            // DB 처리 시 추가
            //if (ConnectGameDB() == false)
            //{
            //    return;
            //}

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

        private static bool ConnectGameDB()
        {
            string DBName = m_Config.GetIniValue("DB", "NAME");
            string DBIP = m_Config.GetIniValue("DB", "IP");
            string DBID = m_Config.GetIniValue("DB", "ID");
            string DBPW = m_Config.GetIniValue("DB", "PW");

            m_DBInfo = string.Format("Data Source={0};Initial Catalog={1};User ID={2};Password={3};", DBIP, DBName, DBID, DBPW);

            SQLDB dbconn = new SQLDB(m_DBInfo);
            if (dbconn.Check() == false)
            {
                Console.WriteLine("DB Open Fail!!");
                return false;
            }

            return true;
        }
    }
}
