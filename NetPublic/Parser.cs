using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NetPublic
{
    public abstract class Parser
    {
        //Packet
        public Dictionary<short, PacketInfo> m_PacketDic = new Dictionary<short, PacketInfo>();

        //Event 
        public Dictionary<short, EventInfo> m_EventDic = new Dictionary<short, EventInfo>();
        public short m_LastEventID = 0;

        public virtual void Process()
        {
            UpdateEvent();

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
            if (context.m_RequestID < 0)
            {
                await ProcessEvent(context);
            }

            if (context.m_RequestID > 0)
            {
                await ProcessPacket(context);
            }

            NetworkService.Instance.ReleaseContext(context);
        }

        //Packet
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

        protected virtual void SendError(Context context, Packet errorAck)
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

        //Event
        public void RegisterEvent(Int64 interval, EventFunc func)
        {
            if (func == null)
            {
                Console.WriteLine("RegisterEvent: func null ");
                return;
            }

            --m_LastEventID;

            if (m_EventDic.ContainsKey(m_LastEventID) == true)
            {
                Console.WriteLine("RegisterEvent: exist " + m_LastEventID.ToString());
                return;
            }

            EventInfo info = new EventInfo();

            info.m_EventID = m_LastEventID;
            info.m_LastTick = Util.GetTotalTick();
            info.m_IntervalTick = interval;
            info.m_Func = func;

            m_EventDic.Add(info.m_EventID, info);
        }

        private async Task<ERROR_TYPE> ProcessEvent(Context context)
        {
            EventInfo info = null;
            if (m_EventDic.TryGetValue(context.m_RequestID, out info) == true)
            {
                await info.m_Func();
            }

            return ERROR_TYPE.None;
        }

        private void UpdateEvent()
        {
            Int64 currentTick = Util.GetTotalTick();

            foreach (EventInfo info in m_EventDic.Values)
            {
                if (info == null)
                    continue;

                if (currentTick >= info.m_LastTick + info.m_IntervalTick)
                {
                    //info.m_LastTick = currentTick;
                    info.m_LastTick += info.m_IntervalTick;

                    Context context = new Context();
                    context.m_RequestID = info.m_EventID;

                    NetworkService.Instance.EnqueueContext(context);
                }
            }
        }
    }
}
