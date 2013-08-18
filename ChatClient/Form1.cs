using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ChatClient
{
    public delegate void WriteLogDelegate(string format, params object[] obj);
    public partial class Form1 : Form
    {

        public void WriteLog(string msg, params object[] obj)
        {
            this.logBox.Invoke(new Action(() => {
                this.logBox.AppendText("\r\n" + string.Concat(DateTime.Now.ToShortTimeString(), " -> ", string.Format(msg, obj)));
            }));
        }

        public NetworkHelper helper;

        public Form1()
        {
            InitializeComponent();
        }

        async void button1_Click(object sender, EventArgs e)
        {
            string[] data = ipBox.Text.Split(':');

            try
            {
                WriteLog("Trying to connect to {0} on port {1}", data[0], data[1]);

                helper = new NetworkHelper(data[0], int.Parse(data[1]), unameBox.Text, passwordBox.Text, this.WriteLog);
                if (helper.Connect())
                {
                    WriteLog("Connection successful !");
                    Program.Callback = () => Application.Run(new MainForm(helper));
                    await Task.Delay(500);
                    this.Close();
                }
                else
                {
                    WriteLog("Connection error !");
                }
            }
            catch (FormatException)
            {
                WriteLog("Error on parsing port number or IP Address");
            }
            catch (IndexOutOfRangeException)
            {
                WriteLog("You must specify a port number! <IP>:<port>");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var settings = global::ChatClient.Properties.Settings.Default;

            string ip = settings.PreferredIP,
                username = settings.Username;

            if (!string.IsNullOrWhiteSpace(ip) && this.ipBox.Text == string.Empty)
                this.ipBox.Text = ip;

            if (!string.IsNullOrWhiteSpace(username) && this.unameBox.Text == string.Empty)
                this.unameBox.Text = username;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                button1_Click(this, e);
            }
        }
    }
}