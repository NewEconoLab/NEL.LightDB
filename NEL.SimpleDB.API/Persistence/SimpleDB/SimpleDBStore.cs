using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using System;
using NEL.SimpleDB.API.DB;
using NEL.SimpleDB.API;

namespace Neo.Persistence.SimpleDB
{
    public class SimpleDBStore : Store, IDisposable
    {
        private readonly SimpleServerDB db;

        public SimpleDBStore(Setting setting)
        {
            db = SimpleServerDB.Open(setting);
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public override DataCache<UInt160, AccountState> GetAccounts()
        {
            return new DbCache<UInt160, AccountState>(db, Prefixes.ST_Account);
        }

        public override DataCache<UInt256, AssetState> GetAssets()
        {
            return new DbCache<UInt256, AssetState>(db, Prefixes.ST_Asset);
        }

        public override DataCache<UInt256, BlockState> GetBlocks()
        {
            return new DbCache<UInt256, BlockState>(db, Prefixes.DATA_Block);
        }

        public override DataCache<UInt160, ContractState> GetContracts()
        {
            return new DbCache<UInt160, ContractState>(db, Prefixes.ST_Contract);
        }

        public override Snapshot GetSnapshot()
        {
            return new DbSnapshot(db);
        }

        public override DataCache<UInt256, SpentCoinState> GetSpentCoins()
        {
            return new DbCache<UInt256, SpentCoinState>(db, Prefixes.ST_SpentCoin);
        }

        public override DataCache<StorageKey, StorageItem> GetStorages()
        {
            return new DbCache<StorageKey, StorageItem>(db, Prefixes.ST_Storage);
        }

        public override DataCache<UInt256, TransactionState> GetTransactions()
        {
            return new DbCache<UInt256, TransactionState>(db, Prefixes.DATA_Transaction);
        }

        public override DataCache<UInt256, UnspentCoinState> GetUnspentCoins()
        {
            return new DbCache<UInt256, UnspentCoinState>(db, Prefixes.ST_Coin);
        }

        public override DataCache<ECPoint, ValidatorState> GetValidators()
        {
            return new DbCache<ECPoint, ValidatorState>(db, Prefixes.ST_Validator);
        }

        public override DataCache<UInt32Wrapper, HeaderHashList> GetHeaderHashList()
        {
            return new DbCache<UInt32Wrapper, HeaderHashList>(db, Prefixes.IX_HeaderHashList);
        }

        public override MetaDataCache<ValidatorsCountState> GetValidatorsCount()
        {
            return new DbMetaDataCache<ValidatorsCountState>(db, Prefixes.IX_ValidatorsCount);
        }

        public override MetaDataCache<HashIndexState> GetBlockHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentBlock);
        }

        public override MetaDataCache<HashIndexState> GetHeaderHashIndex()
        {
            return new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentHeader);
        }
    }
}
