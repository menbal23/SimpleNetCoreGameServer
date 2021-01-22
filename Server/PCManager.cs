using System;
using System.Collections.Concurrent;

namespace Server
{
    class PCManager
    {
        public static PCManager Instance { get; private set; } = new PCManager();
    }
}
