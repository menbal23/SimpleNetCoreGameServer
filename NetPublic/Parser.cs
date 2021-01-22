using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetPublic
{
    public abstract class Parser
    {
        //Packet
        public Dictionary<short, PacketInfo> m_PacketDic = new Dictionary<short, PacketInfo>();

        public virtual void Process()
        {
            Context context = null;
            while (true)
            {
                context = NetworkService.Instance.DequeueContext();
                if (context == null)
                    break;

                ProcessContext(context);
            }
        }

        private async void ProcessContext(Context context)
        {
            if (context.m_RequestID > 0)
            {
                await ProcessPacket(context);
            }

            NetworkService.Instance.ReleaseContext(context);
        }

        // 패킷을 등록 한다.
        public void RegisterPacket<PacketTypeReq>(PacketTypeReq req, short protocolAck, PacketFunc<PacketTypeReq> func) where PacketTypeReq : Packet
        {
            if (func == null)
            {
                Console.WriteLine("RegisterPacket: func null");
                return;
            }

            if (req.m_Type <= 0)
            {
                Console.WriteLine("RegisterPacket: protocol req:" + req.m_Type.ToString());
                return;
            }

            if (m_PacketDic.ContainsKey(req.m_Type) == true)
            {
                Console.WriteLine("RegisterPacket: exist " + req.m_Type.ToString());
                return;
            }

            PacketInfo info = new PacketInfoInstance<PacketTypeReq>(req.m_Type, protocolAck, func);
            m_PacketDic.Add(req.m_Type, info);

            Receiver.Instance.RegisterDeserializer(req);
        }

        public virtual void SendError(Context context, Packet errorAck)
        {
            NetworkService.Instance.SendPeer(context.m_PeerID, context.m_AccountID, errorAck);
        }

        private async Task<ERROR_TYPE> ProcessPacket(Context context)
        {
            PacketInfo info = null;

            if (m_PacketDic.TryGetValue(context.m_RequestID, out info) == true)
            {
                ERROR_TYPE error = await info.Execute(context);

                if (error != ERROR_TYPE.None && info.m_ProtocolAck != 0)
                {
                    Packet errorAck = new Packet();
                    errorAck.m_Type = info.m_ProtocolAck;
                    errorAck.m_ErrorType = (int)error;

                    SendError(context, errorAck);
                }
            }

            return ERROR_TYPE.None;
        }
    }
}
