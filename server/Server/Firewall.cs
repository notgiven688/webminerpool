using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Server {
	public static class Firewall {

		public enum UpdateEntry {
			SolvedJob,
			AuthSuccess,
			AuthFailure,
			WrongHash,
			Handshake
		}

		private class Entry {
			public string Address;

			public Entry (string adr) {
				Address = adr;
			}

			public int SolvedJobs = 0;
			public int WrongHash = 0;
			public int AuthSuccess = 0;
			public int AuthFailure = 0;
			public int Handshake = 0;

			public DateTime FirstSeen = DateTime.Now;
		}

		private static ConcurrentDictionary<string, Entry> entries = new ConcurrentDictionary<string, Entry> ();

		public const int CheckTimeInHeartbeats = 6 * 10; //  every 10min

		private static void AddToIpTables (string ip) {
			Helper.WriteTextAsyncWrapper ("ip_list", ip + Environment.NewLine);
		}

		public static void Update (string ip, UpdateEntry update) {
			Entry entry = null;
			if (entries.TryGetValue (ip, out entry)) {

				if (update == UpdateEntry.SolvedJob)
					entry.SolvedJobs++;
				else if (update == UpdateEntry.AuthFailure)
					entry.AuthFailure++;
				else if (update == UpdateEntry.AuthSuccess)
					entry.AuthSuccess++;
				else if (update == UpdateEntry.WrongHash)
					entry.WrongHash++;
				else if (update == UpdateEntry.Handshake)
					entry.Handshake++;
			} else {
				entries.TryAdd (ip, new Entry (ip));
			}

		}

		public static void Heartbeat (int heartBeats) {
			Entry dummy;

			List<Entry> entrylst = new List<Entry> (entries.Values);
			foreach (Entry entry in entrylst) {
				// decide here...
				if (entry.AuthSuccess == 0 && entry.SolvedJobs == 0 &&
					entry.AuthFailure > 20) {

					AddToIpTables (entry.Address);
					entries.TryRemove (entry.Address, out dummy);
					Console.WriteLine ("Added {0} to iptables (rule #1)", entry.Address);

				} else if (entry.AuthFailure > 500 && entry.AuthSuccess < 500) {
					AddToIpTables (entry.Address);
					entries.TryRemove (entry.Address, out dummy);
					Console.WriteLine ("Added {0} to iptables (rule #2)", entry.Address);
				} else if (entry.AuthSuccess + entry.AuthFailure > 1000 && entry.SolvedJobs < 3) {
					AddToIpTables (entry.Address);
					entries.TryRemove (entry.Address, out dummy);
					Console.WriteLine ("Added {0} to iptables (rule #3)", entry.Address);
				} else if (entry.AuthSuccess + entry.AuthFailure > 4000) {
					AddToIpTables (entry.Address);
					entries.TryRemove (entry.Address, out dummy);
					Console.WriteLine ("Added {0} to iptables (rule #4)", entry.Address);
				} else if (entry.WrongHash > 0 && entry.AuthSuccess < 5) {
					AddToIpTables (entry.Address);
					entries.TryRemove (entry.Address, out dummy);
					Console.WriteLine ("Added {0} to iptables (rule #5)", entry.Address);
				} else if (entry.AuthSuccess + entry.AuthFailure > 2000 && entry.Handshake < 1) {
					AddToIpTables (entry.Address);
					entries.TryRemove (entry.Address, out dummy);
					Console.WriteLine ("Added {0} to iptables (rule #6)", entry.Address);
				}
			}

			if ((heartBeats % CheckTimeInHeartbeats) == 0) {
				entries.Clear ();
			}
		}

	}
}