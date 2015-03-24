using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Linq;
using System.Threading;

namespace ChatServer
{
    class Server
    {
        public static AuthMethod AuthMethod { get; set; }
        public static string Password { private get; set; }

        readonly TcpListener _listener;
        readonly IPAddress ip = IPAddress.Any;
        readonly int port, maxConnections;
        readonly bool logPackets, acceptServers;
        public readonly Thread listenThread;
        public static readonly ASCIIEncoding enc = new ASCIIEncoding();

        public Dictionary<int, Client> Connections { get; set; }
        public ICollection<string> Blacklist { get; set; }

        public bool Listen { get; set; }

        public Server(int port, int maxConnections, AuthMethod auth, string password)
        {
            this.maxConnections = maxConnections;
            this.port = port;
            this._listener = new TcpListener(ip, port);
            this.Connections = new Dictionary<int, Client>(maxConnections);
            this.listenThread = new Thread(StartListening);
            this.logPackets = Program.Settings["logPackets"];
            this.acceptServers = Program.Settings["acceptServerSockets"];

            this.Blacklist = new List<string>();

            Password = password;
            AuthMethod = auth;
        }

        void StartListening()
        {
            try
            {
                _listener.Start();
                Listen = true;
                Program.Write(LogMessageType.Network, "Started listening on port " + this.port);
            }
            catch { Program.Write("There may be another server listening on this port, we can't start our TCP listener", "Network", ConsoleColor.Magenta); }

            while (Listen)
            {
                try
                {
                    if (Connections.Count + 1 > maxConnections) 
                        continue;
                    Socket socket = _listener.AcceptSocket();
                    if (this.Blacklist.Contains(socket.RemoteEndPoint.ToString().Split(':')[0]))
                    {
                        Program.Write(LogMessageType.Auth, "Rejected blacklisted IP: {0}", socket.RemoteEndPoint.ToString());
                        socket.Send(BlacklistedPacket);
                        socket.Close();
                        continue;
                    }
                    var watch = Stopwatch.StartNew();
                    Program.Write(LogMessageType.Network, "Incoming connection");
                    ConnectionFlags flag = ConnectionFlags.None;

                    Client client = OnClientConnected(socket, ref flag);
                    if (client != null && flag == ConnectionFlags.OK)
                        OnSuccessfulClientConnect(client);
                    else if ((flag == ConnectionFlags.BadPassword && AuthMethod == ChatServer.AuthMethod.Full) || flag == ConnectionFlags.BadFirstPacket)
                    {
                        try
                        {
                            socket.Send(AccessDeniedPacket);
                            socket.Shutdown(SocketShutdown.Both);
                            Program.Write(LogMessageType.Auth, "A client failed to connect");
                        }
                        finally
                        {
                            socket.Close();
                            socket = null;
                        }
                    }
                    else if (flag == ConnectionFlags.SocketError)
                    {
                        Program.Write("Socket error on connection", "Auth", ConsoleColor.Red);
                    }
                    watch.Stop();
                    Program.Write("Handled new connection in " + watch.Elapsed.TotalSeconds + " seconds", "Trace");
                }
                catch (SocketException e)
                {
                    Program.Write("Exception code: " + e.ErrorCode, "Socket Error", ConsoleColor.Red);
                    break;
                }
            }
        }

        void OnSuccessfulClientConnect(Client newClient)
        {
            newClient.Socket.Send(AccessGrantedPacket);
            Thread.Sleep(100);
            newClient.Send(CreatePacket(1, newClient.UserID, newClient.Username));

            lock (Connections.Values)
            {
                foreach (var otherGuy in Connections)
                    newClient.Send(CreatePacket(29, otherGuy.Key, otherGuy.Value.Username)); // tell the new guy about the others

                foreach (var otherGuy in Connections)
                    otherGuy.Value.Send(CreatePacket(31, newClient.UserID, newClient.Username)); // tell the other guys about the new guy
            }
                Connections.Add(newClient.UserID, newClient);

            Program.Write(LogMessageType.UserEvent, "{0} connected [{1}]", newClient.Username, newClient.UserID);
            (newClient.Thread = new Thread(() => HandleClient(newClient))).Start();
        }

