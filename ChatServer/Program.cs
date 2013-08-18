using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace ChatServer
{
    partial class Program
    {
        public static readonly string[] ReservedNames = new[] { "admin", "system", "server" };

        const ConsoleColor DefaultColor = ConsoleColor.DarkGray;
        static Server server;
        static bool WriteInLogFile;

        static StreamWriter writer = new StreamWriter("logs.txt", true);

        public static void Write(string msg, string header = "", ConsoleColor color = DefaultColor)
        {
            string msg_ = (!string.IsNullOrWhiteSpace(header) ? (string.Concat(DateTime.Now.ToLongTimeString(), " >>> [", header, "]  ", msg)) : (string.Concat(">>>", "   ", msg)));
            Console.ForegroundColor = color;
            Console.WriteLine(msg_);
            Console.ForegroundColor = DefaultColor;

            if (WriteInLogFile)
            {
                writer.WriteLine(msg_);
                writer.Flush();
            }
        }
        public static void Write(LogMessageType type, string msg, params object[] obj)
        {
            string head = GetEnum(type);
            msg = string.Format(msg, obj);
            switch (type)
            {
                case LogMessageType.Config:
                    Write(msg, head, ConsoleColor.Red);
                    break;

                case LogMessageType.Network:
                    Write(msg, head, ConsoleColor.Green);
                    break;

                case LogMessageType.Chat:
                    Write(msg, head, ConsoleColor.DarkCyan);
                    break;

                case LogMessageType.Auth:
                    Write(msg, head);
                    break;

                case LogMessageType.UserEvent:
                    Write(msg, head, ConsoleColor.DarkYellow);
                    break;

                case LogMessageType.Packet:
                    Write(msg, head);
                    break;

                default:
                    Write(msg);
                    break;
            }
        }

        static void Main(string[] args)
        {
            try
            {
                var watch = Stopwatch.StartNew();
                Settings = Program.LoadSettings();
                WriteInLogFile = Settings["writeInFile"];
                InitializeConsole();

                server = new Server(Settings["serverPort"], Settings["maxClients"], Parse<AuthMethod>(Settings["authMethod"]), Settings["passKey"]);
                server.listenThread.Start();
                watch.Stop();
                Program.Write("Loaded server in " + watch.Elapsed.TotalSeconds + " seconds", "Trace");
                string line = "";
                while ((line = Console.ReadLine()) != "close server now")
                {
                    try
                    {
                        if (line == "clients")
                            foreach (var pair in server.Connections)
                                Console.WriteLine("{0}\t-\t{1}", pair.Key, pair.Value.Username);
                        else if (line == "settings")
                            foreach (var pair in Settings)
                                Console.WriteLine("{0} \t -\t {1}", pair.Key, pair.Value);
                        else if (line == "cls")
                            Console.Clear();
                        else if (line[0] == 'b')
                            server.Broadcast(line.Substring(2));
                        else if (line[0] == 'a')
                        {
                            int[] uids = new String(line.Substring(2).TakeWhile(c => c != ' ').ToArray()).Split(',').Select(s => int.Parse(s)).ToArray();
                            server.AdminMessage(line.Substring(line.IndexOf('^') + 1), uids);
                        }
                        else if (line[0] == 'k')
                        {
                            Client client = server.Connections[line.Substring(2).ToInt()];
                            var endpoint = client.Socket.RemoteEndPoint.ToString().Split(':')[0];

                            server.Broadcast(String.Format("{0} [{1}] has been kicked from the server!" , client.Username, client.UserID));
                            server.Blacklist.Add(endpoint);
                            client.Send("-1");

                            Server.OnError(client);
                            RemoveBlacklist(endpoint);
                        }
                    }
                    catch { }
                }
            }
            finally
            {
                server = null;
                writer.Dispose();
            }
        }


        static async void RemoveBlacklist(string p)
        {
            await Task.Delay(180000);
            server.Blacklist.Remove(p);
            Write(LogMessageType.UserEvent, "Removed {0} from blacklist", p);
        }

        static void InitializeConsole()
        {
            Console.Title = "Chat Server";
            Console.BackgroundColor = Parse<ConsoleColor>(Settings["consoleBackColor"]);
            Console.SetWindowSize(105, 25);
            Console.SetBufferSize(105, 1500);

            Console.Clear();
            Program.Write(LogMessageType.Config, "Settings loaded");
        }

        static string GetEnum<T>(T _enum) where T : struct
        {
            return Enum.GetName(typeof(T), _enum);
        }

        static T Parse<T>(string from) where T : struct // == where T : Enum
        {
            T auth;
            if (Enum.TryParse<T>(from, true, out auth))
                return auth;
            else return default(T);
        }
    }
}