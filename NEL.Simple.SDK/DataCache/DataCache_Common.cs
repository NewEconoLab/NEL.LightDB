using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace NEL.Simple.SDK.DataCache
{
    public class DataCache_Common<TKey, TValue> : ICache<TKey, TValue>
    {
        private ConcurrentDictionary<TKey, TValue> dic = new ConcurrentDictionary<TKey, TValue>();

        public void Add(TKey key, TValue value)
        {
            lock (dic)
            {
                dic.TryAdd(key, value);
            }
        }

        public async Task<TValue> Get(TKey key)
        {
            while (true)
            {
                TValue value;
                lock (dic)
                {
                    if (dic.TryGetValue(key, out value))
                        return value;
                }
                await Task.Delay(0);
            }
        }
    }
}
