using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueue
{
    public static class JobQueueUtils
    {
        public static void QueueNewJob(
            string assembly,
            string jobName,
            string[] args,
            DateTime scheduledTimestamp = default(DateTime),
            int maxRunCount = -1,
            string comments = "")
        {
            QueueNewJob(@"F:\QueueService\JobQueueFile.tsv", assembly, jobName, args, scheduledTimestamp, maxRunCount, comments);
        }

        private static void QueueNewJob(string queueFile, string assembly, string jobName, string[] args, DateTime scheduledTimestamp = default(DateTime), int maxRunCount = -1, string comments = "")
        {
            if (scheduledTimestamp == default(DateTime))
            {
                scheduledTimestamp = DateTime.Now;
            }

            if (!File.Exists(queueFile))
            {
                CreateJobQueueHeader(queueFile);
            }

            InsertJob(queueFile, Guid.NewGuid().ToString(), DateTime.Now, scheduledTimestamp, assembly, jobName, args, JobStatus.Queued, 0, maxRunCount, comments);
        }

        public static void CreateJobQueueHeader(string file)
        {
            using (var sw = new StreamWriter(file, false, Encoding.UTF8))
            {
                sw.WriteLine(string.Join("\t", new[]
                    { "JobId", "Timestamp", "ScheduledTimestamp", "JobStatus", "JobString", "RunCount", "MaxRunCount", "Comments" }
                ));
            }
        }

        public static void InsertJob(
            string file,
            string jobId,
            DateTime timestamp,
            DateTime scheduledTimestamp,
            string assembly,
            string jobName,
            string[] args,
            JobStatus status,
            int runCount,
            int maxRunCount,
            string comments)
        {
            using (var sw = new StreamWriter(file, true, Encoding.UTF8))
            {
                sw.WriteLine(string.Join("\t", new[]
                    {
                        jobId,
                        timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff tt"),
                        scheduledTimestamp.ToString("yyyy-MM-dd hh:mm:ss.fff tt"),
                        status.ToString(),
                        assembly + "|" + jobName + "|" + string.Join("|", args),
                        runCount.ToString(),
                        maxRunCount.ToString(),
                        comments,
                    }
                ));
            }
        }
    }
}
