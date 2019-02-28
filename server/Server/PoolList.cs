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
using System.Text;
using System.Threading.Tasks;
using TinyJson;

using JsonData = System.Collections.Generic.Dictionary<string, object>;

namespace Server
{

    public struct PoolInfo
    {
        public int Port;
        public string Url;
        // some pools require a non-empty password
        public string EmptyPassword;
        public string DefaultAlgorithm;
    }

    public class PoolList
    {

        private Dictionary<string, PoolInfo> pools;
        private PoolList() { }

        public string JsonPools { private set; get; }

        public bool TryGetPool(string pool, out PoolInfo info)
        {
            return pools.TryGetValue(pool, out info);
        }

        public int Count { get { return pools.Count; } }

        public static PoolList LoadFromFile(string filename)
        {
            PoolList pl = new PoolList();
            pl.pools = new Dictionary<string, PoolInfo>();

            string json = File.ReadAllText(filename);

            JsonData data = json.FromJson<JsonData>();

            foreach (string pool in data.Keys)
            {
                JsonData jinfo = data[pool] as JsonData;
                PoolInfo pi = new PoolInfo();

                if (!(jinfo.ContainsKey("url") && jinfo.ContainsKey("port") &&
                        jinfo.ContainsKey("emptypassword") && jinfo.ContainsKey("algorithm")))
                    throw new Exception("Invalid entry.");

                pi.Url = jinfo["url"].GetString();
                pi.EmptyPassword = jinfo["emptypassword"].GetString();

                pi.DefaultAlgorithm = jinfo["algorithm"].GetString();
                pi.Port = int.Parse(jinfo["port"].GetString());

                if (!AlgorithmHelper.ValidAlgorithm(pi.DefaultAlgorithm))
                    throw new Exception("Invalid algorithm found in pools.json: " + pi.DefaultAlgorithm );

                    pl.pools.Add(pool, pi);

            }

            int counter = 0;

            pl.JsonPools = "{\"identifier\":\"" + "poolinfo";

            foreach (string pool in pl.pools.Keys)
            {
                counter++;
                pl.JsonPools += "\",\"pool" + counter.ToString() + "\":\"" + pool;
            }

            pl.JsonPools += "\"}\n";

            return pl;
        }

    }

}