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
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using Fleck;
using TinyJson;

using JsonData = System.Collections.Generic.Dictionary<string, object>;

namespace Server
{

    public class Client
    {
        public PoolConnection PoolConnection;
        public IWebSocketConnection WebSocket;
        public string Pool, Login, Password;
        public bool GotHandshake;
        public bool GotPoolInfo;
        public DateTime Created;
        public DateTime LastPoolJobTime;
        public string LastTarget, UserId;
        public int NumChecked = 0;
        public double Fee = Donation.DonationLevel;
        public int Version = 1;
    }

    public class JobInfo
    {
        public string JobId;
        public string InnerId;
        public string Blob;
        public string Target;
        public int Variant;
        public int Height;
        public string Algo;
        public CcHashset<string> Solved;
        public bool OwnJob;
    }

    public class Job
    {
        public string Blob;
        public string Target;
        public int Variant;
        public int Height;
        public string JobId;
        public string Algo;
        public DateTime Age = DateTime.MinValue;
    }

    public struct Credentials
    {
        public string Pool;
        public string Login;
        public string Password;
    }

    class MainClass
    {

        [DllImport("libhash.so", CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr hash_cn(string hex, int algo, int variant, int height);

        [DllImport("libhash.so", CallingConvention = CallingConvention.StdCall)]
        static extern IntPtr hash_free(IntPtr ptr);

        public const string RegexIsHex = "^[a-fA-F0-9]+$";
        public const string RegexIsXMR = "[a-zA-Z|\\d]{95}";

        public const int JobCacheSize = (int)1e5;

        private static bool libHashAvailable = false;

        private static PoolList PoolList;

        // time to connect to a pool in seconds 
        private const int GraceConnectionTime = 16;
        // server logic every x seconds
        private const int HeartbeatRate = 10;
        // after that job-age we do not forward own jobs 
        private const int TimeOwnJobsAreOld = 600;
        // in seconds, pool is not sending new jobs 
        private const int PoolTimeout = 60 * 12;
        // for the statistics shown every heartbeat
        private const int SpeedAverageOverXHeartbeats = 10;
        // try not to kill ourselfs  
        private const int MaxHashChecksPerHeartbeat = 40;
        // so we can keep an eye on the memory 
        private const int ForceGCEveryXHeartbeat = 40;
        // mining with the same credentials (pool, login, password)
        // results in connections beeing "bundled" to a single connection
        // seen by the pool. that can result in large difficulties and
        // hashrate fluctuations. this parameter sets the number of clients
        // in one batch, e.g. for BatchSize = 100 and 1000 clients
        // there will be 10 pool connections.
        public const int BatchSize = 200;

        private static int Heartbeats = 0;
        private static int HashesCheckedThisHeartbeat = 0;

        private static long totalHashes = 0;
        private static long totalOwnHashes = 0;
        private static long exceptionCounter = 0;

        private static CcDictionary<Guid, Client> clients = new CcDictionary<Guid, Client>();
        private static CcDictionary<string, JobInfo> jobInfos = new CcDictionary<string, JobInfo>();
        private static CcDictionary<string, Credentials> loginids = new CcDictionary<string, Credentials>();
        private static CcDictionary<string, int> credentialSpamProtector = new CcDictionary<string, int>();

        private static CcHashset<Client> slaves = new CcHashset<Client>();

        private static CcQueue<string> jobQueue = new CcQueue<string>();

        private static Job ownJob = new Job();

        static Client ourself;

        private static UInt32 HexToUInt32(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            UInt32 result = BitConverter.ToUInt32(bytes, 0);
            return result;
        }

        private static bool CheckHashTarget(string target, string result)
        {
            string ourtarget = result.Substring(56, 8);
            return (HexToUInt32(ourtarget) < HexToUInt32(target));
        }

        private static bool CheckHash(JobInfo ji, string result, string nonce, bool fullcheck)
        {
            string ourtarget = result.Substring(56, 8);

            if (HexToUInt32(ourtarget) >= HexToUInt32(ji.Target))
                return false;

            if (libHashAvailable && fullcheck)
            {
                string parta = ji.Blob.Substring(0, 78);
                string partb = ji.Blob.Substring(86, ji.Blob.Length - 86);

                IntPtr pStr;

                if (ji.Algo == "cn") pStr = hash_cn(parta + nonce + partb, 0, ji.Variant, ji.Height);
                else if (ji.Algo == "cn-lite") pStr = hash_cn(parta + nonce + partb, 1, ji.Variant, ji.Height);
                else if (ji.Algo == "cn-pico") pStr = hash_cn(parta + nonce + partb, 2, ji.Variant, ji.Height);
                else if (ji.Algo == "cn-half") pStr = hash_cn(parta + nonce + partb, 3, ji.Variant, ji.Height);
                else if (ji.Algo == "cn-rwz") pStr = hash_cn(parta + nonce + partb, 4, ji.Variant, ji.Height);
                else return false;

                string ourresult = Marshal.PtrToStringAnsi(pStr);
                hash_free(pStr);

                if (ourresult != result) return false;
            }

            return true;
        }

        private static void PoolDisconnectCallback(Client client, string reason)
        {
            DisconnectClient(client, reason);
        }

        private static void PoolErrorCallback(Client client, JsonData msg)
        {
            if (msg["error"] == null)
            {
                string forward = "{\"identifier\":\"" + "hashsolved" + "\"}\n";
                client.WebSocket.Send(forward);
                Console.WriteLine("{0}: solved job", client.WebSocket.ConnectionInfo.Id);
            }
            else
            {
                if (client != ourself)
                {
                    string forward = "{\"identifier\":\"" + "error" +
                        "\",\"param\":\"" + "pool rejected hash" + "\"}\n";

                    client.WebSocket.Send(forward);
                    Console.WriteLine("{0}: got a pool rejection", client.WebSocket.ConnectionInfo.Id);
                }
            }
        }

        private static bool IsCompatible(string blob, string algo, int variant, int clientVersion)
        {
            return clientVersion > 7;
        }

        private static void PoolReceiveCallback(Client client, JsonData msg, CcHashset<string> hashset)
        {
            string jobId = Guid.NewGuid().ToString("N");

            client.LastPoolJobTime = DateTime.Now;

            JobInfo ji = new JobInfo
            {
                JobId = jobId,
                Blob = msg["blob"].GetString(),
                Target = msg["target"].GetString(),
                InnerId = msg["job_id"].GetString(),
                Algo = msg["algo"].GetString(),
                Solved = hashset,
                OwnJob = (client == ourself)
            };

            if (!int.TryParse(msg["variant"].GetString(), out ji.Variant)) { ji.Variant = -1; }
            if (!int.TryParse(msg["height"].GetString(), out ji.Height)) { ji.Height = 0; }

            jobInfos.TryAdd(jobId, ji);
            jobQueue.Enqueue(jobId);

            if (client == ourself)
            {
                ownJob.Blob = ji.Blob;
                ownJob.JobId = jobId;
                ownJob.Age = DateTime.Now;
                ownJob.Algo = ji.Algo;
                ownJob.Target = ji.Target;
                ownJob.Height = ji.Height;
                ownJob.Variant = ji.Variant;

                List<Client> slavelist = new List<Client>(slaves.Values);

                foreach (Client slave in slavelist)
                {
                    bool compatible = IsCompatible(ownJob.Blob, ownJob.Algo, ownJob.Variant, slave.Version);
                    if (!compatible) continue;

                    string newtarget, forward;

                    if (string.IsNullOrEmpty(slave.LastTarget))
                    {
                        newtarget = ownJob.Target;
                    }
                    else
                    {
                        uint diff1 = HexToUInt32(slave.LastTarget);
                        uint diff2 = HexToUInt32(ownJob.Target);
                        if (diff1 > diff2)
                            newtarget = slave.LastTarget;
                        else
                            newtarget = ownJob.Target;
                    }

                    forward = "{\"identifier\":\"" + "job" +
                        "\",\"job_id\":\"" + ownJob.JobId +
                        "\",\"algo\":\"" + ownJob.Algo.ToLower() +
                        "\",\"variant\":" + ownJob.Variant.ToString() +
                        ",\"height\":" + ownJob.Height.ToString() +
                        ",\"blob\":\"" + ownJob.Blob +
                        "\",\"target\":\"" + newtarget + "\"}\n";

                    slave.WebSocket.Send(forward);
                    Console.WriteLine("Sending job to slave {0}", slave.WebSocket.ConnectionInfo.Id);
                }
            }
            else
            {
                string forward = string.Empty;

                bool tookown = false;

                if (Random2.NextDouble() < client.Fee)
                {
                    if (((DateTime.Now - ownJob.Age).TotalSeconds < TimeOwnJobsAreOld))
                    {
                        bool compatible = IsCompatible(ownJob.Blob, ownJob.Algo, ownJob.Variant, client.Version);

                        if (compatible)
                        {
                            // do not send ownjob.Target, but the last difficulty
                            string newtarget = string.Empty;

                            if (string.IsNullOrEmpty(client.LastTarget))
                            {
                                newtarget = ownJob.Target;
                            }
                            else
                            {
                                uint diff1 = HexToUInt32(client.LastTarget);
                                uint diff2 = HexToUInt32(ownJob.Target);
                                if (diff1 > diff2)
                                    newtarget = client.LastTarget;
                                else
                                    newtarget = ownJob.Target;
                            }

                            forward = "{\"identifier\":\"" + "job" +
                                "\",\"job_id\":\"" + ownJob.JobId +
                                "\",\"algo\":\"" + ownJob.Algo.ToLower() +
                                "\",\"variant\":" + ownJob.Variant.ToString() +
                                ",\"height\":" + ownJob.Height.ToString() +
                                ",\"blob\":\"" + ownJob.Blob +
                                "\",\"target\":\"" + newtarget + "\"}\n";

                            tookown = true;
                        }
                    }
                }

                if (!tookown)
                {
                    bool compatible = IsCompatible(ji.Blob, ji.Algo, ji.Variant, client.Version);
                    if (!compatible) return;

                    forward = "{\"identifier\":\"" + "job" +
                        "\",\"job_id\":\"" + jobId +
                        "\",\"algo\":\"" + ji.Algo +
                        "\",\"variant\":" + ji.Variant.ToString() +
                        ",\"height\":" + ji.Height.ToString() +
                        ",\"blob\":\"" + ji.Blob +
                        "\",\"target\":\"" + ji.Target + "\"}\n";

                    client.LastTarget = msg["target"].GetString();
                }

                if (tookown)
                {
                    if (!slaves.Contains(client)) slaves.TryAdd(client);
                    Console.WriteLine("Send own job!");
                }
                else
                {
                    slaves.TryRemove(client);
                }

                client.WebSocket.Send(forward);
                Console.WriteLine("{0}: got job from pool ({1}, v{2})", client.WebSocket.ConnectionInfo.Id, ji.Algo, ji.Variant);
            }
        }

        public static void RemoveClient(Guid guid)
        {
            Client client;

            if (!clients.TryRemove(guid, out client)) return;

            slaves.TryRemove(client);

            try
            {
                var wsoc = client.WebSocket as WebSocketConnection;
                if (wsoc != null) wsoc.CloseSocket();
            }
            catch { }

            try { client.WebSocket.Close(); } catch { }

            if (client.PoolConnection != null)
                PoolConnectionFactory.Close(client);
        }

        public static void DisconnectClient(Client client, string reason)
        {
            if (client.WebSocket.IsAvailable)
            {
                string msg = "{\"identifier\":\"" + "error" +
                    "\",\"param\":\"" + reason + "\"}\n";

                System.Threading.Tasks.Task t = client.WebSocket.Send(msg);

                t.ContinueWith((prevTask) =>
                {
                    prevTask.Wait();
                    RemoveClient(client.WebSocket.ConnectionInfo.Id);
                });

            }
            else
            {
                RemoveClient(client.WebSocket.ConnectionInfo.Id);
            }
        }

        public static void DisconnectClient(Guid guid, string reason)
        {
            Client client = clients[guid];
            DisconnectClient(client, reason);
        }

        private static void CreateOurself()
        {
            ourself = new Client();

            ourself.Login = Donation.Address;
            ourself.Pool = Donation.PoolUrl;
            ourself.Created = ourself.LastPoolJobTime = DateTime.Now;
            ourself.Password = Donation.PoolPwd;
            ourself.WebSocket = new EmptyWebsocket();

            clients.TryAdd(Guid.Empty, ourself);

            ourself.PoolConnection = PoolConnectionFactory.CreatePoolConnection(ourself,
                Donation.PoolUrl, Donation.PoolPort, Donation.Address, Donation.PoolPwd);

            ourself.PoolConnection.DefaultAlgorithm = "cn";
        }

        private static bool CheckLibHash(string input, string expected, int algo, int variant, int height, out Exception ex)
        {
            string hashedResult = string.Empty;

            try
            {
                IntPtr pStr = hash_cn(input, algo, variant, height);
                hashedResult = Marshal.PtrToStringAnsi(pStr);
                hash_free(pStr);
            }
            catch (Exception e)
            {
                ex = e;
                return false;
            }

            if (hashedResult != expected)
            {
                ex = new Exception("Hash function returned wrong hash");
                return false;
            }

            ex = null;
            return true;
        }

        private static void ExcessiveHashTest()
        {
            Parallel.For(0, 100, (i) =>
            {
                string testStr = new string('1', 151) + '3';

                IntPtr ptr = hash_cn(testStr, 1, 0, 0);
                string str = Marshal.PtrToStringAnsi(ptr);
                hash_free(ptr);

                Console.WriteLine(i.ToString() + " " + str);
            });
        }

        public static void Main(string[] args)
        {
            CConsole.ColorInfo(() =>
            {
#if (DEBUG)
                Console.WriteLine("[{0}] webminerpool server started - DEBUG MODE", DateTime.Now);
#else
                Console.WriteLine ("[{0}] webminerpool server started", DateTime.Now);
#endif

                Console.WriteLine();
            });

            try
            {
                PoolList = PoolList.LoadFromFile("pools.json");
            }
            catch (Exception ex)
            {
                CConsole.ColorAlert(() => Console.WriteLine("Could not load pool list from pools.json: {0}", ex.Message));
                return;
            }

            CConsole.ColorInfo(() => Console.WriteLine("Loaded {0} pools from pools.json.", PoolList.Count));

            Exception exception = null;
            libHashAvailable = true;

            libHashAvailable = libHashAvailable && CheckLibHash("6465206f6d6e69627573206475626974616e64756d",
                                 "2f8e3df40bd11f9ac90c743ca8e32bb391da4fb98612aa3b6cdc639ee00b31f5",
                                  0, 0, 0, out exception);

            libHashAvailable = libHashAvailable && CheckLibHash("38274c97c45a172cfc97679870422e3a1ab0784960c60514d816271415c306ee3a3ed1a77e31f6a885c3cb",
                                 "ed082e49dbd5bbe34a3726a0d1dad981146062b39d36d62c71eb1ed8ab49459b",
                                  0, 1, 0, out exception);

            libHashAvailable = libHashAvailable && CheckLibHash("5468697320697320612074657374205468697320697320612074657374205468697320697320612074657374",
                                 "353fdc068fd47b03c04b9431e005e00b68c2168a3cc7335c8b9b308156591a4f",
                                  0, 2, 0, out exception);

            libHashAvailable = libHashAvailable && CheckLibHash("5468697320697320612074657374205468697320697320612074657374205468697320697320612074657374",
                                 "f759588ad57e758467295443a9bd71490abff8e9dad1b95b6bf2f5d0d78387bc",
                                  0, 4, 1806260, out exception);

            libHashAvailable = libHashAvailable && CheckLibHash("5468697320697320612074657374205468697320697320612074657374205468697320697320612074657374",
                                 "32f736ec1d2f3fc54c49beb8a0476cbfdd14c351b9c6d72c6f9ffcb5875be6b3",
                                  4, 2, 0, out exception);


            if (!libHashAvailable) CConsole.ColorWarning(() =>
            {
                Console.WriteLine("libhash.so is not available. Checking user submitted hashes disabled:");
                Console.WriteLine(" -> {0}", new StringReader(exception.ToString()).ReadLine());
            });

            PoolConnectionFactory.RegisterCallbacks(PoolReceiveCallback, PoolErrorCallback, PoolDisconnectCallback);

            string loginsFilename = "logins.json";

            if (File.Exists(loginsFilename))
            {
                try
                {
                    string json = File.ReadAllText(loginsFilename);
                    JsonData data = json.FromJson<JsonData>();

                    foreach(string loginID in data.Keys)
                    {
                        JsonData jinfo = data[loginID] as JsonData;

                        Credentials cred = new Credentials
                        {
                            Pool = jinfo["pool"].GetString(),
                            Login = jinfo["login"].GetString(),
                            Password = jinfo["password"].GetString()
                        };

                        loginids.TryAdd(loginID, cred);
                    }
                }
                catch (Exception ex)
                {
                    CConsole.ColorAlert(() => Console.WriteLine("Error while reading logins: {0}", ex));
                }
            }

            X509Certificate2 cert = null;

            try { cert = new X509Certificate2("certificate.pfx", "miner"); } catch (Exception e) { exception = e; cert = null; }

            bool certAvailable = (cert != null);

            if (!certAvailable) CConsole.ColorWarning(() =>
            {
                Console.WriteLine("SSL certificate could not be loaded. Secure connection disabled.");
                Console.WriteLine(" -> {0}", new StringReader(exception.ToString()).ReadLine());
            });

            string localAddr = (certAvailable ? "wss://" : "ws://") + "0.0.0.0:8181";
            WebSocketServer server = new WebSocketServer(localAddr);
            server.Certificate = cert;

            FleckLog.LogAction = (level, message, ex) =>
            {
                switch (level)
                {
                    case LogLevel.Debug:
#if (DEBUG)
                        Console.WriteLine("FLECK (Debug): " + message);
#endif
                        break;
                    case LogLevel.Error:
                        if (ex != null && !string.IsNullOrEmpty(ex.Message))
                        {

                            CConsole.ColorAlert(() => Console.WriteLine("FLECK: " + message + " " + ex.Message));

                            exceptionCounter++;
                            if ((exceptionCounter % 200) == 0)
                            {
                                Helper.WriteTextAsyncWrapper("fleck_error.txt", ex.ToString());
                            }

                        }
                        else Console.WriteLine("FLECK: " + message);
                        break;
                    case LogLevel.Warn:
                        if (ex != null && !string.IsNullOrEmpty(ex.Message))
                        {
                            Console.WriteLine("FLECK: " + message + " " + ex.Message);

                            exceptionCounter++;
                            if ((exceptionCounter % 200) == 0)
                            {
                                Helper.WriteTextAsyncWrapper("fleck_warn.txt", ex.ToString());
                            }
                        }
                        else Console.WriteLine("FLECK: " + message);
                        break;
                    default:
                        Console.WriteLine("FLECK: " + message);
                        break;
                }
            };

            server.RestartAfterListenError = true;
            server.ListenerSocket.NoDelay = false;

            server.Start(socket =>
            {
                socket.OnOpen = () =>
                {
                    string ipadr = string.Empty;
                    try { ipadr = socket.ConnectionInfo.ClientIpAddress; } catch { }

                    Client client = new Client();
                    client.WebSocket = socket;
                    client.Created = client.LastPoolJobTime = DateTime.Now;

                    Guid guid = socket.ConnectionInfo.Id;
                    clients.TryAdd(guid, client);

                    Console.WriteLine("{0}: connected with ip {1}", guid, ipadr);
                };
                socket.OnClose = () =>
                {
                    Guid guid = socket.ConnectionInfo.Id;
                    RemoveClient(socket.ConnectionInfo.Id);

                    Console.WriteLine(guid + ": closed");
                };
                socket.OnError = error =>
                {
                    Guid guid = socket.ConnectionInfo.Id;
                    RemoveClient(socket.ConnectionInfo.Id);

                    Console.WriteLine(guid + ": unexpected close");
                };
                socket.OnMessage = message =>
                {
                    string ipadr = string.Empty;
                    try { ipadr = socket.ConnectionInfo.ClientIpAddress; } catch { }

                    Guid guid = socket.ConnectionInfo.Id;

                    if (message.Length > 3000)
                    {
                        RemoveClient(guid); // that can't be valid, do not even try to parse
                    }

                    JsonData msg = message.FromJson<JsonData>();
                    if (msg == null || !msg.ContainsKey("identifier")) return;

                    Client client = null;

                    // in very rare occasions, we get interference with onopen()
                    // due to async code. wait a second and retry.
                    for (int tries = 0; tries < 4; tries++)
                    {
                        if (clients.TryGetValue(guid, out client)) break;
                        Task.Run(async delegate { await Task.Delay(TimeSpan.FromSeconds(1)); }).Wait();
                    }

                    if (client == null)
                    {
                        // famous comment: this should not happen
                        RemoveClient(guid);
                        return;
                    }

                    string identifier = (string)msg["identifier"];

                    if (identifier == "handshake")
                    {
                        if (client.GotHandshake)
                        {
                            // no merci for malformed data.
                            DisconnectClient(client, "Handshake already performed.");
                            return;
                        }

                        client.GotHandshake = true;

                        if (msg.ContainsKey("version"))
                        {
                            int.TryParse(msg["version"].GetString(), out client.Version);
                        }

                        if (client.Version < 7)
                        {
                            DisconnectClient(client, "Client version too old.");
                            return;
                        }

                        if (client.Version < 8)
                        {
                            CConsole.ColorWarning(() => Console.WriteLine("Warning: Outdated client connected. Make sure to update the clients"));
                        }

                        if (msg.ContainsKey("loginid"))
                        {
                            string loginid = msg["loginid"].GetString();

                            if (!loginids.TryGetValue(loginid, out Credentials crdts))
                            {
                                Console.WriteLine("Unregistered LoginId! {0}", loginid);
                                DisconnectClient(client, "Loginid not registered!");
                                return;
                            }

                            client.Login = crdts.Login;
                            client.Password = crdts.Password;
                            client.Pool = crdts.Pool;

                        }
                        else if (msg.ContainsKey("login") && msg.ContainsKey("password") && msg.ContainsKey("pool"))
                        {
                            client.Login = msg["login"].GetString();
                            client.Password = msg["password"].GetString();
                            client.Pool = msg["pool"].GetString();
                        }
                        else
                        {
                            // no merci for malformed data.
                            Console.WriteLine("Malformed handshake");
                            DisconnectClient(client, "Login, password and pool have to be specified.");
                            return;
                        }

                        client.UserId = string.Empty;

                        if (msg.ContainsKey("userid"))
                        {
                            string uid = msg["userid"].GetString();

                            if (uid.Length > 200) { RemoveClient(socket.ConnectionInfo.Id); return; }
                            client.UserId = uid;
                        }

                        Console.WriteLine("{0}: handshake - {1}, {2}", guid, client.Pool,
                            (client.Login.Length > 8 ? client.Login.Substring(0, 8) + "..." : client.Login));

                        if (!string.IsNullOrEmpty(ipadr)) Firewall.Update(ipadr, Firewall.UpdateEntry.Handshake);

                        PoolInfo pi;

                        if (!PoolList.TryGetPool(client.Pool, out pi))
                        {
                            // we dont have that pool?
                            DisconnectClient(client, "pool not known");
                            return;
                        }

                        // if pools have some default password
                        if (client.Password == "") client.Password = pi.EmptyPassword;

                        client.PoolConnection = PoolConnectionFactory.CreatePoolConnection(
                            client, pi.Url, pi.Port, client.Login, client.Password);

                        client.PoolConnection.DefaultAlgorithm = pi.DefaultAlgorithm;
                    }
                    else if (identifier == "solved")
                    {
                        if (!client.GotHandshake)
                        {
                            RemoveClient(socket.ConnectionInfo.Id);
                            return;
                        }

                        Console.WriteLine("{0}: reports solved hash", guid);

                        new Task(() =>
                        {

                            if (!msg.ContainsKey("job_id") ||
                                !msg.ContainsKey("nonce") ||
                                !msg.ContainsKey("result"))
                            {
                                // no merci for malformed data.
                                RemoveClient(guid);
                                return;
                            }

                            string jobid = msg["job_id"].GetString();

                            if (!jobInfos.TryGetValue(jobid, out JobInfo ji))
                            {
                                // this job id is not known to us
                                Console.WriteLine("Job unknown!");
                                return;
                            }

                            string reportedNonce = msg["nonce"].GetString();
                            string reportedResult = msg["result"].GetString();

                            if (ji.Solved.Contains(reportedNonce.ToLower()))
                            {
                                Console.WriteLine("Nonce collision!");
                                return;
                            }

                            if (reportedNonce.Length != 8 || (!Regex.IsMatch(reportedNonce, RegexIsHex)))
                            {
                                DisconnectClient(client, "nonce malformed");
                                return;
                            }

                            if (reportedResult.Length != 64 || (!Regex.IsMatch(reportedResult, RegexIsHex)))
                            {
                                DisconnectClient(client, "result malformed");
                                return;
                            }

                            double prob = ((double)HexToUInt32(ji.Target)) / ((double)0xffffffff);
                            long howmanyhashes = ((long)(1.0 / prob));

                            totalHashes += howmanyhashes;

                            if (ji.OwnJob)
                            {
                                // that was an "own" job. could be that the target does not match

                                if (!CheckHashTarget(ji.Target, reportedResult))
                                {
                                    Console.WriteLine("Hash does not reach our target difficulty.");
                                    return;
                                }

                                totalOwnHashes += howmanyhashes;
                            }

                            // default chance to get hash-checked is 10%
                            double chanceForACheck = 0.1;

                            // check new clients more often, but prevent that to happen the first 30s the server is running
                            if (Heartbeats > 3 && client.NumChecked < 9) chanceForACheck = 1.0 - 0.1 * client.NumChecked;

                            bool performFullCheck = (Random2.NextDouble() < chanceForACheck && HashesCheckedThisHeartbeat < MaxHashChecksPerHeartbeat);

                            if (performFullCheck)
                            {
                                client.NumChecked++;
                                HashesCheckedThisHeartbeat++;
                            }

                            bool validHash = CheckHash(ji, reportedResult, reportedNonce, performFullCheck);

                            if (!validHash)
                            {
                                CConsole.ColorWarning(() =>
                                   Console.WriteLine("{0} got disconnected for WRONG hash.", client.WebSocket.ConnectionInfo.Id.ToString()));

                                if (!string.IsNullOrEmpty(ipadr)) Firewall.Update(ipadr, Firewall.UpdateEntry.WrongHash);
                                RemoveClient(client.WebSocket.ConnectionInfo.Id);
                            }
                            else
                            {
                                if (performFullCheck)
                                    Console.WriteLine("{0}: got hash-checked", client.WebSocket.ConnectionInfo.Id.ToString());

                                if (!string.IsNullOrEmpty(ipadr)) Firewall.Update(ipadr, Firewall.UpdateEntry.SolvedJob);

                                ji.Solved.TryAdd(reportedNonce.ToLower());

                                if (!ji.OwnJob) client.PoolConnection.Hashes += howmanyhashes;

                                Client jiClient = client;
                                if (ji.OwnJob) jiClient = ourself;

                                string msg1 = "{\"id\":\"" + jiClient.PoolConnection.PoolId +
                                    "\",\"job_id\":\"" + ji.InnerId +
                                    "\",\"nonce\":\"" + msg["nonce"].GetString() +
                                    "\",\"result\":\"" + msg["result"].GetString() +
                                    "\"}";

                                string msg0 = "{\"method\":\"" + "submit" +
                                    "\",\"params\":" + msg1 +
                                    ",\"id\":\"" + "1" + "\"}\n"; // TODO: check the "1"

                                jiClient.PoolConnection.Send(jiClient, msg0);
                            }

                        }).Start();

                    } // identified == solved
                };
            });

            bool running = true;
            double totalSpeed = 0, totalOwnSpeed = 0;

            while (running)
            {
                Firewall.Heartbeat(Heartbeats++);

                try
                {
                    Task.Run(async delegate { await Task.Delay(TimeSpan.FromSeconds(HeartbeatRate)); }).Wait();

                    if (Heartbeats % SpeedAverageOverXHeartbeats == 0)
                    {
                        totalSpeed = (double)totalHashes / (double)(HeartbeatRate * SpeedAverageOverXHeartbeats);
                        totalOwnSpeed = (double)totalOwnHashes / (double)(HeartbeatRate * SpeedAverageOverXHeartbeats);

                        totalHashes = 0;
                        totalOwnHashes = 0;
                    }

                    CConsole.ColorInfo(() =>
                       Console.WriteLine("[{0}] heartbeat; connections client/pool: {1}/{2}; jobqueue: {3}k; speed: {4}kH/s",
                           DateTime.Now.ToString(),
                           clients.Count,
                           PoolConnectionFactory.Connections.Count,
                           ((double)jobQueue.Count / 1000.0d).ToString("F1"),
                           ((double)totalSpeed / 1000.0d).ToString("F1")));

                    while (jobQueue.Count > JobCacheSize)
                    {
                        if (jobQueue.TryDequeue(out string deq))
                        {
                            jobInfos.TryRemove(deq);
                        }
                    }

                    DateTime now = DateTime.Now;

                    List<PoolConnection> pcc = new List<PoolConnection>(PoolConnectionFactory.Connections.Values);
                    foreach (PoolConnection pc in pcc)
                    {
                        PoolConnectionFactory.CheckPoolConnection(pc);
                    }

                    List<Client> cc = new List<Client>(clients.Values);

                    foreach (Client c in cc)
                    {
                        try
                        {
                            if ((now - c.Created).TotalSeconds > GraceConnectionTime)
                            {
                                if (c.PoolConnection == null || c.PoolConnection.TcpClient == null) DisconnectClient(c, "timeout.");
                                else if (!c.PoolConnection.TcpClient.Connected) DisconnectClient(c, "lost pool connection.");
                                else if ((now - c.LastPoolJobTime).TotalSeconds > PoolTimeout)
                                {
                                    DisconnectClient(c, "pool is not sending new jobs.");
                                }

                            }
                        }
                        catch { RemoveClient(c.WebSocket.ConnectionInfo.Id); }
                    }

                    if (clients.ContainsKey(Guid.Empty))
                    {
                        if (clients.Count == 1)
                            RemoveClient(Guid.Empty);
                    }
                    else
                    {
                        if (clients.Count > 4 && Donation.DonationLevel > double.Epsilon)
                        {
                            CConsole.ColorWarning(() =>
                               Console.WriteLine("disconnected from own pool. trying to reconnect."));
                            ownJob = new Job();
                            CreateOurself();
                        }
                    }

                    HashesCheckedThisHeartbeat = 0;

                    if (Heartbeats % ForceGCEveryXHeartbeat == 0)
                    {
                        CConsole.ColorInfo(() =>
                        {
                            Console.WriteLine("Currently using {1} MB ({0} clients).", clients.Count,
                                Math.Round(((double)(GC.GetTotalMemory(false)) / 1024 / 1024)));

                        });
                    }
                }
                catch (Exception ex)
                {
                    CConsole.ColorAlert(() =>
                       Console.WriteLine("Exception caught in the main loop ! {0}", ex));
                }

            }

        }
    }
}
