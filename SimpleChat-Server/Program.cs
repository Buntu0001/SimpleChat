using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleChat_Server
{
    class Client
    {
        public string ip;
        public List<string> chatRoom = new List<string>();
        public string nickname;
        public TcpClient _client;
        public Thread th;
    }
    class Room
    {
        public string name;
        public string GUID;
        public string sort;
        public int max;
        public List<Client> list = new List<Client>();

    }
    class Program
    {
        private static int port = 2525;
        private static string logpath = System.IO.Directory.GetCurrentDirectory() + @"\logs";
        private static string savepath = System.IO.Directory.GetCurrentDirectory() + @"\db";
        private static string joinlog = System.IO.Directory.GetCurrentDirectory() + @"\logs\EventLogs.txt";
        private static string dblog = System.IO.Directory.GetCurrentDirectory() + @"\logs\DBLogs.txt";
        private static Dictionary<string, Client> clientList = new Dictionary<string, Client>();
        private static Dictionary<string, Room> roomList = new Dictionary<string, Room>();
        private static string ping = "ping!";
        private static string pong = "pong!";
        private static string splitter = "simplectsp";
        static void Main(string[] args)
        {
            Console.WriteLine("[INFO] Simple Chat 서버 부팅 중...");
            Console.WriteLine("[INFO] Made By BUNTU");
            DirectoryInfo di = new DirectoryInfo(logpath);
            if (di.Exists == false)
            {
                Console.WriteLine("[INFO] logs 폴더 생성 중...");
                di.Create();
                Console.WriteLine("[INFO] logs 폴더 생성 완료.");
                Console.WriteLine("[INFO] 경로: {0}", logpath);
            }
            di = new DirectoryInfo(savepath);
            if (di.Exists == false)
            {
                Console.WriteLine("[INFO] db 폴더 생성 중...");
                di.Create();
                Console.WriteLine("[INFO] db 폴더 생성 완료.");
                Console.WriteLine("[INFO] 경로: {0}", savepath);
            }
            FileInfo join = new FileInfo(joinlog);
            if (join.Exists == false)
            {
                Console.WriteLine("[INFO] EventLogs 파일 생성 중...");
                StreamWriter writer;
                writer = File.CreateText(joinlog);
                writer.WriteLine("####### SimpleChat Client Event Log #######");
                writer.Close();
                Console.WriteLine("[INFO] EventLog 파일 생성 완료.");
                Console.WriteLine("[INFO] 경로: {0}", joinlog);
            }
            join = new FileInfo(dblog);
            if (join.Exists == false)
            {
                Console.WriteLine("[INFO] DBLogs 파일 생성 중...");
                StreamWriter writer;
                writer = File.CreateText(dblog);
                writer.WriteLine("####### SimpleChat ChatRoom DB Log #######");
                writer.Close();
                Console.WriteLine("[INFO] DBlogs 파일 생성 완료.");
                Console.WriteLine("[INFO] 경로: {0}", dblog);
            }
            Console.WriteLine("[INFO] 채팅방 불러오는 중...");
            parseDB();
            Thread t1 = new Thread(Listen);
            t1.Start();
            Console.WriteLine("");
            Console.WriteLine("[INFO] 서버 시작! 클라이언트 연결 대기중...");
            Console.WriteLine("");
            while (true)
            {
                Console.Write("SimpleChat-Server > ");
                string command = Console.ReadLine();
                string[] cmd = command.Split(' ');
                if (cmd.Length >= 2)
                {
                    if (cmd[0].Equals("room"))
                    {
                        if (cmd[1].Equals("create"))
                        {
                            if (cmd.Length == 5)
                            {
                                createRoom(cmd[2], cmd[3], Int32.Parse(cmd[4]));
                            }
                            else
                            {
                                Console.WriteLine("[WARN] room create <name> <sort> <min> <max>");
                            }
                        }
                        else if (cmd[1].Equals("delete"))
                        {
                            if (cmd.Length == 3)
                            {
                                destroyRoom(cmd[2]);
                            }
                            else
                            {
                                Console.WriteLine("[WARN] room delete <GUID>");
                            }
                        }
                        else
                        {
                            Console.WriteLine("[WARN] 잘못된 명령어!1");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[WARN] 잘못된 명령어!2");
                    }
                }
                else
                {
                    Console.WriteLine("[WARN] 잘못된 명령어!3");
                }
            }
        }
        private static void createClient(TcpClient client)
        {
            Client cl = new Client();
            cl._client = client;
            cl.ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
            string curTime = System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss");
            StreamWriter writer;
            writer = File.AppendText(joinlog);
            writer.WriteLine("[{0}] ip: {1} 에서 접속", curTime, cl.ip);
            writer.Close();
            clientList.Add(cl.ip, cl);
            Console.WriteLine("");
            Console.WriteLine("[INFO] 클라이언트 접속! ip: {0}", ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString());
            Console.Write("SimpleChat-Server > ");
            if (roomList != null)
            {
                foreach (KeyValuePair<string, Room> kv in roomList)
                {
                    sendData(1, new object[] { cl, kv.Value });
                    Thread.Sleep(100);
                }
            }
            var t = new Thread(() => checkLive(cl));
            cl.th = t;
            t.Start();
        }
        private static void destroyClient(Client client)
        {
            Console.WriteLine("");
            Console.WriteLine("[INFO] 클라이언트 접속 끊김! ip: {0}",
                ((IPEndPoint) client._client.Client.RemoteEndPoint).Address.ToString());
            Console.Write("SimpleChat-Server > ");
            client.th.Abort();
            string curTime = System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss");
            StreamWriter writer;
            writer = File.AppendText(joinlog);
            writer.WriteLine("[{0}] ip: {1} 에서 접속 끊김", curTime, client.ip);
            writer.Close();
            clientList.Remove(client.ip);
        }
        private static void createRoom(string _name, string _sort, int _max)
        {
            Room room = new Room();
            room.name = _name;
            room.sort = _sort;
            room.max = _max;
            room.GUID = guidGenerate();
            FileInfo logs = new FileInfo(System.IO.Directory.GetCurrentDirectory() + @"\logs\" + room.GUID + ".txt");
            if (logs.Exists == false)
            {
                StreamWriter writer;
                writer = File.CreateText(logs.ToString());
                writer.WriteLine("####### GUID: {0} Chat Log #######", room.GUID);
                writer.Close();
            }

            foreach (KeyValuePair<string, Client> kv in clientList)
            {
                sendData(1, new object[] { kv.Value, room });
            }
            StreamWriter writer2;
            writer2 = File.CreateText(savepath + @"\" + room.GUID + ".txt");
            writer2.WriteLine("name: {0}", room.name);
            writer2.WriteLine("GUID: {0}", room.GUID);
            writer2.WriteLine("sort: {0}", room.sort);
            writer2.WriteLine("max: {0}", room.max);
            writer2.Close();
            string curTime = System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss");
            writer2 = File.AppendText(dblog);
            writer2.WriteLine("[{0}] name: {1} GUID: {2} sort: {3} max: {4} 생성", curTime, room.name, room.GUID, room.sort, room.max);
            writer2.Close();
            roomList.Add(room.GUID, room);
            Console.WriteLine("채팅방 생성 완료");
            Console.WriteLine("이름: {0} GUID: {1} 종류: {2} 최대인원: {3}", room.name, room.GUID, room.sort, room.max);
            Console.Write("SimpleChat-Server > ");
        }
        private static void destroyRoom(string _GUID)
        {
            Room room = roomList[_GUID];
            roomList.Remove(_GUID);
            foreach (KeyValuePair<string, Client> kv in clientList)
            {
                sendData(2, new object[] { kv.Value, room });
            }
            System.IO.File.Delete(savepath + @"\" + room.GUID + ".txt");
            System.IO.File.Delete(System.IO.Directory.GetCurrentDirectory() + @"\logs\" + room.GUID + ".txt");
            StreamWriter writer2;
            string curTime = System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss");
            writer2 = File.AppendText(dblog);
            writer2.WriteLine("[{0}] name: {1} GUID: {2} sort: {3} max: {4} 삭제", curTime, room.name, room.GUID, room.sort, room.max);
            writer2.Close();
            Console.WriteLine("채팅방 삭제 완료");
            Console.WriteLine("GUID: {0}", room.GUID);
            Console.Write("SimpleChat-Server > ");
            room = null;
        }
        private static void Listen()
        {
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                createClient(client);
            }
        }
        
        private static void checkLive(Client client)
        {
            while (true)
            {
                try
                {
                    NetworkStream ns = client._client.GetStream();
                    StreamReader sr = new StreamReader(ns);
                    StreamWriter sw = new StreamWriter(ns);
                    sw.WriteLine(ping);
                    sw.Flush();
                    string recv = sr.ReadLine();
                    if (recv.Equals(pong))
                    {
                        continue;
                    }
                    else if (String.IsNullOrEmpty(recv))
                    {
                        var t = new Thread(() => destroyClient(client));
                        t.Start();
                        break;
                    }
                    else if (recv.Contains("join") || recv.Contains("exit") || recv.Contains("send"))
                    {
                        parseData(new object[] {client, recv});
                    }
                    else
                    {
                        var t = new Thread(() => destroyClient(client));
                        t.Start();
                        break;
                    }
                }
                catch
                {
                    var t = new Thread(() => destroyClient(client));
                    t.Start();
                    break;
                }
            }
        }

        private static void parseDB()
        {
            int count = 0;
            foreach (string path in SearchFile(savepath))
            {
                string[] roomInfo = new string[4];
                roomInfo = System.IO.File.ReadAllLines(path);
                Room room = new Room();
                room.name = roomInfo[0].Substring(6);
                room.GUID = roomInfo[1].Substring(6);
                room.sort = roomInfo[2].Substring(6);
                room.max = Int32.Parse(roomInfo[3].Substring(5));
                roomList.Add(roomInfo[1].Substring(6), room);
                Console.WriteLine("[INFO] name: {0} GUID: {1} sort: {2} max: {3}", room.name, room.GUID, room.sort, room.max);
                count++;
            }
            if (count == 0)
            {
                Console.WriteLine("[WARN] 채팅방을 찾을 수 없습니다.");
            }
        }

        private static void joinClient(object[] param)
        {
            Client client = (Client) param[0];
            Room room = (Room) param[1];
            client.chatRoom.Add(room.GUID);
            room.list.Add(client);
            Thread.Sleep(100);
            StreamWriter writer2;
            string curTime = System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss");
            writer2 = File.AppendText(logpath + @"\" + room.GUID + ".txt");
            writer2.WriteLine("[{0}] {1} 에서 채팅방 입장", curTime, client.ip);
            writer2.Close();
            sendData(3, new object[] { client, room ,"accept" });
            Thread.Sleep(700);
            broadcastData(new object[] { client, room, "[+] " + client.ip + " 님이 " + room.name + " 채팅방에 접속하셨습니다." });
        }

        private static void exitClient(object[] param)
        {
            Client client = (Client) param[0];
            Room room = (Room) param[1];
            client.chatRoom.Remove(room.GUID);
            room.list.Remove(client);
            Thread.Sleep(100);
            StreamWriter writer2;
            string curTime = System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss");
            writer2 = File.AppendText(logpath + @"\" + room.GUID + ".txt");
            writer2.WriteLine("[{0}] {1} 에서 채팅방 퇴장", curTime, client.ip);
            writer2.Close();
            Thread.Sleep(700);
            broadcastData(new object[] { client, room, "[-] " + client.ip + " 님이 " + room.name + " 채팅방에서 퇴장하셨습니다." });
        }
        public static string[] SearchFile(string _strPath)
        {
            string[] files = { "", };
            try
            {
                files = Directory.GetFiles(_strPath, "*.*", SearchOption.AllDirectories);
            }
            catch
            {

            }
            return files;
        }
        private static string guidGenerate()
        {
            Guid myuuid = Guid.NewGuid();
            string myuuidAsString = myuuid.ToString();
            return myuuidAsString;
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
        private static void broadcastData(object[] param)
        {
            Client client = (Client)param[0];
            Room room = (Room) param[1];
            StreamWriter writer2;
            string curTime = System.DateTime.Now.ToString("yyyy:MM:dd:HH:mm:ss");
            writer2 = File.AppendText(logpath + @"\" + room.GUID + ".txt");
            writer2.WriteLine("[{0}] {1}: {2}", curTime, client.ip, param[2]);
            writer2.Close();
            sendData(4, new object[] { client, room, param[2] });
        }
        private static void sendData(int type, object[] param)
        {
            Client client = (Client)param[0];
            Room room = (Room)param[1];
            NetworkStream ns = client._client.GetStream();
            StreamReader sr = new StreamReader(ns);
            StreamWriter sw = new StreamWriter(ns);
            switch (type)
            {
                case 1: // Add Room
                    sw.WriteLine(msgGenerate(new object[] { "addroom", room.name, room.GUID, room.sort, room.max }));
                    sw.Flush();
                    break;
                case 2: // Delete Room
                    sw.WriteLine(msgGenerate(new object[] { "deleteroom", room.GUID }));
                    sw.Flush();
                    break;
                case 3: // Determine Join Room
                    sw.WriteLine(msgGenerate(new object[] { "join" + param[2], room.GUID } ));
                    sw.Flush();
                    break;
                case 4: // Broadcast Data to Room
                    sw.WriteLine(msgGenerate(new object[] { "receive", room.GUID, client.ip, param[2] } ));
                    sw.Flush();
                    break;
            }
        }
        private static void parseData(object[] param)
        {
            Client client = (Client)param[0];
            string[] sp = param[1].ToString().Split(new string[] { splitter }, StringSplitOptions.None);
            Room room = roomList[sp[1]];
            switch (sp[0])
            {
                    case "join": // Room Join
                        if (room.list.Count < room.max)
                        {
                            if (!room.list.Contains(client))
                            {
                                joinClient(new object[] {client, room});
                            }
                            else if (room.list.Contains(client))
                            {
                                {
                                    sendData(3, new object[] { client, room, "deny" });
                                }
                            }
                        }
                        else if (room.list.Count >= room.max)
                        {
                            sendData(3, new object[] { client, room, "deny" });
                        }
                        break;
                    case "exit": // Room Exit
                        if (room.list.Contains(client))
                        {
                            exitClient(new object[] {client, room});
                        }

                        break;
                    case "send": // Chat Send    send | GUID | Text
                        if (room.list.Contains(client))
                        {
                            broadcastData(new object[] { client, room, sp[2] });
                        }
                        break;
            }
        }
    }
}