        void HandleClient(Client client)
        {
            client.ConnectionState = ConnectionState.Online;
            byte[] buffer = new byte[1024];
            int bytesRead = 0;

            //SendServerInformationPacket(client);
            try
            {
                while (client.ConnectionState == ConnectionState.Online)
                {
                    if (!Receive(client.Socket, buffer, ref bytesRead))
                    {
                        OnError(client);
                        break;
                    }

                    using (var packet = new Packet(enc.GetString(buffer, 0, bytesRead), client, false))
                        HandlePacket(packet);
                }
            }
            finally
            {
                lock (Connections)
                    OnClientDisconnected(client);
            }
        }

        static void SendServerInformationPacket(Client cln)
        {
            string packet = CreatePacket(69, Program.Settings["svOwner"], Program.Settings["svName"]);
            cln.Send(packet);
            cln.Send(CreatePacket(3, Program.Settings["svWelcomeMsg"]));
        }
        static string CreatePacket(params object[] o) {
            return string.Join("|", o);
        }

        void OnClientDisconnected(Client client)
        {
            Connections.Remove(client.UserID);
            lock (Connections)
                foreach (var user in Connections.Values)
                    user.Send(CreatePacket(12, client.UserID ^ 0x50));
            Program.Write(string.Format("{0} disconnected [{1}]", client.Username, client.UserID), "Network", ConsoleColor.Gray);
        }

        void HandlePacket(Packet p)
        {
            p.Seek(+1);
            var sender = p.Sender;
            if (logPackets && p.Header != HeaderTypes.POST_MESSAGE)
                Program.Write("Received packet of type " + GetHeaderType(p.Header) + ", from " + sender.Username + "[" + sender.UserID + "]", "PacketLogs", ConsoleColor.Blue);

            if (p.Header == HeaderTypes.POST_MESSAGE) // msg
            {
                sender.MessagesSent++;
                string message = p.ReadString();
                foreach (var user in Connections.Values)
                    user.Send(CreatePacket(HeaderTypes.NOTIFY_POST, sender.UserID ^ 0x121, message));
            }
            else if (p.Header == HeaderTypes.SEND_WHISPER) // whisper
            {
                string targetName = p.ReadString();
                Client target = Connections.Values.FirstOrDefault(c => c.Username == targetName);
                if (target != null)
                {
                    string message = p.ReadString();
                    target.Send(CreatePacket(HeaderTypes.RECEIVE_WHISPER, sender.UserID, message)); // 37 = received whisper
                    sender.Send(CreatePacket(HeaderTypes.SENT_WHISPER, target.UserID, message)); // 38 = sent whisper
                }
                else
                {
                    sender.Send(CreatePacket(HeaderTypes.WHISPER_ERROR));
                }
            }
            else if (p.Header == HeaderTypes.CHANGE_USERNAME_REQUEST) // change Username request
            {
                string newUsername = p.ReadString();
                if (Connections.Any(pair => pair.Value.Username == newUsername)) // there is someone with this username
                {
                    sender.Send(CreatePacket(HeaderTypes.CHANGE_USERNAME_DENIED));
                }
                else
                {
                    Program.Write(LogMessageType.UserEvent, "{0}[{1}] changed username to {2}", sender.Username, sender.UserID, newUsername);
                    sender.Username = newUsername;
                    foreach (var client in Connections.Values)
                        client.Send(CreatePacket(HeaderTypes.CHANGE_USERNAME_ANNOUNCE, sender.UserID, sender.Username));
                }
            }
            else if (p.Header == HeaderTypes.REPORT_USER)
            {
                int reportedUserID = p.ReadInt();
                string reason = p.ReadString();
                Client reportedClient;
                bool valid = true, before=false;
                if (Connections.TryGetValue(reportedUserID, out reportedClient))
                {
                    if (before=(reportedClient.Reports.ContainsKey(sender.UserID))) // reported before by same user
                    {
                        DateTime lastReportFromSender = reportedClient.Reports[sender.UserID];
                        if ((DateTime.Now - lastReportFromSender) > TimeSpan.FromMinutes(5)) // 5 mins passed
                        {
                            valid = true;
                            reportedClient.Reports[sender.UserID] = DateTime.Now;
                        }
                        else valid = false;
                    }
                    if (valid)
                    {
                        if (!before)
                        {
                            reportedClient.Reports.Add(sender.UserID, DateTime.Now);
                        }
                        Program.Write(
                                LogMessageType.ReportFromUser,
                                "User {0} was reported by {1} for the following reason: \n\t\"{2}\"",
                                reportedClient.Username, sender.Username, reason);
                        AdminMessage("Report successful, thank you", new[]{sender.UserID});

                        if (Program.Settings["autoban"]==true)
                        {
                            int reportsBevoidTime = Program.Settings["reportsBevoidTime"];
                            int reportMajority = Program.Settings["reportMajorityPercent"];
                            int validReports=reportedClient.Reports.Count(pair => (pair.Value.AddMinutes(reportsBevoidTime) > DateTime.Now));
                            if ((double)validReports / Connections.Count * 100 > reportMajority)
                            {
                                string endpoint = reportedClient.GetEndpoint();
                                Program.Kick(reportedClient, endpoint);
                                Program.RemoveBlacklist(endpoint, reportsBevoidTime * 60 / 2);
                            }
                        }
                    }
                }
            }
        }

