using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace Installer
{
    public class Log
    {
        private static readonly string FileName;

        public static readonly string Directory;

        private static string LogPrefix
        {
            get { return DateTime.Now.ToShortTimeString() + ": "; }
        }

        static Log()
        {
            var dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Directory = Path.Combine(dir, "Granikos\\NikosOne\\Logs");

            if (!System.IO.Directory.Exists(Directory))
            {
                System.IO.Directory.CreateDirectory(Directory);
            }

            FileName = Path.Combine(Directory, DateTime.Now.ToString("yyMMddHHmmss") + "_" + Assembly.GetEntryAssembly().GetName().Name + ".log");

            if (File.Exists(FileName))
            {
                File.Delete(FileName);
            }
        }

        public static void Info(string text)
        {
            File.AppendAllText(FileName, LogPrefix + text + "\r\n");
        }

        public static void Error(string text)
        {
            File.AppendAllText(FileName, LogPrefix + "ERROR\r\n=====\r\n" + text + "\r\n");
        }

        public static void Error(Exception ex)
        {
            var sb = new StringBuilder();
            sb.AppendLine(LogPrefix);
            sb.AppendLine("ERROR");
            sb.AppendLine("=====");

            while (ex != null)
            {
                sb.AppendLine(ex.Message);
#if DEBUG
                sb.AppendLine(ex.StackTrace);
#endif
                sb.AppendLine("-----------------------------------");
                ex = ex.InnerException;
            }

            File.AppendAllText(FileName, sb.ToString());
        }
    }
}
