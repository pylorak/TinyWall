using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PKSoft
{
    internal class ThreadBarrier
    {
        internal ThreadBarrier(int count)
        {
            Count = count;
        }

        internal int Count { get; set; }

        internal void Wait()
        {
            lock (this)
            {
                if (--Count > 0)
                {
                    System.Threading.Monitor.Wait(this);
                }
                else
                {
                    System.Threading.Monitor.PulseAll(this);
                }
            }
        }
    }
}
