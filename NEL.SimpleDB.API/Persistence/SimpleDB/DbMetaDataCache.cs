using NEL.Simple.SDK.Helper;
using Neo.IO;
using Neo.IO.Caching;
using System;
using NEL.SimpleDB.API.DB;

namespace Neo.Persistence.SimpleDB
{
    internal class DbMetaDataCache<T> : MetaDataCache<T>
        where T : class, ICloneable<T>, ISerializable, new()
    {
        private readonly SimpleServerDB db;
        private readonly byte prefix;

        public DbMetaDataCache(SimpleServerDB db, byte prefix, Func<T> factory = null)
            : base(factory)
        {
            this.db = db;
            this.prefix = prefix;
        }

        protected override void AddInternal(T item)
        {
        }

        protected override T TryGetInternal()
        {
            //if (!db.TryGet(options, prefix, out Slice slice))
            //    return null;
            //return slice.ToArray().AsSerializable<T>();
            return db.Get<T>(prefix.ToBytes());
        }

        protected override void UpdateInternal(T item)
        {
        }

        public override void Commit(ulong height)
        {
            base.Commit();
        }
    }
}
