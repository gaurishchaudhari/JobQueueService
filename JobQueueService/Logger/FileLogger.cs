using System;
using System.IO;
using System.Text;
using JobQueue;

namespace JobQueueService.Logger
{
    public class FileLogger : ILogger
    {
        private readonly string _logFile;
        public FileLogger(string logFile)
        {
            this._logFile = logFile;
        }

        public void Log(string message)
        {
            Log(message, "INFO");
        }

        public void Log(string message, string type)
        {
            WriteLine($"[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}]: [{type}]: {message}");
        }

        private void WriteLine(string text, bool append = true)
        {
            using (var sw = new StreamWriter(this._logFile, append, Encoding.UTF8))
            {
                if (!string.IsNullOrEmpty(text))
                {
                    sw.WriteLine(text);
                }
            }
        }
    }
}
