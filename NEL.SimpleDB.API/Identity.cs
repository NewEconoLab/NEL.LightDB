using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.SimpleDB.API
{
    public class Identity
    {
        public string Host;
        public string Mehotd;
        public string ID;
        public string Key;

        public Identity(string host,string method,string id,string key)
        {
            Host = host;
            Mehotd = method;
            ID = id;
            Key = key;
        }

        public Identity()
        {
            Host = "";
            Mehotd = "";
            ID = "";
            Key = "";
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}|{3}",Host , Mehotd , ID,Key);
        }

        public static Identity ToIdentity(string str)
        {
            if (str.IndexOf("|") < 0)
                return new Identity();
            string[] strs = str.Split("|");
            if (strs.Length != 4)
                return new Identity();
            return new Identity(strs[0], strs[1], strs[2], strs[3]);
        }
    }
}
