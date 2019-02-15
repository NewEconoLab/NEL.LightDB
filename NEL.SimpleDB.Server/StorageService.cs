using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.SimpleDB.Server
{
    public class StorageService
    {
        public static DB maindb { get; private set; }

        public static bool state_DBOpen { get; private set; }

        public StorageService()
        {
            maindb = new DB();
            state_DBOpen = false;
            string fullpath = System.IO.Path.GetFullPath(Setting.StoragePath);
            if (System.IO.Directory.Exists(fullpath) == false)
                System.IO.Directory.CreateDirectory(fullpath);
            string pathDB = System.IO.Path.Combine(fullpath, "maindb");
            try
            {
                maindb.Open(pathDB);
                state_DBOpen = true;
                Console.WriteLine("db opened in:" + pathDB);
            }
            catch (Exception err)
            {
                Console.WriteLine("error msg:" + err.Message);
            }
            if (state_DBOpen == false)
            {
                Console.WriteLine("open database fail. try to create it.");
                maindb.Open(pathDB,true);
            }
        }
    }
}
