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
            if (dictionary.ContainsKey(key))
                dictionary[key].Enqueue(value);
            else
            {
                dictionary.TryAdd(key, new ConcurrentQueue<TValue>());
                dictionary[key].Enqueue(value);
            }
        }

        public async Task<TValue> Get(TKey key)
        {
            while (true)
            {
                if (dictionary.ContainsKey(key) && dictionary[key].Count > 0)
                {
                    TValue value;
                    dictionary[key].TryDequeue(out value);
                    return value;
                }
                await Task.Delay(0);
            }
        }
    }
}
