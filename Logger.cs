using System;
using System.IO;
using System.Reflection;

namespace SCUMBot
{
    public static class Logger
    {
        private static string m_exePath = string.Empty;
        public static void LogWrite(string logMessage)
        {
            m_exePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            
            if (!File.Exists(m_exePath + "\\" + "scumbot_debug.txt"))
            {
                File.Create(m_exePath + "\\" + "scumbot_debug.txt");
            }

            try
            {
                using (StreamWriter w = File.AppendText(m_exePath + "\\" + "scumbot_debug.txt"))
                {
                    AppendLog(logMessage, w);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }

        private static void AppendLog(string logMessage, TextWriter txtWriter)
        {
            try
            {
                txtWriter.WriteLine("{0} {1}: {2}", DateTime.Now.ToLongTimeString(), DateTime.Now.ToLongDateString(), logMessage);
            }
            catch (Exception ex)
            {
            }
        }
    }
}
