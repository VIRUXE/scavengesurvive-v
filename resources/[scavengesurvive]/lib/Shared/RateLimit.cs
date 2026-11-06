using System.Collections.Generic;

namespace Shared
{
    /// <summary>
    /// One-second window, same rule as MTA <c>lib/modules/net.lua</c> <c>allowed</c>.
    /// A new window starts when <paramref name="nowMs"/> is at least 1000 ms after the current one.
    /// </summary>
    public sealed class RateLimit
    {
        private sealed class Window
        {
            public long Start;
            public int Count;
        }

        private readonly Dictionary<string, Window> _windows = new Dictionary<string, Window>();

        public bool Allow(string key, int perSecond, long nowMs)
        {
            Window window;
            if (!_windows.TryGetValue(key, out window) || nowMs - window.Start >= 1000)
            {
                window = new Window { Start = nowMs, Count = 0 };
                _windows[key] = window;
            }

            window.Count++;
            return window.Count <= perSecond;
        }

        public void Forget(string key)
        {
            _windows.Remove(key);
        }
    }
}
