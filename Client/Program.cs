using System;
using NetPublic;
using System.Threading;
using System.Collections.Generic;


namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            BufferManager.Instance.Initialize();

            List<Client> listClient = new List<Client>();
            for (int i = 0; i < 100; ++i)
            {
                Client client = new Client();
                client.ConnectServer();
                listClient.Add(client);
            }

            while(true)
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
