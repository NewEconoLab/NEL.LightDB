using System;

namespace NEL.Common
{
    public class Logger : ILogger
    {
        public enum OUTPosition
        {
            Console = 0x01,
            Trace = 0x02,
            Debug = 0x04,
            File = 0x08,
            Other = 0x10,
        }
        public enum LogType
        {
            Info,
            Warn,
            Error,
        }
        string _outfilepath;
        public string outfilepath
        {
            get
            {
                return _outfilepath;
            }
            set
            {
                _outfilepath = value;
                if (outfilepath.Contains("/"))
                {
                    var path = System.IO.Path.GetDirectoryName(outfilepath);
                    if (System.IO.Directory.Exists(path) == false)
                        System.IO.Directory.CreateDirectory(path);
                }
            }
        }
        public ILogger otherLogger
        {
            get; set;
        }
        public OUTPosition outtag_info
        {
            get; set;
        }
        public OUTPosition outtag_warn
        {
            get; set;
        }
        public OUTPosition outtag_error
        {
            get; set;
        }
        public Logger()
        {
            var time = DateTime.Now;
            var filetime = time.ToString("yyyyMMdd_HHmmss");
            outfilepath = "log/log_" + filetime + ".log";
            otherLogger = null;
            outtag_info = OUTPosition.Console;
            outtag_warn = OUTPosition.Console | OUTPosition.Trace | OUTPosition.File;
            outtag_error = OUTPosition.Console | OUTPosition.Trace | OUTPosition.File;
        }

        void WriteLine(LogType type, OUTPosition outtag, string str)
        {
            string tag = null;

            if ((outtag & OUTPosition.Console) > 0)
            {
                if (tag == null)
                {
                    if (type == LogType.Info)
                        Console.ForegroundColor = ConsoleColor.Gray;
                    else if (type == LogType.Warn)
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    else if (type == LogType.Error)
                        Console.ForegroundColor = ConsoleColor.Red;
                }
                //Console.Write(tag);
                Console.WriteLine(str);

                Console.ForegroundColor = ConsoleColor.Gray;
            }
            if ((outtag & OUTPosition.Trace) > 0)
            {
                if (tag == null)
                {
                    if (type == LogType.Info)
                        tag = "<I>";
                    else if (type == LogType.Warn)
                        tag = "<W>";
                    else if (type == LogType.Error)
                        tag = "<E>";
                }
                System.Diagnostics.Trace.Write(tag);
                System.Diagnostics.Trace.WriteLine(str);
            }
            if ((outtag & OUTPosition.Debug) > 0)
            {
                if (tag == null)
                {
                    if (type == LogType.Info)
                        tag = "<I>";
                    else if (type == LogType.Warn)
                        tag = "<W>";
                    else if (type == LogType.Error)
                        tag = "<E>";
                }
                System.Diagnostics.Debug.Write(tag);
                System.Diagnostics.Debug.WriteLine(str);
            }
            lock (this)
            {
                if ((outtag & OUTPosition.File) > 0)
                {
                    try
                    {
                        System.IO.File.AppendAllText(outfilepath, tag + str +"\n", System.Text.Encoding.UTF8);

                    }
                    catch
                    {
                        Console.Write("<LOG ERROR>cant write to file:" + outfilepath + "=" + str);
                    }
                }
            }
            if (otherLogger != null && (outtag_info & OUTPosition.Other) > 0)
            {
                try
                {
                    if (type == LogType.Info)
                        otherLogger.Info(str);
                    else if (type == LogType.Warn)
                        otherLogger.Warn(str);
                    else if (type == LogType.Error)
                        otherLogger.Error(str);
                }
                catch
                {
                    Console.Write("<LOG ERROR>cant write to otherLogger:" + str);

                }
            }
        }
        public void Info(string str)
        {
            WriteLine(LogType.Info, outtag_info, str);
        }

        public void Warn(string str)
        {
            WriteLine(LogType.Warn, outtag_warn, str);
        }

        public void Error(string str)
        {
            WriteLine(LogType.Error, outtag_error, str);
        }
    }

}
