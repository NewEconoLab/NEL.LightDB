using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace NEL.Simple.SDK.DataCache
{
    public class DataCache_QueueValue<TKey, TValue>:ICache<TKey, TValue>
    {
        public DataCache_QueueValue()
        {

        }

        private ConcurrentDictionary<TKey, ConcurrentQueue<TValue>> dictionary = new ConcurrentDictionary<TKey, ConcurrentQueue<TValue>>();

        public void Add(TKey key, TValue value)
        {
            lock (dictionary)
            {
                if (dictionary.ContainsKey(key))
                {
                    dictionary[key].Enqueue(value);
                }
                else
                {
                    dictionary.TryAdd(key, new ConcurrentQueue<TValue>());
                    dictionary[key].Enqueue(value);
                }
            }
        }

        public async Task<TValue> Get(TKey key)
        {
            await Task.Delay(0);
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
            }
        }
    }
}
