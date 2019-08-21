using System.Configuration;

namespace JobQueueService
{
    public class Settings
    {
        public int PollInterval;

        public string JobQueueFile;

        public string JobQueueFileTmp => JobQueueFile + ".tmp";

        public string FinishedJobsFile;

        public string LogDir;

        public int MaxRetries;
        
        public void Load()
        {
            PollInterval = int.Parse(ConfigurationManager.AppSettings["PollInterval"]);

            JobQueueFile = ConfigurationManager.AppSettings["JobQueueFile"];
            FinishedJobsFile = ConfigurationManager.AppSettings["FinishedJobsFile"];
            LogDir = ConfigurationManager.AppSettings["LogDir"];
            MaxRetries = int.Parse(ConfigurationManager.AppSettings["MaxRetries"]);
        }
    }
}
