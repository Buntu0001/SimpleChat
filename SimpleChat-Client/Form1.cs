using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SimpleChat_Client
{
    public enum ErrorCode
    {
        Data_Damaged = 0x64,
        Data_Incorrect = 0x79,
        TCP_Error_Server_Not_Found = 0x51,
        TCP_Error_Server_Disconnect = 0x40,
    }

    class Room
    {
        public string name { get; set; }
        public string GUID { get; set; }
        public string sort { get; set; }
        public int max { get; set; }
    }

    public partial class Form1 : Form
    {
        public delegate void ParentForm(object[] param);
        public delegate void ChildForm(object[] param);

        public event ParentForm ParentFormEvent;
        private static string last_request;
        private static string last_message;
        private static int port = 2525;
        private static string host = Dns.GetHostAddresses("loamap.p-e.kr")[0].ToString();
        private static TcpClient client;
        private static string ping = "ping!";
        private static string pong = "pong!";
        private static Thread liveThread;
        private static Thread t1;
        private static string splitter = "simplectsp";
        private static Dictionary<string, Room> roomList = new Dictionary<string, Room>();
        private static Dictionary<string, Form2> chatList = new Dictionary<string, Form2>();

        public Form1()
        {
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            t1 = new Thread(Start);
            t1.Start();
        }

        private void Start()
        {
            try
            {
                client = new TcpClient(host, port);
                liveThread = new Thread(receiveData);
                liveThread.Start();
            }
            catch
            {
                MsgBox($"서버를 찾을 수 없습니다. (error code: {ErrorCode.TCP_Error_Server_Not_Found})");
            }
        }

        private void reloadChatRoom()
        {
            listView1.Items.Clear();
            foreach (KeyValuePair<string, Room> kv in roomList)
            {
                var row = new string[] {kv.Value.name, kv.Value.sort, kv.Value.max.ToString()};
                var item = new ListViewItem(row);
                listView1.Items.Add(item);
            }
        }

        private void receiveData()
        {
            while (true)
            {

                    NetworkStream ns = client.GetStream();
                    StreamReader sr = new StreamReader(ns);
                    StreamWriter sw = new StreamWriter(ns);
                    string recv = sr.ReadLine();
                    if (recv.Equals(ping))
                    {
                        sw.WriteLine(pong);
                        sw.Flush();
                    }
                    else if (recv.Contains("join") || recv.Contains("addroom") || recv.Contains("deleteroom") ||
                             recv.Contains("receive"))
                    {
                        parseData(recv);
                    }
                    else
                    {
                        MsgBox($"서버를 찾을 수 없습니다123. (error code: {ErrorCode.TCP_Error_Server_Disconnect})");
                    }
                
            }
        }

        private static void MsgBox(string msg)
        {
            SystemSounds.Beep.Play();
            MessageBox.Show(msg, "SimpleChat", MessageBoxButtons.OK);
        }

        public void EventMethod(object[] param)
        {
            if (param[0].Equals("exit"))
            {
                chatList[param[1].ToString()] = null;
                chatList.Remove(param[1].ToString());
                sendData(3, new object[] {param[1]});
            }
            else
            {
                sendData(2, new object[] {param[0], param[1]});
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            System.Diagnostics.Process[] mProcess =
                System.Diagnostics.Process.GetProcessesByName(Application.ProductName);
            foreach (System.Diagnostics.Process p in mProcess)
                p.Kill();
        }

        private void parseData(string msg)
        {
            string[] sp = msg.Split(new string[] {"simplectsp"}, StringSplitOptions.None);
            switch (sp[0])
            {
                case "joinaccept":
                    if (!sp[1].Equals(last_request))
                    {
                        MsgBox($"무결성 오류. (error code: {ErrorCode.Data_Incorrect})");
                        last_request = null;
                    }
                    else if (sp[1].Equals(last_request))
                    {
                        
                        this.Invoke(new Action(
                            delegate()
                            {
                                chatList.Add(sp[1], new Form2(sp[1]));
                                this.ParentFormEvent += chatList[sp[1]].EventMethod;
                                chatList[sp[1]].ChildFormEvent += this.EventMethod;
                                chatList[sp[1]].Show();
                            }));
                        
                    }
                    else
                    {
                        MsgBox($"무결성 오류. (error code: {ErrorCode.Data_Damaged})");
                        last_request = null;
                    }

                    break;
                case "receive": // receive | GUID | ip | text
                    if (roomList.ContainsKey(sp[1]))
                    {
                        if (chatList.ContainsKey(sp[1]))
                        {
                            if (ParentFormEvent != null)
                            {
                                ParentFormEvent(new object[] {sp[1], sp[2], sp[3]});
                            }
                        }
                    }
                    break;
                case "joindeny":
                    if (!sp[1].Equals(last_request))
                    {
                        MsgBox($"무결성 오류. (error code: {ErrorCode.Data_Incorrect})");
                        last_request = null;
                    }
                    else if (sp[1].Equals(last_request))
                    {
                        MsgBox("해당 채팅방으로써의 접근권한이 없습니다.");
                    }
                    else
                    {
                        MsgBox($"무결성 오류. (error code: {ErrorCode.Data_Damaged})");
                        last_request = null;
                    }
                    break;
                case "addroom":
                    Room room = new Room();
                    room.name = sp[1];
                    room.GUID = sp[2];
                    room.sort = sp[3];
                    room.max = Int32.Parse(sp[4]);
                    roomList.Add(room.GUID, room);
                    this.Invoke(new Action(
                        delegate() { reloadChatRoom(); }));
                    break;
                case "deleteroom":
                    roomList.Remove(sp[1]);
                    this.Invoke(new Action(
                        delegate() { reloadChatRoom(); }));
                    break;
            }
        }

        private static string msgGenerate(object[] param)
        {
            StringBuilder build = new StringBuilder();
            for (int i = 0; i < param.Length; i++)
            {
                if (i == param.Length - 1)
                {
                    build.Append(param[i]);
                }
                else
                {
                    build.Append(param[i]);
                    build.Append(splitter);
                }
            }

            return build.ToString();
        }

        private void sendData(int type, object[] param)
        {
            Room room = roomList[param[0].ToString()];
            NetworkStream ns = client.GetStream();
            StreamReader sr = new StreamReader(ns);
            StreamWriter sw = new StreamWriter(ns);
            switch (type)
            {
                case 1: // request Join ChatRoom
                    last_request = room.GUID;
                    sw.WriteLine(msgGenerate(new object[] {"join", room.GUID}));
                    sw.Flush();
                    break;
                case 2: // request Send Data
                    sw.WriteLine(msgGenerate(new object[] {"send", room.GUID, param[1]}));
                    sw.Flush();
                    break;
                case 3: // request Exit ChatRoom
                    sw.WriteLine(msgGenerate(new object[] {"exit", room.GUID}));
                    sw.Flush();
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 1)
            {
                int i = listView1.SelectedItems[0].Index;
                sendData(1, new object[] {roomList.ElementAt(i).Value.GUID});
            }
        }
    }
}