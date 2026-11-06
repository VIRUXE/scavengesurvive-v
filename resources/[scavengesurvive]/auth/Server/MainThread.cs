using System;
using System.Collections.Concurrent;
using CitizenFX.Core;

namespace Server
{
    static class MainThread
    {
        static readonly ConcurrentQueue<Action> Pending = new ConcurrentQueue<Action>();

        public static void Post(Action work)
        {
            if (work != null)
                Pending.Enqueue(work);
        }

        public static void Drain()
        {
            Action work;
            int n = 0;
            while (n < 16 && Pending.TryDequeue(out work))
            {
                n++;
                try
                {
                    work();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("[auth] " + ex.GetType().Name);
                }
            }
        }
    }
}
