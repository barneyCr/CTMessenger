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
            Program.Write("Sending message of type " + Server.GetHeaderType(message.Split('|')[0]) + " to " + this.Username + "[" + this.UserID + "]",
                "PacketLogs", ConsoleColor.Blue);
        }

        public ConnectionState ConnectionState { get; set; }
        public Thread Thread { get; set; }
        public Socket Socket { get; set; }
        public int MessagesSent { get; set; }
        public string Username { get; set; }
        public int UserID { get; set; }

        public Client(string name, Socket socket)
        {
            this.Username = name;
            this.Socket = socket;
            this.UserID = Helper.Randomizer.Next(70, 69000);
        }
    }
}