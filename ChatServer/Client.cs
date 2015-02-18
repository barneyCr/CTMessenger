using System;
using System.Net.Sockets;
using System.Threading;

namespace ChatServer
{
    public enum ConnectionState
    {
        Offline,
        Online // lol
    }

    public class Client
    {
        public void Send(string message, params object[] p)
        {
            this.Send(string.Format(message, p));
        }
        public void Send(string message)
        {
            this.Socket.Send(message);
            //Console.WriteLine("Packet to" + this.Username +": "+ message);
            Program.Write("Sending message of type " + Server.GetHeaderType(message.Split('|')[0]) + " to " + this.Username + "[" + this.UserID + "]",
                "PacketLogs", ConsoleColor.Blue);
        }

        public ConnectionState ConnectionState { get; set; }
        public Thread Thread { get; set; }
        public Socket Socket { get; set; }
        public int MessagesSent { get; set; }
        public string Username { get; set; }
        public int UserID { get; set; }

        /// <summary>
        /// Returns a new instance of the Client class, and generates a UID between 70 and 500000
        /// </summary>
        /// <param name="name">Given name</param>
        /// <param name="socket">TCP connection</param>
        public Client(string name, Socket socket)
        {
            this.Username = name;
            this.Socket = socket;
            this.UserID = Helper.Randomizer.Next(70, 500000);
        }
    }
}