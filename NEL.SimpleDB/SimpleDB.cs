using System;
using System.Collections;
using System.Collections.Generic;

namespace NEL.SimpleDB
{
    public class DB : IDisposable
    {
        IntPtr dbPtr;
        IntPtr defaultWriteOpPtr;
        public void Open(string path, bool createIfMissing = false)
        {
            if (dbPtr != IntPtr.Zero)
                throw new Exception("already open a db.");
            this.defaultWriteOpPtr = RocksDbSharp.Native.Instance.rocksdb_writeoptions_create();

            var HandleOption = RocksDbSharp.Native.Instance.rocksdb_options_create();
            if (createIfMissing)
            {
                RocksDbSharp.Native.Instance.rocksdb_options_set_create_if_missing(HandleOption, true);
            }
            RocksDbSharp.Native.Instance.rocksdb_options_set_compression(HandleOption, RocksDbSharp.CompressionTypeEnum.rocksdb_snappy_compression);
            //RocksDbSharp.DbOptions option = new RocksDbSharp.DbOptions();
            //option.SetCreateIfMissing(true);
            //option.SetCompression(RocksDbSharp.CompressionTypeEnum.rocksdb_snappy_compression);
            IntPtr handleDB = RocksDbSharp.Native.Instance.rocksdb_open(HandleOption, path);
            this.dbPtr = handleDB;

            snapshotLast = CreateSnapInfo();
            snapshotLast.AddRef();
        }
        public void Dispose()
        {
            snapshotLast.Dispose();
            snapshotLast = null;

            RocksDbSharp.Native.Instance.rocksdb_writeoptions_destroy(this.defaultWriteOpPtr);
            this.defaultWriteOpPtr = IntPtr.Zero;
            RocksDbSharp.Native.Instance.rocksdb_close(this.dbPtr);
            this.dbPtr = IntPtr.Zero;
        }
        //创建快照
        private SnapShot CreateSnapInfo()
        {
            //看最新高度的快照是否已经产生
            var snapshot = new SnapShot(this.dbPtr);
            snapshot.Init();
            return snapshot;
        }
        private SnapShot snapshotLast;

