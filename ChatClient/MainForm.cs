using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class MainForm : Form
    {
        private Properties.Settings settings = global::ChatClient.Properties.Settings.Default;

        readonly NetworkHelper helper;
        readonly Action<String> SystemMessage;

        public void WriteLog(string str, Color col)
        {
            this.Invoke(new MethodInvoker(() => {
                int pos = this.logBox.TextLength;
                this.logBox.AppendText(str + "\n");

                this.logBox.Select(pos, str.Length);
                this.logBox.SelectionColor = col;
                this.logBox.Select();
                this.inputBx.Focus();
            }));
        }

        public void WriteLog(string msg)
        {
            this.WriteLog(msg, Color.DarkBlue);
        }

        public void AnnounceDisConnection(string username, int id, bool conn)
        {
            this.WriteLog(string.Format("{0} [{1}] {2}!", username, id, conn ? "connected" : "disconnected"), settings.UserDisConnect);
        }

        public void ReceivedWhisper(int senderID, string message)
        {
            Player sender = Player.All[senderID];
            this.WriteLog(String.Format("{0} whispered to you: {1}", sender.Username, message), settings.ReceiveWhisper);
        }
        public void SentWhisper(int targetID, string message)
        {
            Player target = Player.All[targetID];
            this.WriteLog(String.Format("You whispered to {0}: {1}", target.Username, message), settings.SentWhisper);
        }

        public void WriteInChat(string senderName, int id, string msg)
        {
            this.WriteLog(
                string.Format("{0} {1} [{2}]: {3}", DateTime.Now.ToShortTimeString(), senderName, id, msg),
                senderName != this.helper.Username ? settings.UserPostMessage : settings.MePostMessage);
        }

        MainForm()
        {
            InitializeComponent();

            this.KeyPreview = true;
            this.inputBx.KeyDown += tbKeyDown;

            this.SystemMessage = (s) => this.WriteLog(s, settings.SystemMessage);
        }
        
        void tbKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                this.button1.PerformClick();
        }

        public MainForm(NetworkHelper helper) : this()
        {
            this.helper = helper;
            helper.Form = this;

            this.Text = "Chat client on " + helper.IP;
            this.logBox.AppendText("Welcome, " + helper.Username + "!\n");

            helper.ConnectionLost += ConnectionLost;
            helper.ReconnectTick += helper_ReconnectTick;
            helper.Reconnected += helper_Reconnected;
            this.inputBx.TextChanged += OnInputChanged;
        }

        private void OnInputChanged(object sender, EventArgs e)
        {
            this.button1.Enabled = !String.IsNullOrWhiteSpace(this.inputBx.Text);
        }

        void helper_Reconnected()
        {
            this.Invoke(new Action(() => {
                this.button1.Enabled = true;
                this.inputBx.TextChanged += OnInputChanged;
                this.SystemMessage("Reconnected !");
            }));
        }

        void helper_ReconnectTick()
        {
            this.SystemMessage("Trying to reconnect to " + this.helper.IP + "...");
        }

        private void ConnectionLost()
        {
            this.Invoke(new Action(() => {
                this.button1.Enabled = false;
                this.inputBx.TextChanged -= this.OnInputChanged;
                this.SystemMessage("Connection to server on " + this.helper.IP + " lost.");
            }));
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string text = this.inputBx.Text.TrimEnd().Replace('|', ' ');
            if (String.IsNullOrWhiteSpace(text))
                return;
            if (!text.StartsWith("/"))
                helper.Send("32|{0}", text);
            else if (text.Length>1)
            {
                switch (text[1])
                {
                    case 'w':
                        string[] data = text.Split(' ');
                        helper.Send("35|" + data[1] + "|" + String.Join(" ", data.Skip(2).ToArray()));
                        break;

                    case 'o':
                        this.SystemMessage("\nUsers online:");
                        foreach (var user in Player.All.Values)
                            this.WriteLog(String.Format("{0} -> UserID: {1}", user.UserID, user.Username), Color.RoyalBlue);
                        this.logBox.AppendText("\n\n");
                        break;

                    case 'c':
                        this.logBox.ResetText();
                        break;

                    case 'D':

                        break;

                    case 'u':
                        helper.Send("41|" + text.Substring(3));
                        break;
                }
            }
            this.inputBx.ResetText();
        }

        private void editColorsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new SettingsEditor().Show();
        }
    }
}
