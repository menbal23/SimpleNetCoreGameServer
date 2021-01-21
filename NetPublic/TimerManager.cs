using System.Collections.Generic;
using System.Threading;

namespace NetPublic
{
    public class TimerManager
    {
        public static TimerManager Instance { get; private set; } = new TimerManager();

        private List<Timer> m_TimerList = new List<Timer>();

        public void Add(Timer timer)
        {
            if (timer == null)
                return;

            m_TimerList.Add(timer);
        }
    }
}
