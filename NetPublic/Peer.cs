using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;

namespace NetPublic
{
    public class Peer
    {
        private const int DEFAULT_BUFFER_SIZE = 1024;

        private Socket m_Socket = null;

        // 접속한 유저를 구분하기위한 ID
        public int m_PeerID = 0;

        //로그인후 실제 유저데이터와 연결하기 위한 ID
        public Int64 m_AccountID = 0;

        // 살아있는지 체크
        private Int64 m_EventCount = 0;
        public Int64 m_ReceiveTick = 0;
        public Int64 m_SendTick = 0;
        private int m_CloseFlag = 0;

        //리셋
        public void Reset()
        {
            m_Socket = null;
            m_PeerID = 0;
            m_AccountID = 0;
            m_EventCount = 0;
            m_ReceiveTick = 0;
            m_SendTick = 0;
            m_CloseFlag = 0;
        }

        private SocketAsyncEventArgs AllocEvent()
        {
            SocketAsyncEventArgs arg = NetworkService.Instance.AllocSocketEvent(this);
            if (arg == null)
                return null;

            Interlocked.Increment(ref m_EventCount);

            return arg;
        }

        private void ReleaseEvent(SocketAsyncEventArgs arg)
        {
            if (arg == null)
                return;

            BufferManager.Instance.Push(arg.Buffer);

            Interlocked.Decrement(ref m_EventCount);

            NetworkService.Instance.ReleaseSocketEvent(arg);
        }

        public void Bind(SocketAsyncEventArgs arg)
        {
            Interlocked.Increment(ref m_EventCount);

            try
            {
                m_Socket = arg.AcceptSocket;
                m_Socket.NoDelay = true;
                m_Socket.LingerState = new LingerOption(false, 0);

                m_ReceiveTick = Util.GetTotalTick() + Define.SOCKET_RECV_TICK;
                m_SendTick = Util.GetTotalTick() + Define.SOCKET_SEND_TICK;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Bind: " + ex.Message);
                Close(arg);
                return;
            }

            //받기요청
            arg.UserToken = this;
            arg.AcceptSocket = null;
            Receive(arg);
        }

        public string GetIPAddress()
        {
            try
            {
                if (m_Socket != null)
                    return ((IPEndPoint)m_Socket.RemoteEndPoint).Address.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("GetIPAddress Exception : {0}", ex.Message));
            }

            return "";
        }