        public void Broadcast(string message)
        {
            var watch = Stopwatch.StartNew();
            lock (this.Connections)
            {
                foreach (var user in this.Connections.Values)
                {
                    user.Send(CreatePacket(HeaderTypes.BROADCAST, message));
                }
                Program.Write("Message broadcasted to " + this.Connections.Count + " clients in " + watch.Elapsed.Milliseconds + " ms");
            }
            watch.Stop();
        }

        public void AdminMessage(string message, int[] ids)
        {
            for (int i = 0; i < ids.Length; i++)
                if (this.Connections.ContainsKey(ids[i]))
                    this.Connections[ids[i]].Send(CreatePacket(HeaderTypes.SYSTEM_MESSAGE, message));
        }

        public static String GetHeaderType(int header)
        {
            return GetHeaderType(header.ToString());
        }

        public static String GetHeaderType(string header)
        {
            
            switch (header)
            {
                case "32":
                    return "POST_MESSAGE";
                case "1":
                    return "INIT_CLIENT";
                case "E":
                    return "GET_ROLE_of";
                case "34":
                    return "NOTIFY_POST";
                case "29":
                    return "ADD_PREV_USER";
                case "31":
                    return "ADD_NEW_USER";
                case "12":
                    return "CLIENT_DISCONNECTED";
                case "3":
                    return "BROADCAST";
                case "4":
                    return "SYSTEM_MESSAGE";
                case "69":
                    return "SERVER_INFO";
                case "37":
                    return "RECEIVE_WHISPER";
                case "35":
                    return "SEND_WHISPER";
                case "38":
                    return "SENT_WHISPER";
                case "-38":
                    return "WHISPER_ERROR";
                case "-1":
                    return "KICK";
                case "41":
                    return "CHANGE_USERNAME_REQUEST";
                case "-41":
                    return "CHANGE_USERNAME_DENIED";
                case "42":
                    return "CHANGE_USERNAME_ANNOUNCE";
                default:
                    return "Unknown";
            }
        }

        internal static void OnError(Client client)
        {
            client.Socket.Close();
            client.ConnectionState = ConnectionState.Offline;
        }

