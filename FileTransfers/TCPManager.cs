using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfers
{
    internal class TCPManager
    {
        private TcpListener listener;
        public TCPManager(int port)
        {
            this.listener = new TcpListener(IPAddress.Loopback, port);
        }

        internal void Start()
        {
            this.listener.Start();
            while (true)
            {
                Socket client = listener.AcceptSocket();
                byte[] key = new byte[36];
                // 4 -> uid
                // 32 -> sid
                client.SendFile("file.exe", new byte[] { }, new byte[] { }, TransmitFileOptions.Disconnect);

            }
        }
    }
}
