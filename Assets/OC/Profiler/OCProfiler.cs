using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace OC.Profiler
{
    internal static class OCProfiler
    {
        [ThreadStatic]
        private static bool _isInitialized;
        [ThreadStatic]
        private static Queue<Stopwatch> _stopWatchPool;

        [ThreadStatic]
        private static Stack<Stopwatch> _watchStack;

        public static volatile bool Enabled = true;

        public static long CurrentTicks
        {
            get { return DateTime.Now.Ticks; }
        }

        public static float GetMillisecondSpan(long startTicks, long endTicks)
        {
            return (endTicks - startTicks) / 10000.0f;
        }

        public static void Start()
        {
            if (Enabled)
            {
                if (!_isInitialized)
                {
                    _stopWatchPool = new Queue<Stopwatch>();
                    _watchStack = new Stack<Stopwatch>();
                    _isInitialized = true;
                }

                Stopwatch watch;
                if (_stopWatchPool.Count > 0)
                {
                    watch = _stopWatchPool.Dequeue();
                }
                else
                {
                    watch = new Stopwatch();
                }

                _watchStack.Push(watch);
                watch.Reset();
                watch.Start();
            }
        }

        public static long Stop()
        {
            if (Enabled)
            {
                var watch = _watchStack.Pop();
                watch.Stop();
                var elapsed = watch.ElapsedMilliseconds;
                _stopWatchPool.Enqueue(watch);
                return elapsed;
            }

            return 0;
        }
    }
}
