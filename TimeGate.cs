namespace AvatarAnimator
{
    public abstract class TimeGate
    {
        abstract public void Reset();
        abstract public bool Now();
    }
    public class UpdateTimeGate : TimeGate
    {
        private readonly int m_init;
        private readonly int m_interval;
        private int m_timer;
        public UpdateTimeGate(int interval, int startAt = 0)
        {
            m_init = startAt;
            m_interval = interval;
            Reset();
        }

        public override void Reset() { m_timer = m_init; }

        public override bool Now()
        {
            m_timer = (m_timer + 1) % m_interval;
            return 0 == m_timer;
        }
    }
    public class DelayTimeGate : TimeGate
    {
        private readonly double m_interval;
        private DateTime m_timer;
        public DelayTimeGate(double sec)
        {
            m_interval = sec;
            Reset();
        }

        public override bool Now()
        {
            if (DateTime.Now > m_timer)
            {
                m_timer.AddSeconds(m_interval);
                if (DateTime.Now > m_timer) Reset();
                return true;
            }
            return false;
        }

        public override void Reset()
        {
            m_timer = DateTime.Now.AddSeconds(m_interval);
        }
    }

}
