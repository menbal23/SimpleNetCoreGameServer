using System;
using System.Net;
using System.Net.Sockets;

namespace NetPublic
{
    public class Listener
    {
        public static Listener Instance { get; private set; } = new Listener();

        private Socket m_Socket = null;

        //Listen
        public void Init(int port)
        {
            if (m_Socket != null)
            {
                Console.WriteLine("Init: m_Socket exist");
                return;
            }

            try
            {
                m_Socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                m_Socket.NoDelay = true;
                m_Socket.LingerState = new LingerOption(false, 0);

                m_Socket.Bind(new IPEndPoint(IPAddress.Any, port));

                m_Socket.Listen(int.MaxValue);

                for (int i = 0; i < 500; i++)
                {
                    Accept();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Init: " + ex.Message);
            }
        }

        //Accept
        public void Accept()
        {
            SocketAsyncEventArgs arg = NetworkService.Instance.AllocSocketEvent(null);

            try
            {
                // 즉시 완료 되면 이벤트가 발생하지 않으므로 리턴값이 false일 경우 콜백 매소드를 직접 호출해 줍니다.
                // 리턴값이 true일 경우 비동기 요청이 들어간 상태이므로 콜백 매소드를 기다리면 됩니다.
                if (m_Socket.AcceptAsync(arg) == false)
                {
                    Complete(arg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Accept: " + ex.Message);
                NetworkService.Instance.ReleaseSocketEvent(arg);
            }
        }

        public void Complete(SocketAsyncEventArgs arg)
        {
            //새로 접속대기 요청
            Accept();

            //접속처리
            if (arg.SocketError == SocketError.Success)
            {
                Peer peer = NetworkService.Instance.AllocPeer();
                if (peer != null)
                {
                    peer.Bind(arg);
                    return;
                }
                else
                {
                    Console.WriteLine("Complete: peer null");
                }
            }
            else
            {
                Console.WriteLine("Complete: " + arg.SocketError.ToString());
            }

            //실패시 처리
            try
            {
                arg.AcceptSocket.Close();
            }
            catch
            {
            }

            NetworkService.Instance.ReleaseSocketEvent(arg);
        }
    }
}