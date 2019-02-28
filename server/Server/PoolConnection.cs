// The MIT License (MIT)

// Copyright (c) 2018-2019 - the webminerpool developer

// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TinyJson;

using JsonData = System.Collections.Generic.Dictionary<string, object>;

using Fleck;

namespace Server
{

    public class PoolConnection
    {
        public TcpClient TcpClient;

        public byte[] ReceiveBuffer;

        public string Login;
        public string Password;
        public int Port;
        public string Url;
        public bool Closed;

        public string PoolId;
        public string Credentials;

        public long Hashes = 0;

        public Client LastSender;
        public JsonData LastJob;
        public DateTime LastInteraction = DateTime.Now;
        public CcHashset<string> LastSolved;

        public string DefaultAlgorithm = "cn";

        public CcHashset<Client> WebClients = new CcHashset<Client>();

        public void Send(Client client, string msg)
        {
            try
            {
                Byte[] bytesSent = Encoding.ASCII.GetBytes(msg);
                TcpClient.GetStream().BeginWrite(bytesSent, 0, bytesSent.Length, SendCallback, null);
                this.LastSender = client;
            }
            catch { }
        }

        private void SendCallback(IAsyncResult result)
        {
            if (!TcpClient.Connected) return;

            try
            {
                NetworkStream networkStream = TcpClient.GetStream();
                networkStream.EndWrite(result);
            }
            catch { }
        }

    }
    public class PoolConnectionFactory
    {
        public delegate void ReceiveJobDelegate(Client client, JsonData json, CcHashset<string> hashset);
        public delegate void ReceiveErrorDelegate(Client client, JsonData json);
        public delegate void DisconnectedDelegate(Client client, string reason);

        private static ReceiveErrorDelegate ReceiveError;
        private static ReceiveJobDelegate ReceiveJob;
        private static DisconnectedDelegate Disconnect;

        public static CcDictionary<string, PoolConnection> Connections = new CcDictionary<string, PoolConnection>();

