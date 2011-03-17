

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace MTConnectAgentCore
{
    public class LogToFile
    {
        public static String currentLogFileName;
        public static bool log;

        public static void Initialize(bool _log)
        {
            log = _log;
            currentLogFileName = getLogFileName();
        }
        public static void Log(string msg)
        {
            if (log)
            {
                System.IO.StreamWriter sw = null;
                String newLogFileName = getLogFileName();
                if (!currentLogFileName.Equals(newLogFileName))
                {
                    currentLogFileName = newLogFileName;//date is changed so log file is renewed.
                }
                sw = System.IO.File.AppendText(currentLogFileName);
                try
                {
                    string logLine = System.String.Format(
                        "<Log> {0:G}: {1} </Log>", System.DateTime.Now, msg);
                    sw.WriteLine(logLine);
                }
                finally
                {
                    sw.Close();
                }
            }
        }

        private static String getLogFileName()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar + "agent_" + Util.GetToday() + ".log";
        }
    }
}
