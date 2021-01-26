using System;
using NetPublic;
using System.Threading;
using System.Collections.Generic;


namespace Client
{
    class Program
    {
        private static IniUtil m_Config;
        static void Main(string[] args)
        {
            BufferManager.Instance.Initialize();

            m_Config = new IniUtil(Environment.CurrentDirectory + @"\Client.ini");
            string ip = m_Config.GetIniValue("Server", "IP");
            int port = int.Parse(m_Config.GetIniValue("Server", "Port"));

            List<Client> listClient = new List<Client>();
            for (int i = 0; i < 100; ++i)
            {
                Client client = new Client();
                client.ConnectServer(ip, port);
                listClient.Add(client);
            }

            foreach (var client in listClient)
                client.ConnectSend();

            while (true)
            {
                foreach (var client in listClient)
                {
                    client.Send();
                }

                Thread.Sleep(1);
            }
        }
    }
}
