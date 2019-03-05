using System;
using System.Collections.Generic;
using System.Text;
using Neo;
using System.Threading.Tasks;
using Neo.Persistence;
using System.Linq;

namespace Neo.Persistence.SimpleDB
{
    public class UpdateHeaderHashList
    {
        public List<UInt256> header_index { get; private set; }
        public Store store;

        public static UpdateHeaderHashList Ins;
        public static void CreateIns(Store store)
        {
            Ins = new UpdateHeaderHashList(store);
        }
        private UpdateHeaderHashList()
        {
        }
        private UpdateHeaderHashList(Store _store)
        {
            store = _store;
            header_index = new List<UInt256>();
            StartUpdate();
        }

        private void StartUpdate()
        {
            header_index = new List<UInt256>();
            header_index.AddRange(store.GetHeaderHashList().Find().OrderBy(p => (uint)p.Key).SelectMany(p => p.Value.Hashes));
        }

        public UInt256 GetHeader(int index)
        {
            lock (header_index)
            {
                if (index >= header_index.Count)
                {
                    header_index = new List<UInt256>();
                    header_index.AddRange(store.GetHeaderHashList().Find().OrderBy(p => (uint)p.Key).SelectMany(p => p.Value.Hashes));
                }
                return header_index[index];
            }
        }

    }
}
