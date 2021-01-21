using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Net.Sockets;
using System.Collections.Concurrent;

namespace NetPublic
{
    public class NetworkService
    {
        public static NetworkService Instance { get; private set; } = new NetworkService();

        //SocketEvent
        private ConcurrentQueue<SocketAsyncEventArgs> m_SocketEventQueue = new ConcurrentQueue<SocketAsyncEventArgs>();

        //Peer
        private ConcurrentDictionary<int, Peer> m_PeerDic = new ConcurrentDictionary<int, Peer>();
        private ConcurrentDictionary<Int64, Peer> m_PeerDicByAccountID = new ConcurrentDictionary<Int64, Peer>();
        private int m_LastPeerID = 0;

        //Context
        private bool m_UseContextDic = false;
        private class ContextQueue : Queue<Context> { public object m_Lock { get; private set; } = new object(); }
        private ConcurrentDictionary<Int64, ContextQueue> m_ContextDic = new ConcurrentDictionary<Int64, ContextQueue>();
        private ConcurrentQueue<Context> m_ContextQueue = new ConcurrentQueue<Context>();
        private Int64 m_ContextRecvCount = 0;
        private Int64 m_ContextSendCount = 0;

        private int m_FPS = 0;

        private int PopFPS()
        {
            return Interlocked.Exchange(ref m_FPS, 0);
        }

        public void IncrementFPS()
        {
            Interlocked.Increment(ref m_FPS);
        }

        //Init
        public NetworkService()
        {
            TimerManager.Instance.Add(new Timer(new TimerCallback(AlivePeer), null, 0, 1000));
        }

        //SocketEvent
        public SocketAsyncEventArgs AllocSocketEvent(Peer peer)
        {
            SocketAsyncEventArgs arg = null;

            if (m_SocketEventQueue.TryDequeue(out arg) == false)
            {
                arg = new SocketAsyncEventArgs();
                arg.Completed += new EventHandler<SocketAsyncEventArgs>(CompleteSocketEvent);
            }

            arg.UserToken = peer;

            return arg;
        }

        public void ReleaseSocketEvent(SocketAsyncEventArgs arg)
        {
            if (arg == null)
                return;

            arg.AcceptSocket = null;
            arg.UserToken = null;
            arg.RemoteEndPoint = null;
            arg.SetBuffer(null, 0, 0);

            m_SocketEventQueue.Enqueue(arg);
        }

        public void CompleteSocketEvent(object sender, SocketAsyncEventArgs arg)
        {
            if (arg.LastOperation == SocketAsyncOperation.Accept)
            {
                Listener.Instance.Complete(arg);
                return;
            }

            var peer = (Peer)arg.UserToken;
            if (peer == null)
            {
                Console.WriteLine("CompleteSocketEvent: peer null");
                ReleaseSocketEvent(arg);
                return;
            }

            switch (arg.LastOperation)
            {
                case SocketAsyncOperation.Connect:
                    peer.ConnectCompleted(arg);
                    break;

                case SocketAsyncOperation.Send:
                    peer.SendCompleted(arg);
                    break;

                case SocketAsyncOperation.Receive:
                    peer.ReceiveProcess(arg, false);
                    //peer.ReceiveCompleted(arg);
                    break;

                default:
                    Console.WriteLine("CompleteSocketEvent: LastOperation");
                    peer.Close(arg);
                    break;
            }
        }

        //Peer
        public Peer AllocPeer()
        {
            Peer peer = new Peer();

            while (true)
            {
                peer.m_PeerID = Interlocked.Increment(ref m_LastPeerID);
                peer.m_ReceiveTick = Util.GetTotalTick() + Define.SOCKET_WAIT_TICK;
                peer.m_SendTick = Util.GetTotalTick() + Define.SOCKET_WAIT_TICK;

                if (peer.m_PeerID >= int.MaxValue || peer.m_PeerID < 0)
                {
                    Interlocked.Exchange(ref m_LastPeerID, 1);
                }

                if (m_PeerDic.TryAdd(peer.m_PeerID, peer) == true)
                    break;
            }

            return peer;
        }