        //connect
        public void Connect(string ip, int port)
        {
            SocketAsyncEventArgs arg = AllocEvent();
            try
            {
                IPAddress address = null;

                if (IPAddress.TryParse(ip, out address) == false)
                {
                    IPHostEntry entry = Dns.GetHostEntry(ip);

                    // IPV4 우선 검색
                    if (address == null)
                    {
                        for (int i = 0; i < entry.AddressList.Length; i++)
                        {
                            if (entry.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
                            {
                                address = entry.AddressList[i];
                                break;
                            }
                        }
                    }

                    // IPV6 검색
                    if (address == null)
                    {
                        for (int i = 0; i < entry.AddressList.Length; i++)
                        {
                            if (entry.AddressList[i].AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                address = entry.AddressList[i];
                                break;
                            }
                        }
                    }
                }

                m_Socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                m_Socket.NoDelay = true;
                m_Socket.LingerState = new LingerOption(false, 0);

                arg.RemoteEndPoint = new IPEndPoint(address, port);

                if (m_Socket.ConnectAsync(arg) == false)
                {
                    ConnectCompleted(arg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Connect: " + ex.Message);
                Close(arg);
            }
        }

        public void ConnectCompleted(SocketAsyncEventArgs arg)
        {
            if (arg.SocketError != SocketError.Success)
            {
                Close(arg);
                return;
            }

            m_ReceiveTick = Util.GetTotalTick() + Define.SOCKET_RECV_TICK;
            m_SendTick = Util.GetTotalTick() + Define.SOCKET_SEND_TICK;

            //받기요청
            arg.RemoteEndPoint = null;
            Receive(arg);
        }

        //receive
        private void Receive(SocketAsyncEventArgs arg)
        {
            ReceiveProcess(arg, true);
        }

        public void ReceiveProcess(SocketAsyncEventArgs arg, bool bTryReceive)
        {
            try
            {
                while (true)
                {
                    if (arg.Buffer == null)
                    {
                        byte[] buffer = BufferManager.Instance.Pop(DEFAULT_BUFFER_SIZE);
                        arg.SetBuffer(buffer, 0, buffer.Length);
                    }

                    if (bTryReceive == true && m_Socket.ReceiveAsync(arg) == true)
                        break;

                    if (ReceiveCompleted(arg) == false)
                        break;

                    bTryReceive = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ReceiveProcess: " + ex.Message);
                Close(arg);
            }
        }

        public bool ReceiveCompleted(SocketAsyncEventArgs arg)
        {
            if (arg.SocketError != SocketError.Success || arg.BytesTransferred <= 0)
            {
                Close(arg);
                return false;
            }

            m_ReceiveTick = Util.GetTotalTick() + Define.SOCKET_RECV_TICK;

            byte[] buffer = arg.Buffer;
            int remainDataSize = arg.Offset + arg.BytesTransferred;
            int currentPos = 0;

            while (true)
            {
                if (remainDataSize < Define.MIN_PACKET_SIZE)
                    break;

                int packetSize = BitConverter.ToInt32(arg.Buffer, currentPos);
                if (packetSize <= Define.MIN_PACKET_SIZE || packetSize >= Define.MAX_PACKET_SIZE)
                {
                    Console.WriteLine(string.Format("ReceiveCompleted: packetSize {0}, Client Peer", packetSize), false, true, ConsoleColor.Red);
                    Close(arg);
                    return false;
                }

                if (packetSize <= remainDataSize) //패킷이 완성되었다.
                {
                    //context 생성후 전달
                    Context context = new Context();
                    context.m_PeerID = m_PeerID;
                    context.m_Binary = new byte[packetSize];
                    Array.Copy(arg.Buffer, currentPos, context.m_Binary, 0, packetSize);
                    context.m_RequestID = BitConverter.ToInt16(context.m_Binary, Define.PACKET_TYPE_INDEX);
                    context.m_AccountID = BitConverter.ToInt64(context.m_Binary, Define.PACKET_ACCOUNTID_INDEX);
                    if (m_AccountID > 0)
                        context.m_AccountID = m_AccountID;
                    Receiver.Instance.Push(context);

                    //다음패킷 처리를 위해 위치 조정
                    currentPos += packetSize;
                    remainDataSize -= packetSize;
                }
                else    //아직 데이터를 더 받아야 한다.
                {
                    if (buffer.Length < packetSize) //버퍼의 크기를 패킷 크기 만큼 확보한다.
                    {
                        byte[] temp = BufferManager.Instance.Pop(packetSize);
                        if (temp == null)
                        {
                            Close(arg);
                            return false;
                        }
                        Array.Copy(buffer, currentPos, temp, 0, remainDataSize);
                        BufferManager.Instance.Push(buffer);
                        buffer = temp;
                        currentPos = 0;
                    }
                    break;
                }
            }

            if (remainDataSize > 0) //아직 버퍼에 처리되지 않은 데이터가 남아있다.
            {
                if (currentPos > 0)
                {
                    byte[] temp = BufferManager.Instance.Pop(Math.Max(remainDataSize, DEFAULT_BUFFER_SIZE));
                    if (temp == null)
                    {
                        Close(arg);
                        return false;
                    }
                    Array.Copy(buffer, currentPos, temp, 0, remainDataSize);
                    BufferManager.Instance.Push(buffer);
                    buffer = temp;
                }

                arg.SetBuffer(buffer, remainDataSize, buffer.Length - remainDataSize);
            }
            else
            {
                BufferManager.Instance.Push(buffer);
                arg.SetBuffer(null, 0, 0);
            }

            return true;
        }

        //send
        public void Send(Context context)
        {
            // 보낼 패킷을 버퍼에 설정한다.
            SocketAsyncEventArgs arg = AllocEvent();
            try
            {
                arg.SetBuffer(context.m_Binary, 0, context.m_Binary.Length);

                if (m_Socket.SendAsync(arg) == false)
                {
                    SendCompleted(arg);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Send: " + ex.ToString());
                Close(arg);
                return;
            }
        }

        public void SendCompleted(SocketAsyncEventArgs arg)
        {
            if (arg.SocketError != SocketError.Success)
            {
                Close(arg);
                return;
            }

            m_SendTick = Util.GetTotalTick() + Define.SOCKET_SEND_TICK;

            if (arg.BytesTransferred != arg.Count)
            {
                Console.WriteLine("SendCompleted: transferred: " + arg.BytesTransferred.ToString() + "  total: " + arg.Count.ToString());
                return;
            }

            ReleaseEvent(arg);
        }

        public void Close(SocketAsyncEventArgs arg)
        {
            ReleaseEvent(arg);

            try
            {
                if (m_Socket != null)
                {
                    m_Socket.Shutdown(SocketShutdown.Send);
                    m_Socket.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Peer Close : {ex.Message}");
            }

            m_Socket = null;

            m_ReceiveTick = 0;
            m_SendTick = 0;

            if (Interlocked.CompareExchange(ref m_CloseFlag, 1, 0) == 0)
            {
                NetworkService.Instance.ReleasePeer(m_PeerID);
            }
        }

        public bool CloseCheck()
        {
            if (m_ReceiveTick > 0 || m_EventCount > 0)
                return false;

            return true;
        }
    }
}
