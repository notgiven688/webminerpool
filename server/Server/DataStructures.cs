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

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Server
{

    public class CcDictionary<T, V> : ConcurrentDictionary<T, V>
    {
        public bool TryRemove(T item)
        {
            V dummy;
            return this.TryRemove(item, out dummy);
        }

    }

    public class CcQueue<T> : ConcurrentQueue<T> { }

    public class CcHashset<T>
    {
        ConcurrentDictionary<T, byte> dictionary = new ConcurrentDictionary<T, byte>();

        public bool TryAdd(T item)
        {
            return dictionary.TryAdd(item, byte.MaxValue);
        }

        public ICollection<T> Values
        {
            get { return dictionary.Keys; }
        }

        public int Count { get { return dictionary.Count; } }

        public bool Contains(T item)
        {
            return dictionary.ContainsKey(item);
        }

        public bool TryRemove(T item)
        {
            byte dummy;
            return dictionary.TryRemove(item, out dummy);
        }

    }
}