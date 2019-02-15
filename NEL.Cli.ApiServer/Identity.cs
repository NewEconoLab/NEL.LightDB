using System;
using System.Collections.Generic;
using System.Text;

namespace NEL.Cli.ApiServer
{
    public class Identity
    {
        public string Host;
        public string Mehotd;
        public string ID;

        public Identity(string host,string method,string id)
        {
            Host = host;
            Mehotd = method;
            ID = id;
        }

        public override string ToString()
        {
            return string.Format("{0}|{1}|{2}",Host , Mehotd , ID);
        }

        public static Identity ToIdentity(string str)
        {
            string[] strs = str.Split("|");
            if (strs.Length != 3)
                throw new Exception("error:conver to identity faild");
            return new Identity(strs[0], strs[1], strs[2]);
        }
    }
}
