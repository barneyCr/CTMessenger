using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient
{
    public partial class SettingsEditor : Form
    {
        public Color[] colors;
        private Properties.Settings settings = global::ChatClient.Properties.Settings.Default;

        public SettingsEditor()
        {
            this.InitializeComponent();

            this.colors = new[] { settings.UserPostMessage, settings.SystemMessage, settings.UserDisConnect };
            this.FormClosing += SettingsEditor_FormClosing;
        }

        void SettingsEditor_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.settings.UserPostMessage = colors[0];
            this.settings.SystemMessage = colors[1];
            this.settings.UserDisConnect = colors[2];

            this.settings.Save();
        }

        private void selectionChanged(object sender, EventArgs e)
        {
            this.label3.ForeColor = this.colors[this.listBox1.SelectedIndex];
            this.label3.Text = this.colors[this.listBox1.SelectedIndex].ToString().Substring(5).Trim('[', ' ', ']');
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.colorDialog1.ShowDialog();
            this.colors[this.listBox1.SelectedIndex] = this.colorDialog1.Color;
            this.selectionChanged(this, EventArgs.Empty);
        }
    }
}