        //如果 height=0，取最新的快照
        public ISnapShot UseSnapShot()
        {
            var snap = snapshotLast;

            snap.AddRef();
            return snap;
        }
        public IWriteBatch CreateWriteBatch()
        {
            return new WriteBatch(this.dbPtr, UseSnapShot() as SnapShot);
        }
        public void WriteBatch(IWriteBatch wb)
        {
            RocksDbSharp.Native.Instance.rocksdb_write(this.dbPtr, this.defaultWriteOpPtr, (wb as WriteBatch).batchptr);
            snapshotLast.Dispose();
            snapshotLast = CreateSnapInfo();
            snapshotLast.AddRef();
        }
        private byte[] GetDirectFinal(byte[] finalkey)
        {
            var data = RocksDbSharp.Native.Instance.rocksdb_get(dbPtr, snapshotLast.readopHandle, finalkey);
            if (data == null || data.Length == 0)
                return null;
            return data;
        }
        public byte[] GetDirect(byte[] tableid, byte[] key)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, key);
            return GetDirectFinal(finalkey);
        }
        public UInt64 GetUInt64Direct(byte[] tableid, byte[] key)
        {
            var data = GetDirect(tableid, key);
            if (data == null || data.Length == 0)
                return 0;
            else return BitConverter.ToUInt64(data, 0);
        }
        public void PutUInt64Direct(byte[] tableid, byte[] key, UInt64 v)
        {
            this.PutDirect(tableid, key, BitConverter.GetBytes(v));
        }
        private void DeleteDirectFinal(byte[] finalkey)
        {
            RocksDbSharp.Native.Instance.rocksdb_delete(this.dbPtr, this.defaultWriteOpPtr, finalkey, finalkey.LongLength);

        }
        private void PutDirectFinal(byte[] finalkey, byte[] data)
        {
            RocksDbSharp.Native.Instance.rocksdb_put(this.dbPtr, this.defaultWriteOpPtr, finalkey, (UIntPtr)finalkey.Length, data, (UIntPtr)data.Length, out IntPtr err);
            if (err != IntPtr.Zero)
            {
                return;
            }
        }
        public void PutDirect(byte[] tableid, byte[] key, byte[] data)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, key);
            var countkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableCount);



            var countdata = GetDirectFinal(countkey);
            UInt32 count = 0;
            if (countdata != null)
            {
                count = BitConverter.ToUInt32(countdata, 0);
            }
            var vdata = GetDirectFinal(finalkey);
            if (vdata == null || vdata.Length == 0)
            {
                count++;
            }
            else
            {
                if (LightDB.Helper.BytesEquals(vdata, data) == false)
                    count++;
            }
            PutDirectFinal(finalkey, data);

            var countvalue = BitConverter.GetBytes(count);
            PutDirectFinal(countkey, countvalue);

        }
        public void DeleteDirect(byte[] tableid, byte[] key)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, key);

            var countkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableCount);
            var countdata = GetDirectFinal(countkey);
            UInt32 count = 0;
            if (countdata != null)
            {
                count = BitConverter.ToUInt32(countdata, 0);
            }

            var vdata = GetDirectFinal(finalkey);
            if (vdata != null && vdata.Length != 0)
            {
                DeleteDirectFinal(finalkey);
                count--;
                var countvalue = BitConverter.GetBytes(count);
                PutDirectFinal(countkey, countvalue);

            }
        }
        public void CreateTableDirect(byte[] tableid, byte[] info)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableInfo);
            var countkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableCount);
            var data = GetDirectFinal(finalkey);
            if (data != null && data.Length != 0)
            {
                throw new Exception("alread have that.");
            }
            PutDirectFinal(finalkey, info);

            var byteCount = GetDirectFinal(countkey);
            if (byteCount == null || byteCount.Length == 0)
            {
                byteCount = BitConverter.GetBytes((UInt32)0);
            }
            PutDirectFinal(countkey, byteCount);

        }
        public void DeleteTableDirect(byte[] tableid)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableInfo);
            //var countkey = Helper.CalcKey(tableid, null, SplitWord.TableCount);
            var vdata = GetDirectFinal(finalkey);
            if (vdata != null && vdata.Length != 0)
            {
                DeleteDirectFinal(finalkey);
            }
        }
    }
    public interface ISnapShot : IDisposable
    {
        byte[] GetValueData(byte[] tableid, byte[] key);
        IKeyFinder CreateKeyFinder(byte[] tableid, byte[] beginkey = null, byte[] endkey = null);
        IKeyIterator CreateKeyIterator(byte[] tableid, byte[] _beginkey = null, byte[] _endkey = null);
        byte[] GetTableInfoData(byte[] tableid);
        uint GetTableCount(byte[] tableid);
    }
    class SnapShot : ISnapShot
    {
        public SnapShot(IntPtr dbPtr)
        {
            this.dbPtr = dbPtr;
        }
        public void Init()
        {
            //this.readop = new RocksDbSharp.ReadOptions();
            this.readopHandle = RocksDbSharp.Native.Instance.rocksdb_readoptions_create();

            snapshotHandle = RocksDbSharp.Native.Instance.rocksdb_create_snapshot(this.dbPtr);
            RocksDbSharp.Native.Instance.rocksdb_readoptions_set_snapshot(readopHandle, snapshotHandle);
        }
        int refCount = 0;
        public IntPtr dbPtr;
        //public RocksDbSharp.RocksDb db;
        public IntPtr readopHandle;
        //public RocksDbSharp.ReadOptions readop;
        public IntPtr snapshotHandle = IntPtr.Zero;
        //public RocksDbSharp.Snapshot snapshot;

        public void Dispose()
        {
            lock (this)
            {
                refCount--;
                if (refCount == 0 && snapshotHandle != IntPtr.Zero)
                {
                    RocksDbSharp.Native.Instance.rocksdb_release_snapshot(this.dbPtr, snapshotHandle);
                    //snapshot.Dispose();
                    snapshotHandle = IntPtr.Zero;

                    RocksDbSharp.Native.Instance.rocksdb_readoptions_destroy(readopHandle);
                    readopHandle = IntPtr.Zero;
                }
            }
        }
        /// <summary>
        /// 对snapshot的引用计数加锁，保证处理是线程安全的
        /// </summary>
        public void AddRef()
        {
            lock (this)
            {
                refCount++;
            }
        }
        public byte[] GetValueData(byte[] tableid, byte[] key)
        {
            byte[] finialkey = LightDB.Helper.CalcKey(tableid, key);
            return RocksDbSharp.Native.Instance.rocksdb_get(this.dbPtr, this.readopHandle, finialkey);
            //(readOptions ?? DefaultReadOptions).Handle, key, keyLength, cf);

            //return this.db.Get(finialkey, null, readop);
        }
        public IKeyFinder CreateKeyFinder(byte[] tableid, byte[] beginkey = null, byte[] endkey = null)
        {
            TableKeyFinder find = new TableKeyFinder(this, tableid, beginkey, endkey);
            return find;
        }
        public IKeyIterator CreateKeyIterator(byte[] tableid, byte[] _beginkey = null, byte[] _endkey = null)
        {
            var beginkey = LightDB.Helper.CalcKey(tableid, _beginkey);
            var endkey = LightDB.Helper.CalcKey(tableid, _endkey);
            return new TableIterator(this, tableid, beginkey, endkey);
        }
        public byte[] GetTableInfoData(byte[] tableid)
        {
            var tablekey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableInfo);
            var data = RocksDbSharp.Native.Instance.rocksdb_get(this.dbPtr, this.readopHandle, tablekey);
            if (data == null)
                return null;
            return data;
        }
        public uint GetTableCount(byte[] tableid)
        {
            var tablekey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableCount);
            var data = RocksDbSharp.Native.Instance.rocksdb_get(this.dbPtr, this.readopHandle, tablekey);
            return BitConverter.ToUInt32(data, 0);
        }
    }
    public interface IKeyIterator : IEnumerator<byte[]>
    {

    }
    public interface IKeyFinder : IEnumerable<byte[]>
    {

    }

    public interface IWriteBatch
    {
        ISnapShot snapshot
        {
            get;
        }
        byte[] GetData(byte[] finalkey);
        void CreateTable(byte[] tableid, byte[] finaldata);
        void DeleteTable(byte[] tableid);
        void Put(byte[] tableid, byte[] key, byte[] finaldata);
        void Delete(byte[] tableid, byte[] key);
    }
    class WriteBatch : IWriteBatch, IDisposable
    {
        public WriteBatch(IntPtr dbptr, SnapShot snapshot)
        {
            this.dbPtr = dbptr;
            this.batchptr = RocksDbSharp.Native.Instance.rocksdb_writebatch_create();
            //this.batch = new RocksDbSharp.WriteBatch();
            this._snapshot = snapshot;
            this.cache = new Dictionary<string, byte[]>();
        }
        //RocksDbSharp.RocksDb db;
        public IntPtr dbPtr;
        public SnapShot _snapshot;
        public ISnapShot snapshot
        {
            get
            {
                return _snapshot;
            }
        }
        //public RocksDbSharp.WriteBatch batch;
        public IntPtr batchptr;
        Dictionary<string, byte[]> cache;

        public void Dispose()
        {
            if (batchptr != IntPtr.Zero)
            {
                RocksDbSharp.Native.Instance.rocksdb_writebatch_destroy(batchptr);
                batchptr = IntPtr.Zero;
                //batch.Dispose();
                //batch = null;
            }
            _snapshot.Dispose();
        }
        public byte[] GetData(byte[] finalkey)
        {
            var hexkey = LightDB.Helper.ToString_Hex(finalkey);
            if (cache.ContainsKey(hexkey))
            {
                return cache[hexkey];
            }
            else
            {
                var data = RocksDbSharp.Native.Instance.rocksdb_get(dbPtr, _snapshot.readopHandle, finalkey);
                if (data == null || data.Length == 0)
                    return null;
                //db.Get(finalkey, null, snapshot.readop);
                cache[hexkey] = data;
                return data;
            }
        }
        private void PutDataFinal(byte[] finalkey, byte[] value)
        {
            var hexkey = LightDB.Helper.ToString_Hex(finalkey);
            cache[hexkey] = value;
            RocksDbSharp.Native.Instance.rocksdb_writebatch_put(batchptr, finalkey, (ulong)finalkey.Length, value, (ulong)value.Length);
            //batch.Put(finalkey, value);
        }
        private void DeleteFinal(byte[] finalkey)
        {
            var hexkey = LightDB.Helper.ToString_Hex(finalkey);
            cache.Remove(hexkey);
            RocksDbSharp.Native.Instance.rocksdb_writebatch_delete(batchptr, finalkey, (ulong)finalkey.Length);
            //batch.Delete(finalkey);
        }
        public void CreateTable(byte[] tableid, byte[] tableinfo)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableInfo);
            var countkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableCount);
            var data = GetData(finalkey);
            if (data != null && data.Length != 0)
            {
                throw new Exception("alread have that.");
            }
            PutDataFinal(finalkey, tableinfo);

            var byteCount = GetData(countkey);
            if (byteCount == null || byteCount.Length == 0)
            {
                byteCount = BitConverter.GetBytes((UInt32)0);
            }
            PutDataFinal(countkey, byteCount);
        }

        public void DeleteTable(byte[] tableid)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableInfo);
            //var countkey = Helper.CalcKey(tableid, null, SplitWord.TableCount);
            var vdata = GetData(finalkey);
            if (vdata != null && vdata.Length != 0)
            {
                DeleteFinal(finalkey);
            }
        }
        public void Put(byte[] tableid, byte[] key, byte[] finaldata)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, key);
            var countkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableCount);



            var countdata = GetData(countkey);
            UInt32 count = 0;
            if (countdata != null)
            {
                count = BitConverter.ToUInt32(countdata, 0);
            }
            var vdata = GetData(finalkey);
            if (vdata == null || vdata.Length == 0)
            {
                count++;
            }
            else
            {
                if (LightDB.Helper.BytesEquals(vdata, finaldata) == false)
                    count++;
            }
            PutDataFinal(finalkey, finaldata);

            var countvalue = BitConverter.GetBytes(count);
            PutDataFinal(countkey, countvalue);
        }

        public void Delete(byte[] tableid, byte[] key)
        {
            var finalkey = LightDB.Helper.CalcKey(tableid, key);

            var countkey = LightDB.Helper.CalcKey(tableid, null, LightDB.SplitWord.TableCount);
            var countdata = GetData(countkey);
            UInt32 count = 0;
            if (countdata != null)
            {
                count = BitConverter.ToUInt32(countdata, 0);
            }

            var vdata = GetData(finalkey);
            if (vdata != null && vdata.Length != 0)
            {
                DeleteFinal(finalkey);
                count--;
                var countvalue = BitConverter.GetBytes(count);
                PutDataFinal(countkey, countvalue);

            }
        }
    }


    class TableKeyFinder : IKeyFinder
    {
        public TableKeyFinder(SnapShot _snapshot, byte[] _tableid, byte[] _beginkey, byte[] _endkey)
        {
            this.snapshot = _snapshot;
            this.tableid = _tableid;
            this.beginkeyfinal = LightDB.Helper.CalcKey(_tableid, _beginkey);
            this.endkeyfinal = LightDB.Helper.CalcKey(_tableid, _endkey);
        }
        SnapShot snapshot;
        byte[] tableid;
        byte[] beginkeyfinal;
        byte[] endkeyfinal;
        public IEnumerator<byte[]> GetEnumerator()
        {
            return new TableIterator(snapshot, tableid, beginkeyfinal, endkeyfinal);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    class TableIterator : IKeyIterator
    {
        public TableIterator(SnapShot snapshot, byte[] _tableid, byte[] _beginkeyfinal, byte[] _endkeyfinal)
        {
            this.itPtr = RocksDbSharp.Native.Instance.rocksdb_create_iterator(snapshot.dbPtr, snapshot.readopHandle);
            //this.it = snapshot.db.NewIterator(null, snapshot.readop);
            this.tableid = _tableid;
            this.beginkeyfinal = _beginkeyfinal;
            this.endkeyfinal = _endkeyfinal;
            //this.Reset();

        }
        public UInt64 HandleID
        {
            get
            {
                return (UInt64)itPtr.ToInt64();
            }
        }
        bool bInit = false;
        IntPtr itPtr;
        //RocksDbSharp.Iterator it;
        byte[] tableid;
        byte[] beginkeyfinal;
        byte[] endkeyfinal;
        public byte[] Current
        {
            get
            {
                if (this.Vaild)
                {
                    var key = RocksDbSharp.Native.Instance.rocksdb_iter_key(itPtr);
                    var bytes = new byte[key.Length - this.tableid.Length - 2];
                    Buffer.BlockCopy(key, this.tableid.Length + 2, bytes, 0, bytes.Length);
                    return bytes;
                    //return it.Key().Skip(this.tableid.Length + 2).ToArray();
                }
                else
                    return null;
            }
        }

        object IEnumerator.Current
        {
            get
            {
                return Current;
            }
        }

        public bool Vaild
        {
            get;
            private set;
        }
        public bool TestVaild(byte[] data)
        {
            if (data.Length < this.endkeyfinal.Length)
                return false;
            for (var i = 0; i < endkeyfinal.Length; i++)
            {
                if (data[i] != this.endkeyfinal[i])
                    return false;
            }
            return true;
        }
        public bool MoveNext()
        {
            if (bInit == false)
            {
                bInit = true;
                RocksDbSharp.Native.Instance.rocksdb_iter_seek(itPtr, beginkeyfinal, (ulong)beginkeyfinal.Length);

                // it.Seek(beginkeyfinal);
            }
            else
            {
                RocksDbSharp.Native.Instance.rocksdb_iter_next(itPtr);

                //it.Next();
            }
            if (RocksDbSharp.Native.Instance.rocksdb_iter_valid(itPtr) == false)
                return false;
            var key = RocksDbSharp.Native.Instance.rocksdb_iter_key(itPtr);
            this.Vaild = TestVaild(key);
            return this.Vaild;
        }

        public void Reset()
        {
            RocksDbSharp.Native.Instance.rocksdb_iter_seek(itPtr, beginkeyfinal, (ulong)beginkeyfinal.Length);

            //it.Seek(beginkeyfinal);
            bInit = false;
            this.Vaild = false;
        }

        public void Dispose()
        {
            RocksDbSharp.Native.Instance.rocksdb_iter_destroy(this.itPtr);
            this.itPtr = IntPtr.Zero;
            //it.Dispose();
            //it = null;
        }
    }

}
