using System;
using System.Collections.Concurrent;
using NetPublic;

namespace Server
{
    class PlayerManager
    {
        public static PlayerManager Instance { get; private set; } = new PlayerManager();

        private Pool<Player> m_PlayerPool;
        private ConcurrentDictionary<long, Player> m_DicPlayer;

        public void Initialize(int count)
        {
            m_PlayerPool = new Pool<Player>(() => new Player(), player => { player.Reset(); }, count);
            m_DicPlayer = new ConcurrentDictionary<long, Player>();
        }

        public Player Alloc(long accountID)
        {
            if (accountID < 0)
                return null;

            if (m_DicPlayer.ContainsKey(accountID) == true)
                return null;

            Player player = m_PlayerPool.Pop();
            if (player == null)
                return null;

            if (m_DicPlayer.TryAdd(accountID, player) == false)
            {
                m_PlayerPool.Push(player);
                return null;
            }

            player.m_AccountID = accountID;
            return player;
        }

        public void Release(Player player)
        {
            if (player == null)
                return;

            m_DicPlayer.TryRemove(player.m_AccountID, out _);
            m_PlayerPool.Push(player);
        }

        public Player FindAccountID(long accountID)
        {
            if (accountID < 0)
                return null;

            if (m_DicPlayer.TryGetValue(accountID, out var player) == true)
                return player;

            return null;
        }
    }
}
