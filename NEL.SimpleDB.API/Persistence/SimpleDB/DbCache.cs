using Neo.IO;
using Neo.IO.Caching;
using System;
using System.Collections.Generic;
using NEL.SimpleDB.API.DB;
using NEL.Simple.SDK.Helper;
using System.Linq;

namespace Neo.Persistence.SimpleDB
{
    internal class DbCache<TKey, TValue> : DataCache<TKey, TValue>
        where TKey : IEquatable<TKey>, ISerializable, new()
        where TValue : class, ICloneable<TValue>, ISerializable, new()
    {
        private readonly SimpleServerDB db;
        private readonly byte prefix;

        public DbCache(SimpleServerDB db,byte prefix)
        {
            this.db = db;
            this.prefix = prefix;
        }

        protected override void AddInternal(TKey key, TValue value)
        {
        }

        public override void DeleteInternal(TKey key)
        {
        }

        protected override IEnumerable<KeyValuePair<TKey, TValue>> FindInternal(byte[] beginKey)
        {
            return db.Find(prefix.ToBytes().Concat(beginKey).ToArray(), (k, v) => new KeyValuePair<TKey, TValue>(k.ToArray().AsSerializable<TKey>(1), v.ToArray().AsSerializable<TValue>()));
        }

        protected override TValue GetInternal(TKey key)
        {
            return db.Get<TValue>(prefix.ToBytes(),key.ToArray());
        }

        protected override TValue TryGetInternal(TKey key)
        {
            return db.Get<TValue>(prefix.ToBytes(), key.ToArray());
        }

        protected override void UpdateInternal(TKey key, TValue value)
        {
        }

        public override void Commit(ulong height)
        {
            base.Commit();
        }
    }
}
