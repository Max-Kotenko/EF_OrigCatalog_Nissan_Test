using System;
using System.IO;

namespace EF_OrigCatalog_Nissan_Test
{
    interface ILog
    {
        void OutData(string log_data);
    }

    class FileLogger : ILog
    {
        private static object FileShareLock = new object();
        private string LogFilePath;

        public FileLogger(string LogFilePath)
        {
            this.LogFilePath = LogFilePath;
        }

        public void OutData(string log_data)
        {
            while (true)
            {
                try
                {
                    lock (FileShareLock)
                    {
                        File.AppendAllText(this.LogFilePath, log_data);
                        return;
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }
    class ConsoleLogger : ILog
    {
        private static object ConsoleShareLock = new object();
        public void OutData(string log_data)
        {
            while (true)
            {
                try
                {
                    lock (ConsoleShareLock)
                    {
                        Console.WriteLine(log_data);
                        return;
                    }
                }
                catch (Exception)
                {
                }
            }
        }
    }

    public static class Logger
    {
        static private ILog log;
        static Logger()
        {
            log = new ConsoleLogger();//after debug end, change to file logger!
            //log = new FileLogger(@"D:\OriginalCatalogs\NISSAN\NISSAN\log.txt");
        }
        public static void WriteLogText(string log_text)
        {
            log.OutData(log_text);
        }
        public static void WriteLogException(Exception ex)
        {
            log.OutData(string.Format("Exception message: {0} \r\n\tStackTrace: {1}", ex.Message, ex.StackTrace));
        }

    }
}
