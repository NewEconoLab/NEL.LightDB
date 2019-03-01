using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NEL.Simple.SDK.Helper
{
    public static class MongoDBHelper
    {
        private static Dictionary<string, MongoClient> clientPool = new Dictionary<string, MongoClient>();
        private static MongoClient GetOrAdd(string mongodbConn)
        {
            if (clientPool.ContainsKey(mongodbConn))
                return clientPool[mongodbConn];
            else
            {
                var client = new MongoClient(mongodbConn);
                clientPool.Add(mongodbConn, client);
                return client;
            }
        }
        private static IMongoDatabase GetDatabase(this MongoClient client, string mongodbDatabase)
        {
            return client.GetDatabase(mongodbDatabase);
        }
        private static IMongoCollection<T> GetMongoCollection<T>(this IMongoDatabase database, string mongodbColl)
        {
            return database.GetCollection<T>(mongodbColl);
        }
        public static void InsertOne<T>(string mongodbConn, string mongodbDatabase, string mongodbColl, T data)
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetCollection<T>(mongodbColl);
            try
            {
                coll.InsertOne(data);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static List<T> Get<T>(string mongodbConn, string mongodbDatabase, string mongodbColl, string findFliter,string sortStr = "")
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetCollection<T>(mongodbColl);
            return coll.Find(findFliter).ToList();
        }
        public static List<T> Get<T>(string mongodbConn, string mongodbDatabase, string mongodbColl, int skipCount, int limitCount, string findFliter, string sortStr = "")
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetCollection<T>(mongodbColl);
            return coll.Find(findFliter).Skip(skipCount).Limit(limitCount).ToList();
        }
        public static void ReplaceData<T>(string mongodbConn, string mongodbDatabase, string mongodbColl, string whereFliter, T data)
        {
            try
            {
                var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetCollection<T>(mongodbColl);
                coll.ReplaceOne(whereFliter, data);
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        public static void CreateIndex(string mongodbConn, string mongodbDatabase, string mongodbColl, string indexDefinition,string indexName,bool isUnique = false)
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetCollection<BsonDocument>(mongodbColl);
            //检查是否已有设置index
            bool isSet = false;
            using (var cursor = coll.Indexes.List())
            {
                JArray JAindexs = JArray.Parse(cursor.ToList().ToJson());
                var query = JAindexs.Children().Where(index => (string)index["name"] == indexName);
                if (query.Count() > 0) isSet = true;
                // do something with the list...
            }

            if (!isSet)
            {
                try
                {
                    var options = new CreateIndexOptions { Name = indexName, Unique = isUnique };
                    coll.Indexes.CreateOne(new CreateIndexModel<BsonDocument>(indexDefinition, options));
                }
                catch { }
            }
        }
    }
}
