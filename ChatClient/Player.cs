using System.Collections.Generic;

namespace ChatClient
{
    public class Player
    {
        static Player()
        {
            All = new Dictionary<int, Player>(25);
        }

        public Player(int id, string p)
        {
            this.UserID = id;
            this.Username = p;
        }
        public int UserID { get; set; }
        public string Username { get; set; }

        public static Dictionary<int, Player> All { get; set; }
    }
}
