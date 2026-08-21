
using System.Collections.Generic;

namespace BazaarBounty.Utils
{
    public class TimerPool
    {
        private static TimerPool instance;
        private TimerPool() { }
        public static TimerPool GetInstance()
        {
            if (instance == null)
            {
                instance = new TimerPool();
            }
            return instance;
        }

        private List<Timer> timers = new List<Timer>();

        public void RegisterDelay(Timer timer)
        {
            timers.Add(timer);
        }

        public void Update()
        {
            foreach (Timer timer in timers)
            {
                timer.Update();
            }
            // remove all done delays
            timers.RemoveAll(delay => delay.isDone);
        }
    }

    public class Timer
    {
        public static void Delay(float duration, Callback callback)
        {
            TimerPool.GetInstance().RegisterDelay(new Timer(duration, callback));
        }

        public delegate void Callback();
        public Timer(float duration, Callback callback)
        {
            this.duration = duration;
            this.callback = callback;
        }

        public Callback callback;
        public double duration;
        public bool isDone = false;
        public double timeElapsed = 0;

        public void Update()
        {
            timeElapsed += BazaarBountyGame.GetGameTime().ElapsedGameTime.TotalSeconds;
            if (timeElapsed >= duration)
            {
                callback();
                isDone = true;
            }
        }
    }
}