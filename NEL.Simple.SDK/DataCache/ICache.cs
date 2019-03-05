using System.Threading.Tasks;

namespace NEL.Simple.SDK.DataCache
{
    public interface ICache<TKey, TValue>
    {
        void Add(TKey key,TValue value);
        Task<TValue> Get(TKey key);
    }
}
