using System;
using System.IO;
using System.Media;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace SimpleChat_Client
{
    public partial class Form2 : Form
    {

        public event Form1.ChildForm ChildFormEvent;
        private string GUID;
        private string splitter = "simplectsp";
        private bool chk = false;
        public Form2(string _GUID)
        {
            InitializeComponent();
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            GUID = _GUID;
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            Form1 frm = new Form1();
            frm.ParentFormEvent += EventMethod;
            chk = true;
        }

        public void EventMethod(object[] param)
        {
            if (param[0].Equals(GUID))
            {
                if (chk)
                {
                    DisplayText(new object[] {param[1], param[2]});
                }
            }
        }
        private void DisplayText(object[] param)
        {
            if (richTextBox1.InvokeRequired)
            {
                richTextBox1.BeginInvoke(new MethodInvoker(delegate
                {
                    if (param[1].ToString().Contains("접속") || param[1].ToString().Contains("퇴장"))
                    {
                        richTextBox1.AppendText(param[1] + Environment.NewLine);
                    }
                    else
                    {
                        richTextBox1.AppendText(param[0] + ": " + param[1] + Environment.NewLine);
                    }
                }));
            }
            else
            {
                if (param[1].ToString().Contains("접속") || param[1].ToString().Contains("퇴장"))
                {
                    richTextBox1.AppendText(param[1] + Environment.NewLine);
                }
                else
                {
                    richTextBox1.AppendText(param[0] + ": " + param[1] + Environment.NewLine);
                }
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (!String.IsNullOrWhiteSpace(textBox1.Text))
            {
                ChildFormEvent(new object[] {GUID, textBox1.Text});
                textBox1.Text = null;
            }
        }
        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (!String.IsNullOrWhiteSpace(textBox1.Text))
                {
                    ChildFormEvent(new object[] {GUID, textBox1.Text});
                    textBox1.Text = null;
                }
            }
        }
        private static void MsgBox(string msg)
        {
            SystemSounds.Beep.Play();
            MessageBox.Show(msg, "SimpleChat", MessageBoxButtons.OK);
        }

        private void Form2_FormClosing(object sender, FormClosingEventArgs e)
        {
            chk = false;
            ChildFormEvent(new object[] {"exit", GUID});
        }
    }
}