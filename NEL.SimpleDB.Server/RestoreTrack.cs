﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using NEL.Simple.SDK.Helper;
namespace NEL.SimpleDB.Server
{
    class RestoreTrack
    {
        private string conn;
        private string db;
        private string coll;

        public RestoreTrack(Setting setting)
        {
            conn = setting.Conn_Track;
            db = setting.DataBase_Track;
            coll = setting.Coll_Track;
        }

        public void Start()
        {
            var task = new Task(()=> 
            {
                while (true)
                {
                    //获取高度
                    var height = StorageService.maindb.UseSnapShot().DataHeight;
                    //从mongo中获取data然后存入到本地
                    var list = MongoDBHelper.Get<TrackForMongodb>(conn, db, coll, "{height:" + height + "}");
                    if (list.Count > 0)
                    {
                        Restore(list);
                    }
                    if (height % 10000 == 0)
                    {
                        Console.WriteLine("height:"+height+"    time:"+DateTime.UtcNow);
                    }
                }
            });
            task.Start();

        }

        public void Restore(IList<TrackForMongodb> list)
        {
            IWriteBatch wb = StorageService.maindb.CreateWriteBatch();
            foreach (var l in list)
            {
                if (l.state == (byte)Neo.IO.Caching.TrackState.Added)
                {
                    wb.Put(l.tableid.ToBytes(),l.key?.Bytes,l.value?.Bytes);
                }
                else if (l.state == (byte)Neo.IO.Caching.TrackState.Deleted)
                {
                    wb.Delete(l.tableid.ToBytes(),l.key?.Bytes);
                }
                else if (l.state == (byte)Neo.IO.Caching.TrackState.Changed)
                {
                    wb.Put(l.tableid.ToBytes(), l.key?.Bytes, l.value?.Bytes);
                }
            }
            StorageService.maindb.WriteBatch(wb);
        }
    }
}