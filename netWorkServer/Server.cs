using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace netWorkServer
{
    class Server
    {
        private readonly static int BufferSize = 4096;
        private Random rand = new Random();
        public static void Main()
        {
            try
            {
                new Server().Init();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private ArrayList connectClients = new ArrayList();

        private Dictionary<string, Socket> generalClients = new Dictionary<string, Socket>();

        private Dictionary<string, Socket> anonymousClients = new Dictionary<string, Socket>();

        private ArrayList waitState = new ArrayList();

        private Dictionary<string, Socket[]> chattingClients = new Dictionary<string, Socket[]>();
        private Dictionary<string, Socket> openChattingClients = new Dictionary<string, Socket>();

        public Dictionary<string, Socket> GeneralClients
        {
            get => generalClients;
            set => generalClients = value;
        }
        public Dictionary<string, Socket> AnonymousClients
        {
            get => anonymousClients;
            set => anonymousClients = value;
        }
        public Dictionary<string, Socket[]> ChattingClients
        {
            get => chattingClients;
            set => chattingClients = value;
        }

        public Dictionary<string, Socket> OpenChattingClients
        {
            get => openChattingClients;
            set => openChattingClients = value;
        }

        private Socket ServerSocket;

        private readonly IPEndPoint EndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 3000);

        int clientNum;
        int anonymousNum;
        Server()
        {
            ServerSocket = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp
            );
            clientNum = 0;
            anonymousNum = 0;
        }

        void Init()
        {
            ServerSocket.Bind(EndPoint);
            ServerSocket.Listen(100);
            Console.WriteLine("서버 활성화.");

            Accept();

        }


        void Accept()
        {
            do
            {
                Socket client = ServerSocket.Accept();


                Console.WriteLine($"Client accepted: {client.RemoteEndPoint}.");

                SocketAsyncEventArgs args = new SocketAsyncEventArgs();
                args.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                client.ReceiveAsync(args);

            } while (true);
        }

        void Disconnected(Socket client)
        {
            Console.WriteLine($"Client disconnected: {client.RemoteEndPoint}.");
            foreach (KeyValuePair<string, Socket> clients in generalClients.ToArray())
            {
                if (clients.Value == client)
                { 
                    generalClients.Remove(clients.Key);
                    connectClients.Remove(clients.Key);
                    generalBroadcast(clients.Value, $"disConnect,{clients.Key}");
                    break;
                }
            }
            foreach (KeyValuePair<string, Socket> clients in anonymousClients.ToArray())
            {
                if (clients.Value == client)
                {
                    if (waitState.Contains(clients.Key))
                    {
                        waitState.Remove(clients.Key);
                    }
                    anonymousClients.Remove(clients.Key);
                    connectClients.Remove(clients.Key);
                    break;
                }
            }
            foreach (KeyValuePair<string, Socket> clients in openChattingClients.ToArray())
            {
                if (clients.Value == client)
                {
                    openChattingClients.Remove(clients.Key);
                    break;
                }
            }
            foreach (KeyValuePair<string, Socket[]> clients in chattingClients.ToArray())
            {
                if (clients.Value[0] == client)                                                                         // 클라이언트가 존재할때,
                {
                    foreach (KeyValuePair<string, Socket[]> anotherClients in chattingClients.ToArray())
                    {
                        if (anotherClients.Value[1] == client)
                        {
                            anotherClients.Value[0].Send(Encoding.Unicode.GetBytes("disconnectChat_"));
                            chattingClients.Remove(anotherClients.Key);
                        }
                    }
                    chattingClients.Remove(clients.Key);
                    anonymousClients.Remove(clients.Key);
                    break;
                }
            }
            client.Close();
        }

        void Received(object? sender, SocketAsyncEventArgs e)
        {
            Socket client = (Socket)sender!;
            byte[] data = new byte[BufferSize];
            try
            {
                int n = client.Receive(data);
                if (n > 0)
                {

                    //
                    MessageProc(client, data);

                    SocketAsyncEventArgs argsR = new SocketAsyncEventArgs();
                    argsR.Completed += new EventHandler<SocketAsyncEventArgs>(Received);
                    client.ReceiveAsync(argsR);
                }
                else { throw new Exception(); }
            }
            catch (Exception)
            {
                Disconnected(client);
            }
        }

        void MessageProc(Socket s, byte[] bytes)
        {
            string m = Encoding.Unicode.GetString(bytes);
            m = m.Trim().Replace("\0", "");
            string[] tokens = m.Split('_');
            string userID;
            string mode = tokens[0];
            bool isID = false;

            if (mode.Equals("anonymous"))
            {
                userID = tokens[1];
                if (connectClients.Contains(userID))                                                                 // 연결된 모든 클라이언트의 아이디를 비교함.
                {
                    s.Send(Encoding.Unicode.GetBytes("이미 같은 아이디가 존재합니다."));                             // 에러 처리
                }
                else
                {
                    clientNum++;                                                                                     // 연결되어 있는 총 client 숫자.
                    Console.WriteLine("[접속{0}]ID:{1},{2} - anonymous",
                    clientNum, userID, s.RemoteEndPoint);
                    connectClients.Add(userID);
                    AnonymousClients.Add(userID, s);

                    s.Send(Encoding.Unicode.GetBytes(m));                                                            // 정상이면 받은 그대로 보냄.
                }
            }
            else if (mode.Equals("general"))
            {
                userID = tokens[1];
                if (connectClients.Contains(userID))                                                                // 연결된 모든 클라이언트의 아이디를 비교함.
                {
                    s.Send(Encoding.Unicode.GetBytes("이미 같은 아이디가 존재합니다."));                            // 에러 처리
                }
                else
                {
                    clientNum++;                                                                                    // 연결되어 있는 총 client 숫자.
                    Console.WriteLine("[접속{0}]ID:{1},{2} - general",
                    clientNum, userID, s.RemoteEndPoint);
                    connectClients.Add(userID);
                    GeneralClients.Add(userID, s);
                    generalBroadcast(s, $"newConnect,{userID}");
                    Console.WriteLine($"{userID} - in GroupChatting");

                    s.Send(Encoding.Unicode.GetBytes(m));                                                           // 정상이면 받은 그대로 보냄.
                }
            }
            else if (mode.Equals("groupMessage"))
            {
                string msg = tokens[1] + "\t:" + tokens[2];
                //
                generalBroadcast(s, $"groupMessage,{msg}");
                Console.WriteLine($"[전체]{tokens[1]}:{tokens[2]}");
            }
            else if (mode.Equals("showList"))
            {
                userID = tokens[1];

                sendConnectList(s, userID);
                Console.WriteLine($"[{userID}] <- GroupChattingList");
            }
            else if (mode.Equals("sendTo"))
            {
                userID = tokens[1];
                string msg = tokens[2];
                string[] userIDs = msg.Split(':');

                sendTo(userID, s, userIDs);
                Console.WriteLine($"[{userID}] - {msg}");
            }
            else if (mode.Equals("randomChatting"))
            {
                userID = tokens[1];
            }
            else if (mode.Equals("connectOpenChatting"))
            {
                anonymousNum++;
                userID = tokens[1] + "=익명" + anonymousNum;
                OpenChattingClients.Add(userID, s);
                s.Send(Encoding.Unicode.GetBytes(userID));
                Console.WriteLine($"ID : {tokens[1]} - connectOpenChatting");
            }
            else if (mode.Equals("openChatting"))
            {
                userID = tokens[1];
                openChatting(s, userID, tokens[2]);
                string[] userIDs = userID.Split('=');
                Console.WriteLine($"(OpenChattingRoom)[{userIDs[1]}]:{tokens[2]}");
            }
            else if (mode.Equals("waitStateTrue"))
            {
                userID = tokens[1];
                disconnectChatting(s, userID);
                setWaitState(s, userID);
            }
            else if (mode.Equals("waitState"))
            {
                // 채팅 중이지 않은 상태에서 wait요청 했을 때,
                userID = tokens[1];
                setWaitState(s, userID);
            }
            else if (mode.Equals("startRandomChatTrue"))
            {
                userID = tokens[1];
                disconnectChatting(s, userID);
                startChatting(s, userID);
            }
            else if (mode.Equals("startRandomChat"))
            {
                // 채팅 중이지 않은 상태에서 start요청을 했을 때,
                userID = tokens[1];
                startChatting(s, userID);
            }
            else if (mode.Equals("randomMsg"))
            {
                userID = tokens[1];
                if (chattingClients.TryGetValue(userID, out Socket[] sArray))
                {
                    sArray[1].Send(Encoding.Unicode.GetBytes($"randomMsg_{tokens[2]}"));
                } else
                {
                    sArray[0].Send(Encoding.Unicode.GetBytes("보낼 상대가 없습니다만."));
                }
            }
        }

        void sendTo(string id, Socket s, string[] msg)
        {
            string silenceMsg;
            byte[] bytes;
            for (int i = 0; i < msg.Length - 1; i++)
            { 

                if (generalClients.ContainsKey(msg[i]))
                {
                    generalClients.TryGetValue(msg[i], out Socket client);
                    silenceMsg = "silence," + id + "," + msg[msg.Length - 1];
                    bytes = Encoding.Unicode.GetBytes(silenceMsg);
                    client.Send(bytes);
                } else
                {
                    s.Send(Encoding.Unicode.GetBytes($"failSilence,{msg[i]}"));
                }
                /*
                foreach (KeyValuePair<string, Socket> clients in generalClients.ToArray())
                {
                    try
                    {
                        //5-2 send
                        //
                        if (clients.Key == msg[i])
                        {
                            silenceMsg = "silence," + id + "," + msg[msg.Length - 1];
                            bytes = Encoding.Unicode.GetBytes(silenceMsg);
                            clients.Value.Send(bytes);
                        }
                    }
                    catch (Exception)
                    {
                        Disconnected(clients.Value);
                    }
                }
                */
            }
        }
        void generalBroadcast(Socket s, string msg) // 5-2ㅡ모든 클라이언트에게 Send
        {
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            //
            foreach (KeyValuePair<string, Socket> clients in generalClients.ToArray())
            {
                try
                {
                    //5-2 send
                    //
                    if (s != clients.Value)
                        clients.Value.Send(bytes);
                }
                catch (Exception)
                {
                    Disconnected(clients.Value);
                }
            }
        }
        void sendConnectList(Socket s, string userID) // 5-2ㅡ모든 클라이언트에게 Send
        {
            string msg = "list,";
            foreach (KeyValuePair<string, Socket> clients in generalClients.ToArray())
            {
                //5-2 send
                //
                if (clients.Key != userID)
                    msg += clients.Key + ",";
            }
            byte[] bytes = Encoding.Unicode.GetBytes(msg);
            s.Send(bytes);
            Console.WriteLine("list:" + msg);
        }

        void openChatting(Socket s, string userID, string msg)
        {
            string[] userIDs = userID.Split('=');
            byte[] bytes = Encoding.Unicode.GetBytes(userIDs[1] + "\t:" + msg);
            foreach (KeyValuePair<string, Socket> clients in openChattingClients.ToArray())
            {
                // string[] anonymousIDs = clients.Key.Split('=');
                // if (anonymousIDs[0] != userIDs[0])
                // {
                //     clients.Value.Send(bytes);
                // }
                if (s != clients.Value)
                    clients.Value.Send(bytes);
            }
        }

        void setWaitState(Socket s, string userID)
        {
            if (!waitState.Contains(userID))
            {
                waitState.Add(userID);
            }
            s.Send(Encoding.Unicode.GetBytes("waitState_"));
            Console.WriteLine($"# waiting - {waitState.Count} # {userID} -> waitstate");
        }

        void disconnectChatting(Socket s, string userID)
        {
            if (chattingClients.TryGetValue(userID, out Socket[] sArray))
            {
                foreach (KeyValuePair<string, Socket[]> chatClients in chattingClients)
                {
                    if (chatClients.Value[0] == sArray[1])
                    {
                        chatClients.Value[0].Send(Encoding.Unicode.GetBytes("disconnectChat_"));
                        chattingClients.Remove(chatClients.Key);
                        Console.WriteLine(userID + "가 " + chatClients.Key + "와의 연결을 disconnect");
                    }
                }
                s.Send(Encoding.Unicode.GetBytes("disconnectChat_"));
                chattingClients.Remove(userID);
            }
        }

        void startChatting(Socket s, string userID)
        {
            if (waitState.Contains(userID))
            {
                // wait중인 자신을 지움.
                waitState.Remove(userID);
            }

            int waitLength = waitState.Count;

            if (waitLength == 0)
            {
                Console.WriteLine($"{userID}이외에 존재하는 wait이 없어 start 불가");
                s.Send(Encoding.Unicode.GetBytes("noClients_"));
            }
            else if (waitLength > 0)
            {
                int n = rand.Next(waitLength); // 연결 가능한 client 중에 하나 랜덤으로 가져오기
                string anotherClient = (string)waitState[n]; // 랜덤으로 정해진 인덱스에 존재하는 클라이언트 가져오기
                waitState.Remove(anotherClient);   // 가져온 클라이언트를 통해 wait안에 있는 해당 클라이언트 지우기.
                if (anonymousClients.TryGetValue(anotherClient, out Socket anotherSocket))
                {
                    ChattingClients.Add(userID, new Socket[] { s, anotherSocket });
                    ChattingClients.Add(anotherClient, new Socket[] { anotherSocket, s });

                    s.Send(Encoding.Unicode.GetBytes("chattingON_"));
                    anotherSocket.Send(Encoding.Unicode.GetBytes("chattingON_"));
                }
                Console.WriteLine($"# waiting - {waitLength} # {userID}-{anotherClient} -> privateChattingRoom");
            }
            else
            {
                Console.WriteLine("알 수 없는 오류 처리");
                s.Send(Encoding.Unicode.GetBytes("뭐 어떻게 보내신 거에요 ?"));
            }
        }
    }
}
