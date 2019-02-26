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
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public static class Firewall
    {

        public enum UpdateEntry
        {
            SolvedJob,
            AuthSuccess,
            AuthFailure,
            WrongHash,
            Handshake
        }

        private class Entry
        {
            public string Address;

            public Entry(string adr)
            {
                Address = adr;
            }

            public int SolvedJobs = 0;
            public int WrongHash = 0;
            public int AuthSuccess = 0;
            public int AuthFailure = 0;
            public int Handshake = 0;

            public DateTime FirstSeen = DateTime.Now;
        }

        private static CcDictionary<string, Entry> entries = new CcDictionary<string, Entry>();

        public const int CheckTimeInHeartbeats = 6 * 10; //  every 10min

        private static void AddToIpTables(Entry entry, int rule)
        {
            Helper.WriteTextAsyncWrapper("ip_list", entry.Address + Environment.NewLine);
            CConsole.ColorWarning(() => Console.WriteLine("Added {0} to ip_list (rule #{1})", entry.Address, rule.ToString()));
            entries.TryRemove(entry.Address);
        }

        public static void Update(string ip, UpdateEntry update)
        {
            Entry entry = null;
            if (entries.TryGetValue(ip, out entry))
            {

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
            }
            else
            {
                entries.TryAdd(ip, new Entry(ip));
            }

        }

        public static void Heartbeat(int heartBeats)
        {

            List<Entry> entrylst = new List<Entry>(entries.Values);

            foreach (Entry entry in entrylst)
            {
                // decide here...
                if (entry.AuthSuccess == 0 && entry.SolvedJobs == 0 &&
                    entry.AuthFailure > 20)
                {
                    AddToIpTables(entry, 1);
                }
                else if (entry.AuthFailure > 500 && entry.AuthSuccess < 500)
                {
                    AddToIpTables(entry, 2);
                }
                else if (entry.AuthSuccess + entry.AuthFailure > 1000 && entry.SolvedJobs < 3)
                {
                    AddToIpTables(entry, 3);
                }
                else if (entry.AuthSuccess + entry.AuthFailure > 4000)
                {
                    AddToIpTables(entry, 4);
                }
                else if (entry.WrongHash > 0 && entry.AuthSuccess < 5)
                {
                    AddToIpTables(entry, 5);
                }
                else if (entry.AuthSuccess + entry.AuthFailure > 2000 && entry.Handshake < 1)
                {
                    AddToIpTables(entry, 6);
                }
            }

            if ((heartBeats % CheckTimeInHeartbeats) == 0)
            {
                entries.Clear();
            }
        }

    }
}