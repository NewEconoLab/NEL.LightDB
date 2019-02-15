namespace NEL.Common
{
    public interface ILogger
    {
        void Info(string str);//级别最低，
        void Warn(string str);//警告
        void Error(string str);
    }

}
