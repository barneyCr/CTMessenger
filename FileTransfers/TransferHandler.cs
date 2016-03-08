using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransfers
{
    public class TransferHandler
    {
        public int Port { get; internal set; }
        internal Thread WorkThread { get; set; }
        internal Dictionary<int, byte[]> PendingKeys;

        public TransferHandler(int port)
        {
            this.Port = port;
            this.WorkThread = new Thread(Work);
            this.WorkThread.IsBackground = true;
            this.PendingKeys = new Dictionary<int, byte[]>(10);
        }

        internal void Work()
        {

        }
    }
}
