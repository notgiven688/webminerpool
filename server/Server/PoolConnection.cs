using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Collections.Concurrent;

using TinyJson;

using JsonData = System.Collections.Generic.Dictionary<string, object>;

namespace Server
{

    public class PoolConnection
	{
		public TcpClient Client;

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

		public CcDictionary<Client,byte> WebClients = new CcDictionary<Client,byte>();

		public void Send(Client client, string msg)
		{
			try { 
				Byte[] bytesSent = Encoding.ASCII.GetBytes(msg);
				Client.GetStream().BeginWrite(bytesSent,0,bytesSent.Length,SendCallback,null);
				this.LastSender = client;
			}
			catch { }
		}

		private void SendCallback(IAsyncResult result)
		{
			if (!Client.Connected) return;

			try { 
				NetworkStream networkStream = Client.GetStream ();
				networkStream.EndWrite(result); }
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


		public static ConcurrentDictionary<string,PoolConnection> Connections 
		= new ConcurrentDictionary<string,PoolConnection> ();


		private static bool VerifyJob(JsonData data)
		{
			if (data == null) return false;

			if (!data.ContainsKey ("job_id")) {
				return false;
			}
			if (!data.ContainsKey ("blob")) {
				return false;
			}
			if (!data.ContainsKey ("target")) {
				return false;
			}

			string blob = data["blob"].GetString();
			string target = data["target"].GetString();

			if (blob.Length != 152) {
				return false;
			}
			if (target.Length != 8) {
				return false;
			}

			if (!Regex.IsMatch (blob, MainClass.RegexIsHex)) {
				return false;
			}
			if (!Regex.IsMatch (target, MainClass.RegexIsHex)) {
				return false;
			}

			return true;
		}


		private static void ReceiveCallback(IAsyncResult result) {

			PoolConnection mygang = result.AsyncState as PoolConnection;
			TcpClient client = mygang.Client;

			if (!client.Connected) return;

			NetworkStream networkStream;

			try { networkStream = client.GetStream (); }
			catch { return; }

			int bytesread = 0;

			try {
				bytesread = networkStream.EndRead(result); 
			}
			catch { return; }

			string json = string.Empty;

			try {
				if(bytesread == 0) // disconnect
				{

					// slow that down a bit

					var t = Task.Run(async delegate
						{
							await Task.Delay(TimeSpan.FromSeconds(4));

							List<Client> cllist = new List<Client> (mygang.WebClients.Keys);
							foreach (Client ev in cllist) Disconnect(ev,"lost pool connection.");
						}); 

					return;
				}

				json = ASCIIEncoding.ASCII.GetString (mygang.ReceiveBuffer, 0, bytesread);


			networkStream.BeginRead(mygang.ReceiveBuffer, 0, mygang.ReceiveBuffer.Length, new AsyncCallback (ReceiveCallback), mygang);
			
			}
			catch { return; }

			if (bytesread == 0 || string.IsNullOrEmpty(json)) return; //?!

			var msg = json.FromJson<JsonData>();
			if (msg == null) return;

			if (string.IsNullOrEmpty (mygang.PoolId)) {

				// this "protocol" is strange
				if (!msg.ContainsKey ("result")) {

					string additionalInfo = "none";

					// try to get the error
					if (msg.ContainsKey ("error")) {
						msg = msg ["error"] as JsonData;

						if (msg != null && msg.ContainsKey ("message"))
							additionalInfo = msg ["message"].GetString ();
					}


					List<Client> cllist = new List<Client> (mygang.WebClients.Keys);
					foreach (Client ev in cllist)
						Disconnect (ev, "can not connect. additional information: " +  additionalInfo);

					return;
				}

				msg = msg ["result"] as JsonData;

				if (msg == null)
					return;
				if (!msg.ContainsKey ("id"))
					return;
				if (!msg.ContainsKey ("job"))
					return;

				mygang.PoolId = msg ["id"].GetString ();

				var lastjob = msg ["job"]  as JsonData;

				if (!VerifyJob (lastjob)) {
					Console.WriteLine ("Failed to verify job.");
					return;
				}

				mygang.LastJob = lastjob;
				mygang.LastInteraction = DateTime.Now;

				mygang.LastSolved = new CcHashset<string> ();

				List<Client> cllist2 = new List<Client> (mygang.WebClients.Keys);
				foreach (Client ev in cllist2) {
					ReceiveJob (ev, mygang.LastJob, mygang.LastSolved );
				}


			}
			else if (msg.ContainsKey ("method") && msg ["method"].GetString () == "job")
			{
				if (!msg.ContainsKey ("params"))
					return;

				var lastjob = msg ["params"]  as JsonData;

				if (!VerifyJob (lastjob)) {
					Console.WriteLine ("Failed to verify job.");
					return;
				}

				mygang.LastJob = lastjob;
				mygang.LastInteraction = DateTime.Now;
				mygang.LastSolved = new CcHashset<string> ();

				List<Client> cllist2 = new List<Client> (mygang.WebClients.Keys);

				Console.WriteLine ("Sending to {0} clients!", cllist2.Count);


				
				foreach (Client ev in cllist2) {
					ReceiveJob (ev, mygang.LastJob, mygang.LastSolved );
				}


			} 
			else 

			{
				if (msg.ContainsKey ("error")) {
					// who knows?
					ReceiveError (mygang.LastSender, msg);

				} else {
					Console.WriteLine ("Pool is sending nonsense...");
				}	
			}
		}

		private static void ConnectCallback(IAsyncResult result) 
		{

			PoolConnection mygang = result.AsyncState as PoolConnection;
			TcpClient client = mygang.Client;

			if (!mygang.Closed && client.Connected) {

				try {
				NetworkStream networkStream = client.GetStream ();
				mygang.ReceiveBuffer = new byte[client.ReceiveBufferSize];

				networkStream.BeginRead(mygang.ReceiveBuffer, 0, mygang.ReceiveBuffer.Length, new AsyncCallback (ReceiveCallback), mygang);
		
				/* keep things stupid and simple */

				string msg0 = "{\"method\":\"login\",\"params\":{\"login\":\"";
				string msg1 = "\",\"pass\":\"";
					string msg2 = "\",\"agent\":\"webminerpool.com\"},\"id\":1}";

				string msg = msg0 + mygang.Login + msg1 + mygang.Password + msg2 + "\n";

				mygang.Send(mygang.LastSender, msg);
				}
				catch { return; }
			} 
			else {

				// slow that down a bit

				var t = Task.Run (async delegate {
					await Task.Delay (TimeSpan.FromSeconds (4));

					List<Client> cllist = new List<Client> (mygang.WebClients.Keys);
					foreach (Client ev in cllist)
						Disconnect (ev, "can not connect to pool.");
				});
			
			}
	}


		public static void Close(PoolConnection connection, Client client)
		{
			connection.WebClients.TryRemove(client);

			if (connection.WebClients.Count == 0) {

				connection.Closed = true;

				try {
					var networkStream = connection.Client.GetStream (); 
					networkStream.EndRead(null);
				} catch{ }

				try {connection.Client.Close ();}  			catch{ }
				try {connection.Client.Client.Close ();}  	catch{ }
				try {connection.ReceiveBuffer = null;}  	catch{ }

				try{ PoolConnection dummy; Connections.TryRemove(connection.Credentials, out dummy);}catch{}

				Console.WriteLine ("{0}: closed a pool connection.", client.WebSocket.ConnectionInfo.Id);

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
			if ((DateTime.Now - connection.LastInteraction).TotalMinutes < 10)
				return;

			Console.WriteLine ("Initiating reconnect!");
			Console.WriteLine (connection.Credentials);

			try {
				var networkStream = connection.Client.GetStream (); 
				networkStream.EndRead(null);
			} catch{ }

			try {connection.Client.Close ();}  				catch{ }
			try {connection.Client.Client.Close ();}  		catch{ }
			try {connection.ReceiveBuffer = null;}  		catch{ }

			connection.LastInteraction = DateTime.Now;

			connection.PoolId = "";
			connection.LastJob = null;

			connection.Client = new TcpClient ();

			connection.Client.Client.SetKeepAlive (60000, 1000);
			connection.Client.Client.ReceiveBufferSize = 4096*2;

			try{ connection.Client.BeginConnect (connection.Url, connection.Port, new AsyncCallback (ConnectCallback), connection); }
			catch{}

		}


		public static PoolConnection CreatePoolConnection (Client client, string url, int port, string login, string password)
		{
							
			string credential = url + port.ToString () + login + password;

			PoolConnection mygang;

			if (!Connections.TryGetValue (credential, out mygang)) {

				Console.WriteLine ("{0}: established new pool connection. {1} {2} {3}", client.WebSocket.ConnectionInfo.Id,url, login, password);
			
				mygang = new PoolConnection ();
				mygang.Credentials = credential;
				mygang.LastSender = client;

				mygang.Client = new TcpClient ();

				mygang.Client.Client.SetKeepAlive (60000, 1000);
				mygang.Client.Client.ReceiveBufferSize = 4096*2;

				mygang.Login = login;
				mygang.Password = password;
				mygang.Port = port;
				mygang.Url = url;

				mygang.WebClients.TryAdd (client,byte.MaxValue);

				Connections.TryAdd (credential, mygang);

				try{ mygang.Client.Client.BeginConnect (url, port, new AsyncCallback (ConnectCallback), mygang); }
				catch{}

			} else {

				Console.WriteLine ("{0}: reusing pool connection.", client.WebSocket.ConnectionInfo.Id);

				mygang.WebClients.TryAdd (client,byte.MaxValue);

				if (mygang.LastJob != null) ReceiveJob (client, mygang.LastJob,mygang.LastSolved);
				else Console.WriteLine ("{0} no job yet.", client.WebSocket.ConnectionInfo.Id);

			}

			client.TcpClient = mygang;

			return mygang;


		}
	}
}