        public void ReleasePeer(int id)
        {
            if (id <= 0)
                return;

            Peer peer = null;
            if (m_PeerDic.TryRemove(id, out peer) == false)
            {
                Console.WriteLine("Remove: " + id.ToString());
                return;
            }

            Peer temp = null;
            m_PeerDicByAccountID.TryRemove(peer.m_AccountID, out temp);

            PacketCloseAck packet = new PacketCloseAck();
            Receiver.Instance.Push(peer.m_PeerID, peer.m_AccountID, packet);

            peer.Reset();
        }

        public Peer FindPeer(int id)
        {
            Peer peer = null;

            if (m_PeerDic.TryGetValue(id, out peer) == false)
                return null;

            return peer;
        }

        public bool ExistPeerByAccountID(Int64 accountID) { return m_PeerDicByAccountID.ContainsKey(accountID); }
        public Peer FindPeerByAccountID(Int64 accountID)
        {
            Peer peer = null;

            if (m_PeerDicByAccountID.TryGetValue(accountID, out peer) == false)
                return null;

            return peer;
        }

        public void AlivePeer(object state)
        {
            var list = m_PeerDic.Values.ToList();
            Int64 tick = Util.GetTotalTick();

            //중간에 삭제되는 경우가 있으므로 리스트로 받아서 처리함
            PacketAliveAck ack = new PacketAliveAck();

            foreach (var peer in list)
            {
                if (peer.CloseCheck() == true)
                {
                    continue;
                }

                if (peer.m_ReceiveTick < tick)
                {
                    peer.Close(null);
                    continue;
                }

                if (peer.m_SendTick < tick)
                {
                    SendPeer(peer.m_PeerID, 0, ack);
                    continue;
                }
            }

            int fps = PopFPS();
            string strInfo = string.Format
            ("p: {0}, {1}, {2}   c: {3}   r: {4}, {5}   s: {6}, {7}   fps: {8}",
                list.Count, m_ContextRecvCount, m_ContextSendCount, //p
                m_ContextQueue.Count,   //c
                Receiver.Instance.m_Queue.Count, Receiver.Instance.m_Count,   //r
                Sender.Instance.m_Queue.Count, Sender.Instance.m_Count,   //s
                fps    //fps
            );
            Console.WriteLine(strInfo, false, false, fps <= 20 ? ConsoleColor.Red : ConsoleColor.Gray);

            Interlocked.Exchange(ref Receiver.Instance.m_Count, 0);
            Interlocked.Exchange(ref Sender.Instance.m_Count, 0);
            Interlocked.Exchange(ref m_ContextRecvCount, 0);
            Interlocked.Exchange(ref m_ContextSendCount, 0);
        }

        public void SetServerPeer(int peerID)
        {
            Peer peer = FindPeer(peerID);
            if (peer != null)
                peer.m_bServerPeer = true;
        }

        public void ConnectPeer(Int64 accountID, string ip, int port)
        {
            if (accountID <= 0 || string.IsNullOrEmpty(ip) || port <= 0)
                return;

            Peer peer = AllocPeer();
            if (peer == null)
                return;

            peer.m_AccountID = accountID;
            peer.Connect(ip, port);
        }

        public void ClosePeer(int peerID)
        {
            if (peerID <= 0)
                return;

            Peer peer = FindPeer(peerID);
            if (peer == null)
                return;

            peer.Close(null);
        }

        public void BindPeer(int peerID, Int64 accountID, Context prevBindedPeerCloseContext = null)
        {
            if (peerID <= 0 || accountID <= 0)
                return;

            Peer peer = FindPeer(peerID);
            if (peer == null)
                return;

            Peer temp = null;
            if (m_PeerDicByAccountID.TryRemove(accountID, out temp) == true)
            {
                if (temp.m_PeerID != peer.m_PeerID)
                {
                    if (prevBindedPeerCloseContext != null)
                        temp.Send(prevBindedPeerCloseContext);
                    temp.m_AccountID = 0;
                    temp.Close(null);
                }
            }

            peer.m_AccountID = accountID;
            m_PeerDicByAccountID.TryAdd(accountID, peer);
        }

