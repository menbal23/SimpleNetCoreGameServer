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
            RegisterPacket(new PacketConnectReq(), 0, RecvConnectReq);
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
            return await Util.EmptyTaskFunction(ERROR_TYPE.None);
        }


    }
}
