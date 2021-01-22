using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    class Player
    {
        public long AccountID = 0;

        public Player() { }

        public void Reset()
        {
            AccountID = 0;
        }
    }
}
