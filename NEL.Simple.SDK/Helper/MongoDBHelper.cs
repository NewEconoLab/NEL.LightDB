using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace NEL.Simple.SDK.Helper
{
    public static class MongoDBHelper
    {
        private static Dictionary<string, MongoClient> clientPool = new Dictionary<string, MongoClient>();

        private static MongoClient GetOrAdd(string mongodbConn)
        {
            lock (clientPool)
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
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<T>(mongodbColl);
            try
            {
                coll.InsertOne(data);
            }
            catch (Exception e)
            {
                if (e.Message.Contains("E11000")) //如果是重复插入导致的唯一索引报错 就不暴露错误了
                    Console.WriteLine(e.Message);
                else
                    throw e;
            }
        }

        public static List<T> Get<T>(string mongodbConn, string mongodbDatabase, string mongodbColl, string findFliter,string sortStr = "")
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<T>(mongodbColl);
            return coll.Find(findFliter).ToList();
        }

        public static List<T> Get<T>(string mongodbConn, string mongodbDatabase, string mongodbColl, int skipCount, int limitCount, string findFliter, string sortStr = "")
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<T>(mongodbColl);
            return coll.Find(findFliter).Skip(skipCount).Limit(limitCount).ToList();
        }


        public static JArray Get(string mongodbConn, string mongodbDatabase, string mongodbColl, string findFliter, string sortStr = "")
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<BsonDocument>(mongodbColl);
            var query = coll.Find(findFliter).ToList();
            var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            return JArray.Parse(query.ToJson(jsonWriterSettings));
        }

        public static JArray Get(string mongodbConn, string mongodbDatabase, string mongodbColl, int skipCount, int limitCount, string findFliter, string sortStr = "")
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<BsonDocument>(mongodbColl);
            var query =  coll.Find(findFliter).Skip(skipCount).Limit(limitCount).ToList();
            var jsonWriterSettings = new JsonWriterSettings { OutputMode = JsonOutputMode.Strict };
            return JArray.Parse(query.ToJson(jsonWriterSettings));
        }

        public static void ReplaceData<T>(string mongodbConn, string mongodbDatabase, string mongodbColl, string whereFliter, T data)
        {
            try
            {
                var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<BsonDocument>(mongodbColl);
                List<BsonDocument> query = coll.Find(whereFliter).ToList();
                if (query.Count == 0)//表示并没有数据
                {
                    var collection2 = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<T>(mongodbColl);
                    collection2.InsertOne(data);
                }
                else
                {
                    var collection2 = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<T>(mongodbColl);
                    collection2.ReplaceOne(whereFliter, data);
                }
            }
            catch(Exception e)
            {
                throw e;
            }
        }

        public static void DeleteData(string mongodbConn, string mongodbDatabase, string mongodbColl, string whereFliter)
        {
            try
            {
                var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<BsonDocument>(mongodbColl);
                coll.DeleteMany(BsonDocument.Parse(whereFliter));
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        public static void CreateIndex(string mongodbConn, string mongodbDatabase, string mongodbColl, string indexDefinition,string indexName,bool isUnique = false)
        {
            var coll = GetOrAdd(mongodbConn).GetDatabase(mongodbDatabase).GetMongoCollection<BsonDocument>(mongodbColl);
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
                catch(Exception e)
                {
                    throw e;
                }
            }
        }
    }
}
