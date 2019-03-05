using Neo.IO;
using Neo.IO.Data.LevelDB;
using System;
using System.Collections.Generic;
using System.Linq;
using NEL.Simple.SDK.Helper;

namespace NEL.SimpleDB.API.DB
{
    public static class Helper
    {

        public static IEnumerable<T> Find<T>(this SimpleServerDB db, byte[] beginKey) where T : class, ISerializable, new()
        {
            return Find(db, beginKey, (k, v) => v.ToArray().AsSerializable<T>());
        }

        public static IEnumerable<T> Find<T>(this SimpleServerDB db, byte[] prefix, Func<byte[], byte[], T> resultSelector)
        {
            var snapid = db.UseSnapshot();

            var itid = db.CreateIterator(snapid, prefix);
            {
                while (db.MoveToNext(itid))
                {
                    byte[] key = db.Current(itid);
                    byte[] y = prefix;
                    if (key.Length < y.Length) break;
                    //if (!key.Take(y.Length).SequenceEqual(y)) break;

                    byte[] value = db.Get(prefix,key);

                    yield return resultSelector(value, value);
                }
            }
        }
    }
}
