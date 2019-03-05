using Neo.Cryptography.ECC;
using Neo.IO.Caching;
using Neo.IO.Wrappers;
using Neo.Ledger;
using NEL.SimpleDB.API.DB;

namespace Neo.Persistence.SimpleDB
{
    public class DbSnapshot : Snapshot
    {
        private readonly SimpleServerDB db;

        public override DataCache<UInt256, BlockState> Blocks { get; }
        public override DataCache<UInt256, TransactionState> Transactions { get; }
        public override DataCache<UInt160, AccountState> Accounts { get; }
        public override DataCache<UInt256, UnspentCoinState> UnspentCoins { get; }
        public override DataCache<UInt256, SpentCoinState> SpentCoins { get; }
        public override DataCache<ECPoint, ValidatorState> Validators { get; }
        public override DataCache<UInt256, AssetState> Assets { get; }
        public override DataCache<UInt160, ContractState> Contracts { get; }
        public override DataCache<StorageKey, StorageItem> Storages { get; }
        public override DataCache<UInt32Wrapper, HeaderHashList> HeaderHashList { get; }
        public override MetaDataCache<ValidatorsCountState> ValidatorsCount { get; }
        public override MetaDataCache<HashIndexState> BlockHashIndex { get; }
        public override MetaDataCache<HashIndexState> HeaderHashIndex { get; }

        public DbSnapshot(SimpleServerDB db)
        {
            this.db = db;
            Blocks = new DbCache<UInt256, BlockState>(db, Prefixes.DATA_Block);
            Transactions = new DbCache<UInt256, TransactionState>(db, Prefixes.DATA_Transaction);
            Accounts = new DbCache<UInt160, AccountState>(db, Prefixes.ST_Account);
            UnspentCoins = new DbCache<UInt256, UnspentCoinState>(db, Prefixes.ST_Coin);
            SpentCoins = new DbCache<UInt256, SpentCoinState>(db, Prefixes.ST_SpentCoin);
            Validators = new DbCache<ECPoint, ValidatorState>(db, Prefixes.ST_Validator);
            Assets = new DbCache<UInt256, AssetState>(db, Prefixes.ST_Asset);
            Contracts = new DbCache<UInt160, ContractState>(db, Prefixes.ST_Contract);
            Storages = new DbCache<StorageKey, StorageItem>(db, Prefixes.ST_Storage);
            HeaderHashList = new DbCache<UInt32Wrapper, HeaderHashList>(db, Prefixes.IX_HeaderHashList);
            ValidatorsCount = new DbMetaDataCache<ValidatorsCountState>(db, Prefixes.IX_ValidatorsCount);
            BlockHashIndex = new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentBlock);
            HeaderHashIndex = new DbMetaDataCache<HashIndexState>(db, Prefixes.IX_CurrentHeader);
        }

        public override void Commit()
        {
            base.Commit();
        }

        public override void Dispose()
        {
        }
    }
}
