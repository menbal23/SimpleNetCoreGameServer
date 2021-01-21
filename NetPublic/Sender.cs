using System;
using System.Linq;
using System.Threading;
using System.Collections.Concurrent;
using Snappy.Sharp;

namespace NetPublic
{
    public class Sender
    {
        public static Sender Instance { get; private set; } = new Sender();

        public ConcurrentQueue<Context> m_Queue = new ConcurrentQueue<Context>();
        public int m_Count = 0;

        public Sender()
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(Process));
        }

        public void Push(Context context)
        {
            if (context == null)
                return;

            m_Queue.Enqueue(context);

            ThreadPool.QueueUserWorkItem(new WaitCallback(Encrypt), context);
        }

        public void Encrypt(object obj)
        {
            Context context = (Context)obj;
            if (context == null)
                return;

            byte[] binary = null;
            try
            {
                //json
                string json = Util.Serialize(context.m_Packet);
                //압축
                byte[] data = System.Text.Encoding.UTF8.GetBytes(json);

                var snappy = new SnappyCompressor();
                byte[] compressed = BufferManager.Instance.Pop(snappy.MaxCompressedLength(data.Length), true);

                int size = Define.PACKET_HEADER_SIZE;
                size += snappy.Compress(data, 0, data.Length, compressed);
                if (size < Define.MAX_PACKET_SIZE)
                {
                    //Console.WriteLine("Packet Size - size:" + size + ", PacketType:" + ((PROTOCOL)context.m_Packet.m_Type).ToString(), false, ConsoleColor.DarkYellow);

                    binary = new byte[size];
                    //binary = JBufferManager.Instance.Pop(size);

                    byte[] sizeData = BitConverter.GetBytes(size);
                    Array.Copy(sizeData, 0, binary, 0, sizeData.Length);
                    byte[] packetTypeData = BitConverter.GetBytes(context.m_Packet.m_Type);
                    Array.Copy(packetTypeData, 0, binary, Define.PACKET_TYPE_INDEX, packetTypeData.Length);
                    byte[] accountIDData = BitConverter.GetBytes(context.m_AccountID);
                    Array.Copy(accountIDData, 0, binary, Define.PACKET_ACCOUNTID_INDEX, accountIDData.Length);
                    Array.Copy(compressed, 0, binary, Define.PACKET_HEADER_SIZE, size - Define.PACKET_HEADER_SIZE);
                    //암호화
                    Util.Crypt(binary, 0, binary, 0, binary.Length);
                }
                else
                    Console.WriteLine("Max Packet Size Over - size:" + size + ", AccountID:" + context.m_AccountID + ", PacketType:" + ((PROTOCOL)context.m_Packet.m_Type).ToString());

                BufferManager.Instance.Push(compressed);
            }
            catch (Exception ex)
            {
                Console.WriteLine("SendProcess : " + ex.ToString());
                //if (binary != null)
                //    JBufferManager.Instance.Push(binary);
                binary = null;
            }

            //최종데이터 셋팅
            if (binary == null)
            {
                context.Reset();
            }
            else
            {
                context.m_Binary = binary;
            }

            Interlocked.Increment(ref m_Count);
        }

        public void Process(object obj)
        {
            Context context = null;

            while (true)
            {
                if (context == null)
                {
                    if (m_Queue.TryDequeue(out context) == false)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                }

                if (context.Empty() == true)
                {
                    context = null;
                    continue;
                }

                if (context.m_Binary == null)
                {
                    Thread.Sleep(1);
                    continue;
                }

                if (context.m_AccountID == 0 && context.m_AccountIDs != null && context.m_AccountIDs.Count > 0)
                {
                    byte[] sourceBinary = context.m_Binary;
                    for (int i = 0; i < context.m_AccountIDs.Count; ++i)
                    {
                        if (i > 0)
                        {
                            context.m_Binary = new byte[sourceBinary.Length];
                            Array.Copy(sourceBinary, 0, context.m_Binary, 0, sourceBinary.Length);
                        }
                        context.m_AccountID = context.m_AccountIDs[i];
                        byte[] accountIDData = BitConverter.GetBytes(context.m_AccountID);
                        Array.Copy(accountIDData, 0, context.m_Binary, Define.PACKET_ACCOUNTID_INDEX, accountIDData.Length);
                        NetworkService.Instance.TransmitPeer(context);
                    }
                }
                else
                    NetworkService.Instance.TransmitPeer(context);

                context = null;
            }
        }
    }
}
