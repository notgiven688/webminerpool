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

using JsonData = System.Collections.Generic.Dictionary<string, object>;


namespace Server
{

    public static class AlgorithmHelper
    {

        // quite a mess
        // https://github.com/xmrig/xmrig-proxy/blob/dev/doc/STRATUM_EXT.md#mining-algorithm-negotiation

        // we will only support the "algo" flag in short term notation!

        private static Dictionary<string, Tuple<string, int>> lookup = new Dictionary<string, Tuple<string, int>>
        {
            { "cn", new Tuple<string, int>("cn", -1) },
            { "cn/0", new Tuple<string, int>("cn", 0) },
            { "cn/1", new Tuple<string, int>("cn", 1) },
            { "cn/2", new Tuple<string, int>("cn", 2) },
            { "cn/r", new Tuple<string, int>("cn", 4) },
            { "cn-lite/0", new Tuple<string, int>("cn-lite", 0) },
            { "cn-lite/1", new Tuple<string, int>("cn-lite", 1) },
            { "cn-lite/2", new Tuple<string, int>("cn-lite", 2) },
            { "cn-pico/trtl", new Tuple<string, int>("cn-pico", 2) },
            { "cn/half", new Tuple<string, int>("cn-half", 2) }
        };

        public static bool ValidAlgorithm(string algo)
        {
            return lookup.ContainsKey(algo);
        }

        public static bool NormalizeAlgorithmAndVariant(JsonData job)
        {
            int possibleHeight = 0;

            if (job.ContainsKey("height"))
            {
                Int32.TryParse(job["height"].GetString(), out possibleHeight);
            }

            job["height"] = possibleHeight;

            string algo = job["algo"].GetString().ToLower();

            if (algo == "cn" || algo == "cryptonight")
            {
                int possibleVariant = -1;
                if (job.ContainsKey("variant"))
                {
                    Int32.TryParse(job["variant"].GetString(), out possibleVariant);
                }

                job["algo"] = "cn";
                job["variant"] = possibleVariant;
            }

            if (lookup.ContainsKey(algo))
            {
                var tuple = lookup[algo];
                job["algo"] = tuple.Item1;
                job["variant"] = tuple.Item2;
            }
            else
            {
                return false;
            }

            return true;
        }

    }
}