        static byte[] ServerSocketAdminRejected = new byte[] { 49, 64, 81, 100 };
        static byte[] ServerSocketAdminAccepted = new byte[] { 64, 81, 100, 121 };
        static byte[] NameRequiredPacket = new byte[] { 2, 8, 18, 32 };
        static byte[] ServerIsFullPacket = new byte[] { 2, 18, 8, 32 };
        static byte[] FullAuthRequiredPacket = new byte[] { 2, 32, 8, 18 };
        static byte[] AccessGrantedPacket = new byte[] { 2, 32, 18, 8 };
        static byte[] AccessDeniedPacket = new byte[] { 2, 18, 32, 8 };
        static byte[] BlacklistedPacket = new byte[] { 33, 6, 1 };
        static byte[] PingPacket = new byte[] { 4, 36 };

        private Client OnClientConnected(Socket newGuy, ref ConnectionFlags flag)
        {
            switch (Server.AuthMethod)
            {
                case AuthMethod.UsernameOnly:
                    newGuy.Send(NameRequiredPacket);
                    break;
                case AuthMethod.Full:
                    newGuy.Send(FullAuthRequiredPacket);
                    break;
                case AuthMethod.InviteCode:
                default:
                    throw new NotImplementedException();
            }

            byte[] buffer = new byte[64];
            int bytesRead = 0;

            if (!Receive(newGuy, buffer, ref bytesRead))
                return OnConnectionError(ref flag);

            if (buffer[0] < 128)
            {
                if (buffer[0] == 0x45 && AuthMethod == ChatServer.AuthMethod.UsernameOnly) // UsernameOnly
                {
                    string username = Helper.XorText(enc.GetString(buffer, 1, bytesRead - 1), buffer[0]);
                    while (this.Connections.ValuesWhere(c => c.Username == username).Any() || Program.ReservedNames.Contains(username.ToLower()))
                        username += (char)Helper.Randomizer.Next((int)'a', (int)'z');

                    flag = ConnectionFlags.OK;
                    return new Client(username, newGuy);
                }
                else if (buffer[0] == 0x55 && AuthMethod == ChatServer.AuthMethod.Full)
                {
                    string[] data = GetLoginPacketParams(buffer, bytesRead);
                    if (Server.Password == data[1])
                    {
                        flag = ConnectionFlags.OK;
                        return new Client(data[0], newGuy);
                    }
                    else
                    {
                        flag = ConnectionFlags.BadPassword;
                        return null;
                    }
                }
                else if (buffer[0] == 0x65 && AuthMethod == ChatServer.AuthMethod.InviteCode)
                {
                    string[] data = GetLoginPacketParams(buffer, bytesRead);
                    if (Program.InviteCodes.Contains(data[1]))
                    {
                        flag = ConnectionFlags.OK;
                        return new Client(data[0], newGuy);
                    }
                    else
                    {
                        flag = ConnectionFlags.BadInviteCode;
                        return null;
                    }
                }
            }
            else
            {
                flag = ConnectionFlags.BadFirstPacket;
                return null;
            }

            return null;
        }

        private static string[] GetLoginPacketParams(byte[] buffer, int bytesRead)
        {
            string[] data = enc.GetString(buffer, 1, bytesRead - 1).Split('|').Select(s => Helper.XorText(s, buffer[0])).ToArray();
            return data;
        }

        static Client OnConnectionError(ref ConnectionFlags flag)
        {
            flag = ConnectionFlags.SocketError;
            return null;
        }

        static bool Receive(Socket socket, byte[] buffer, ref Int32 bytesRead)
        {
            try
            {
                bytesRead = socket.Receive(buffer, SocketFlags.None);
                if (bytesRead == 0) throw new SocketException();
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                try { socket.Close(); } catch (SocketException) { }
                return false;
            }
        }

        /// <summary>
        /// Represents any possible situation for when a user logs in
        /// </summary>
        internal enum ConnectionFlags
        {
            OK,
            None,
            Banned,
            BadFirstPacket,
            SocketError,
            BadPassword,
            BadInviteCode
        }
    }
}