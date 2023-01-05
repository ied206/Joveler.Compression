using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Joveler.Compression.XZ
{
    public class XZHardware
    {
        #region Hardware - PhysMem & CPU Threads
        public static ulong PhysMem()
        {
            XZInit.Manager.EnsureLoaded();

            return XZInit.Lib.LzmaPhysMem();
        }

        public static uint CpuThreads()
        {
            XZInit.Manager.EnsureLoaded();

            return XZInit.Lib.LzmaCpuThreads();
        }
        #endregion

        #region (internal) Thread Limiter
        internal static uint CheckThreadCount(int threads)
        {
            if (threads < 0)
                throw new ArgumentOutOfRangeException(nameof(threads));
            if (threads == 0) // Use system's thread number by default
                threads = Environment.ProcessorCount;
            else if (Environment.ProcessorCount < threads) // If the number of CPU cores/threads exceeds system thread number,
                threads = Environment.ProcessorCount; // Limit the number of threads to keep memory usage lower.
            return (uint)threads;
        }
        #endregion
    }
}
