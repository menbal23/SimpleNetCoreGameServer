using System;
using System.Collections.Generic;
using System.Text;
using NetPublic;
using System.Threading.Tasks;
using System.Data;

namespace Server
{
    class Player
    {
        public long m_AccountID = 0;

        public Player() { }

        public void Reset()
        {
            m_AccountID = 0;
        }

        public void Send(Packet packet)
        {
            if (m_AccountID <= 0)
                return;

            NetworkService.Instance.SendPeerByAccountID(m_AccountID, packet);
        }

		public async Task<ERROR_TYPE> CharacterLoad()
		{
			//캐릭터 로드
			SQLDB sql = new SQLDB(Program.m_DBInfo);

			sql.SetProcedure("[SPCharacterLoad]");
			sql.Add("@AccountID", SqlDbType.BigInt, m_AccountID);

			switch (await sql.Execute())
			{
				// 성공
				case 0:
					foreach (DataRow row in sql.m_Table.Rows)
					{
						// 데이터 처리
						Int64 id = (Int64)row["ID"];
					}
					break;
				// 실패
				default:
					return ERROR_TYPE.DBError;
			}

			return ERROR_TYPE.None;
		}
	}
}
