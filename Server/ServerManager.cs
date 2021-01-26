using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Data;
using System.Data.SqlClient;
using NetPublic;

namespace Server
{
    class ServerManager : Parser
    {
        public static ServerManager Instance { get; private set; } = new ServerManager();

        // 패킷들을 등록해서 처리한다.
        public void Initialize()
        {
            RegisterPacket(new PacketConnectReq(), (short)PROTOCOL.CONNECT_ACK, RecvConnectReq);
            RegisterPacket(new PacketTestReq(), (short)PROTOCOL.TEST_ACK, RecvTestReq);
        }

        public override void Process()
        {
            base.Process();
        }

        public override void SendError(Context context, Packet errorAck)
        {
            base.SendError(context, errorAck);
        }

        private async Task<ERROR_TYPE> RecvConnectReq(Context context, PacketConnectReq req)
        {
            var player = PCManager.Instance.Alloc(context.m_AccountID);
            if (player == null)
                return ERROR_TYPE.Error;

            NetworkService.Instance.BindPeer(context.m_PeerID, context.m_AccountID);

            PacketConnectAck ack = new PacketConnectAck();
            player.Send(ack);
            
            return await Util.EmptyTaskFunction(ERROR_TYPE.None);
        }

        private async Task<ERROR_TYPE> RecvTestReq(Context context, PacketTestReq req)
        {
            var player = PCManager.Instance.FindAccountID(context.m_AccountID);
            if (player == null)
                return ERROR_TYPE.Error;

            PacketTestAck ack = new PacketTestAck();
            ack.m_TestMsg = req.m_TestMsg;
            player.Send(ack);

            return await Util.EmptyTaskFunction(ERROR_TYPE.None);
        }
    }
}