        public void SendPeer(int peerID, Int64 accountID, Packet packet)
        {
            if (peerID <= 0 || packet == null)
                return;

            Context context = new Context();
            context.m_AccountID = accountID;
            context.m_PeerID = peerID;
            context.m_Packet = packet;
            context.m_RequestID = packet.m_Type;

            Sender.Instance.Push(context);
        }

        public void SendPeer(int peerID, List<Int64> accountIDs, Packet packet)
        {
            if (peerID <= 0 || accountIDs.Count <= 0 || packet == null)
                return;

            Context context = new Context();
            context.m_AccountIDs = accountIDs;
            context.m_PeerID = peerID;
            context.m_Packet = packet;
            context.m_RequestID = packet.m_Type;

            Sender.Instance.Push(context);
        }

        public void SendPeerByAccountID(Int64 accountID, Packet packet)
        {
            if (accountID <= 0 || packet == null)
                return;

            if (ExistPeerByAccountID(accountID) == false)
                return;

            Context context = new Context();
            context.m_AccountID = accountID;
            context.m_Packet = packet;
            context.m_RequestID = packet.m_Type;

            Sender.Instance.Push(context);
        }

        public void SendPeerByAccountID(List<Int64> accountIDs, Packet packet)
        {
            if (accountIDs.Count <= 0 || packet == null)
                return;

            Context context = new Context();
            context.m_AccountIDs = accountIDs;
            context.m_Packet = packet;
            context.m_RequestID = packet.m_Type;

            Sender.Instance.Push(context);
        }

        public void BroadcastPeer(Packet packet)
        {
            if (packet == null)
                return;

            Context context = new Context();
            context.m_Packet = packet;
            context.m_RequestID = packet.m_Type;

            Sender.Instance.Push(context);
        }

        public void TransmitPeer(Context context)
        {
            if (context == null)
                return;

            //보내기
            if (context.m_PeerID > 0)
            {
                Peer peer = FindPeer(context.m_PeerID);
                if (peer == null)
                    return;

                if (peer.CloseCheck() == true)
                    return;

                Interlocked.Increment(ref m_ContextSendCount);

                peer.Send(context);
            }
            else if (context.m_AccountID > 0)
            {
                Peer peer = FindPeerByAccountID(context.m_AccountID);
                if (peer == null)
                    return;

                if (peer.CloseCheck() == true)
                    return;

                Interlocked.Increment(ref m_ContextSendCount);

                peer.Send(context);
            }
            else
            {
                foreach (Peer peer in m_PeerDicByAccountID.Values)
                {
                    if (peer.CloseCheck() == true)
                        continue;

                    Interlocked.Increment(ref m_ContextSendCount);

                    peer.Send(context);
                }
            }
        }

        //Context
        public void UseContextDic()
        {
            m_UseContextDic = true;
        }

        public void EnqueueContext(Context context)
        {
            if (context == null)
                return;

            if (context.m_RequestID > 0)
            {
                Interlocked.Increment(ref m_ContextRecvCount);
            }

            if (m_UseContextDic == true && context.m_AccountID > 0)
            {
                ContextQueue queue = null;
                if (m_ContextDic.TryGetValue(context.m_AccountID, out queue) == false)
                {
                    queue = new ContextQueue();
                    if (m_ContextDic.TryAdd(context.m_AccountID, queue) == false)
                        m_ContextDic.TryGetValue(context.m_AccountID, out queue);
                }

                lock (queue.m_Lock)
                {
                    queue.Enqueue(context);
                    if (queue.Count != 1)
                        return;
                }
            }

            m_ContextQueue.Enqueue(context);
        }

        public Context DequeueContext()
        {
            Context context = null;

            if (m_ContextQueue.TryDequeue(out context) == false)
                return null;

            return context;
        }

        public void ReleaseContext(Context context)
        {
            if (context == null)
                return;

            Context next = null;

            if (m_UseContextDic == true && context.m_AccountID > 0)
            {
                ContextQueue queue = null;
                if (m_ContextDic.TryGetValue(context.m_AccountID, out queue) == true)
                {
                    lock (queue.m_Lock)
                    {
                        if (queue.Count > 0)
                            queue.Dequeue();
                        if (queue.Count > 0)
                            next = queue.Peek();
                    }
                }
            }

            if (next != null)
                m_ContextQueue.Enqueue(next);
        }
    }
}
