using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NEL.Simple.SDK
{
    public class DataCache<TKey, TValue>
    {
        public DataCache()
        {

        }

        private ConcurrentDictionary<TKey, ConcurrentQueue<TValue>> dictionary = new ConcurrentDictionary<TKey, ConcurrentQueue<TValue>>();

        public void Add(TKey key, TValue value)
        {
            lock (dictionary)
            {
                //Console.WriteLine("111111111111111|" + key + "|" + value);
                if (dictionary.ContainsKey(key))
                    dictionary[key].Enqueue(value);
                else
                {
                    dictionary.TryAdd(key, new ConcurrentQueue<TValue>());
                    dictionary[key].Enqueue(value);
                }
            }
        }

        public async Task<TValue> Get(TKey key)
        {
            while (true)
            {
                lock (dictionary)
                {
                    if (dictionary.ContainsKey(key) && dictionary[key].Count > 0)
                    {
                        TValue value;
                        dictionary[key].TryDequeue(out value);
                        if (dictionary[key].Count == 0)
                        {
                            ConcurrentQueue<TValue> q;
                            dictionary.TryRemove(key, out q);
                        }
                        if (value != null)
                            return value;
                    }
                }
                await Task.Delay(0);
            }
        }
    }
}