        private static bool VerifyJob(JsonData data)
        {
            if (data == null) return false;

            if (!data.ContainsKey("job_id")) return false;
            if (!data.ContainsKey("blob")) return false;
            if (!data.ContainsKey("target")) return false;

            string blob = data["blob"].GetString();
            string target = data["target"].GetString();

            if (blob.Length < 152 || blob.Length > 220) return false;
            if (target.Length != 8) return false;

            if (!Regex.IsMatch(blob, MainClass.RegexIsHex)) return false;
            if (!Regex.IsMatch(target, MainClass.RegexIsHex)) return false;

            return true;
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            PoolConnection mypc = result.AsyncState as PoolConnection;
            TcpClient client = mypc.TcpClient;

            if (mypc.Closed || !client.Connected) return;

            NetworkStream networkStream;

            try { networkStream = client.GetStream(); } catch { return; }

            int bytesread = 0;

            try { bytesread = networkStream.EndRead(result); } catch { return; }

            string json = string.Empty;

            try
            {
                if (bytesread == 0) // disconnected
                {
                    // slow that down a bit to avoid negative feedback loop

                    Task.Run(async delegate
                    {
                        await Task.Delay(TimeSpan.FromSeconds(4));

                        List<Client> cllist = new List<Client>(mypc.WebClients.Values);
                        foreach (Client ev in cllist) Disconnect(ev, "lost pool connection.");
                    });

                    return;
                }

                json = Encoding.ASCII.GetString(mypc.ReceiveBuffer, 0, bytesread);

                networkStream.BeginRead(mypc.ReceiveBuffer, 0, mypc.ReceiveBuffer.Length, new AsyncCallback(ReceiveCallback), mypc);
            }
            catch { return; }

            if (bytesread == 0 || string.IsNullOrEmpty(json)) return; //?!

            var msg = json.FromJson<JsonData>();
            if (msg == null) return;

            if (string.IsNullOrEmpty(mypc.PoolId))
            {

                // this "protocol" is strange
                if (!msg.ContainsKey("result"))
                {

                    string additionalInfo = "none";

                    // try to get the error
                    if (msg.ContainsKey("error"))
                    {
                        msg = msg["error"] as JsonData;

                        if (msg != null && msg.ContainsKey("message"))
                            additionalInfo = msg["message"].GetString();
                    }

                    List<Client> cllist = new List<Client>(mypc.WebClients.Values);
                    foreach (Client ev in cllist)
                        Disconnect(ev, "can not connect. additional information: " + additionalInfo);

                    return;
                }

                msg = msg["result"] as JsonData;

                if (msg == null)
                    return;
                if (!msg.ContainsKey("id"))
                    return;
                if (!msg.ContainsKey("job"))
                    return;

                mypc.PoolId = msg["id"].GetString();

                var lastjob = msg["job"] as JsonData;

                if (!VerifyJob(lastjob))
                {
                    CConsole.ColorWarning(() =>
                    Console.WriteLine("Failed to verify job: {0}", json));
                    return;
                }

                // extended stratum 
                if (!lastjob.ContainsKey("algo")) lastjob.Add("algo", mypc.DefaultAlgorithm);
                if (!AlgorithmHelper.NormalizeAlgorithmAndVariant(lastjob))
                {
                    CConsole.ColorWarning(() => Console.WriteLine("Do not understand algorithm/variant!"));
                    return;
                }

                mypc.LastJob = lastjob;
                mypc.LastInteraction = DateTime.Now;

                mypc.LastSolved = new CcHashset<string>();

                List<Client> cllist2 = new List<Client>(mypc.WebClients.Values);
                foreach (Client ev in cllist2)
                {
                    ReceiveJob(ev, mypc.LastJob, mypc.LastSolved);
                }
            }
            else if (msg.ContainsKey("method") && msg["method"].GetString() == "job")
            {
                if (!msg.ContainsKey("params"))
                    return;

                var lastjob = msg["params"] as JsonData;

                if (!VerifyJob(lastjob))
                {
                    CConsole.ColorWarning(() =>
                    Console.WriteLine("Failed to verify job: {0}", json));
                    return;
                }

                // extended stratum 
                if (!lastjob.ContainsKey("algo")) lastjob.Add("algo", mypc.DefaultAlgorithm);
                if (!AlgorithmHelper.NormalizeAlgorithmAndVariant(lastjob))
                {
                    CConsole.ColorWarning(() => Console.WriteLine("Do not understand algorithm/variant!"));
                    return;
                }

                mypc.LastJob = lastjob;
                mypc.LastInteraction = DateTime.Now;
                mypc.LastSolved = new CcHashset<string>();

                List<Client> cllist2 = new List<Client>(mypc.WebClients.Values);

                Console.WriteLine("Sending job to {0} client(s)!", cllist2.Count);

                foreach (Client ev in cllist2)
                {
                    ReceiveJob(ev, mypc.LastJob, mypc.LastSolved);
                }
            }
            else
            {
                if (msg.ContainsKey("error"))
                {
                    // who knows?
                    ReceiveError(mypc.LastSender, msg);
                }
                else
                {
                    CConsole.ColorWarning(() =>
                    Console.WriteLine("Pool is sending nonsense."));
                }
            }
        }

        private static void ConnectCallback(IAsyncResult result)
        {
            PoolConnection mypc = result.AsyncState as PoolConnection;
            TcpClient client = mypc.TcpClient;

            if (!mypc.Closed && client.Connected)
            {
                try
                {
                    NetworkStream networkStream = client.GetStream();
                    mypc.ReceiveBuffer = new byte[client.ReceiveBufferSize];

                    networkStream.BeginRead(mypc.ReceiveBuffer, 0, mypc.ReceiveBuffer.Length, new AsyncCallback(ReceiveCallback), mypc);

                    // keep things stupid and simple 
                    // https://github.com/xmrig/xmrig-proxy/blob/dev/doc/STRATUM_EXT.md#mining-algorithm-negotiation

                    string msg0 = "{\"method\":\"login\",\"params\":{\"login\":\"";
                    string msg1 = "\",\"pass\":\"";
                    string msg2 = "\",\"agent\":\"webminerpool.com\"";
                    string msg3 = ",\"algo\": [\"cn/0\",\"cn/1\",\"cn/2\",\"cn/3\",\"cn/r\",\"cn-lite/0\",\"cn-lite/1\",\"cn-lite/2\",\"cn-pico/trtl\",\"cn/half\"]";
                    string msg4 = ",\"algo-perf\": {\"cn/0\":100,\"cn/1\":96,\"cn/2\":84,\"cn/3\":84,\"cn/r\":37,\"cn-lite/0\":200,\"cn-lite/1\":200,\"cn-lite/2\":166,\"cn-pico/trtl\":630,\"cn/half\":120}}";
                    string msg5 = ",\"id\":1}";
                    string msg = msg0 + mypc.Login + msg1 + mypc.Password + msg2 + msg3 + msg4 + msg5 + "\n";

                    mypc.Send(mypc.LastSender, msg);
                }
                catch { return; }
            }
            else
            {
                // slow that down a bit
                Task.Run(async delegate
                {
                    await Task.Delay(TimeSpan.FromSeconds(4));

                    List<Client> cllist = new List<Client>(mypc.WebClients.Values);
                    foreach (Client ev in cllist)
                        Disconnect(ev, "can not connect to pool.");
                });
            }
        }

