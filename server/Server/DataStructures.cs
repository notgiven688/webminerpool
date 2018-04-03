using System.Collections.Generic;
using System.Collections.Concurrent;


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

    public class CcQueue<T> : ConcurrentQueue<T>
    {
    }

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

        public int Count { get { return dictionary.Count; }}

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