        public static void Close(Client client)
        {
            PoolConnection connection = client.PoolConnection;

            connection.WebClients.TryRemove(client);

            if (connection.WebClients.Count == 0)
            {
                connection.Closed = true;

                try
                {
                    var networkStream = connection.TcpClient.GetStream();
                    networkStream.EndRead(null);
                }
                catch { }

                try { connection.TcpClient.Close(); } catch { }
                try { connection.TcpClient.Client.Close(); } catch { }
                try { connection.ReceiveBuffer = null; } catch { }

                Connections.TryRemove(connection.Credentials);

                Console.WriteLine("{0}: closed a pool connection.", client.WebSocket.ConnectionInfo.Id);
            }
        }

        public static void RegisterCallbacks(ReceiveJobDelegate receiveJob, ReceiveErrorDelegate receiveError, DisconnectedDelegate disconnect)
        {
            PoolConnectionFactory.ReceiveJob = receiveJob;
            PoolConnectionFactory.ReceiveError = receiveError;
            PoolConnectionFactory.Disconnect = disconnect;
        }

        public static void CheckPoolConnection(PoolConnection connection)
        {
            if (connection.Closed) return;

            if ((DateTime.Now - connection.LastInteraction).TotalMinutes < 10)
                return;

            CConsole.ColorWarning(() => Console.WriteLine("Initiating reconnect! {0}:{1}", connection.Url, connection.Login));

            try
            {
                var networkStream = connection.TcpClient.GetStream();
                networkStream.EndRead(null);
            }
            catch { }

            try { connection.TcpClient.Close(); } catch { }
            try { connection.TcpClient.Client.Close(); } catch { }

            connection.ReceiveBuffer = null;
            connection.LastInteraction = DateTime.Now;
            connection.PoolId = "";
            connection.LastJob = null;
            connection.TcpClient = new TcpClient();

            Fleck.SocketExtensions.SetKeepAlive(connection.TcpClient.Client, 60000, 1000);
            connection.TcpClient.Client.ReceiveBufferSize = 4096 * 2;

            try { connection.TcpClient.BeginConnect(connection.Url, connection.Port, new AsyncCallback(ConnectCallback), connection); } catch { }
        }

        public static PoolConnection CreatePoolConnection(Client client, string url, int port, string login, string password)
        {
            string credential = url + port.ToString() + login + password;

            PoolConnection lpc, mypc = null;

            int batchCounter = 0;

            while (Connections.TryGetValue(credential + batchCounter.ToString(), out lpc))
            {
                if (lpc.WebClients.Count > MainClass.BatchSize) batchCounter++;
                else { mypc = lpc; break; }
            }

            credential += batchCounter.ToString();

            if (mypc == null)
            {
                CConsole.ColorInfo(() =>
                {
                    Console.WriteLine("{0}: initiated new pool connection", client.WebSocket.ConnectionInfo.Id);
                    Console.WriteLine("{0} {1} {2}", login, password, url);
                });

                mypc = new PoolConnection();
                mypc.Credentials = credential;
                mypc.LastSender = client;

                mypc.TcpClient = new TcpClient();

                Fleck.SocketExtensions.SetKeepAlive(mypc.TcpClient.Client, 60000, 1000);
                mypc.TcpClient.Client.ReceiveBufferSize = 4096 * 2;

                mypc.Login = login;
                mypc.Password = password;
                mypc.Port = port;
                mypc.Url = url;

                mypc.WebClients.TryAdd(client);

                Connections.TryAdd(credential, mypc);

                try { mypc.TcpClient.Client.BeginConnect(url, port, new AsyncCallback(ConnectCallback), mypc); } catch { }
            }
            else
            {
                Console.WriteLine("{0}: reusing pool connection", client.WebSocket.ConnectionInfo.Id);

                mypc.WebClients.TryAdd(client);

                if (mypc.LastJob != null) ReceiveJob(client, mypc.LastJob, mypc.LastSolved);
                else Console.WriteLine("{0} no job yet.", client.WebSocket.ConnectionInfo.Id);

            }

            client.PoolConnection = mypc;

            return mypc;
        }
    }
}