//  --------------------------------------------------------------------------------------------------------------------
//  <copyright file="JobQueueService.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//  </copyright>
//  --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Timers;
using JobQueue;
using JobQueueService.Logger;

namespace JobQueueService
{
    public partial class JobQueueService : ServiceBase
    {
        private readonly Settings _settings;
        private readonly Timer _timer;
        private ILogger _logger;

        public JobQueueService()
        {
            InitializeComponent();

            _settings = new Settings();
            _settings.Load();

            _timer = new Timer();

            var loggerFileName = Path.Combine(
                _settings.LogDir,
                "JobQueueService_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            _logger = new FileLogger(loggerFileName);

            if (!File.Exists(_settings.JobQueueFile))
            {
                JobQueueUtils.CreateJobQueueHeader(_settings.JobQueueFile);
            }
        }

        protected override void OnStart(string[] args)
        {
            _logger.Log("Starting JobQueueService.");

            _timer.Elapsed += OnElapsedTime;
            _timer.Interval = _settings.PollInterval * 1000; // converto to ms
            _timer.Enabled = true;
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            var loggerFileName = Path.Combine(
                _settings.LogDir,
                "JobQueueService_" + DateTime.Now.ToString("yyyy-MM-dd") + ".log");
            if (!File.Exists(loggerFileName))
            {
                _logger = new FileLogger(Path.Combine(_settings.LogDir, loggerFileName));
            }

            _logger.Log("Recalling JobQueueService.");

            // clear jobs queue

            var activeJobs = new List<Job>();
            try
            {
                ClearJobQueue(); // clear job queue

                activeJobs = GetActiveJobs().ToList();
            }
            catch (Exception ex)
            {
                _logger.Log("Failed to load active jobs " + ex);
            }

            // Process Queue
            foreach (var job in activeJobs)
            {
                _logger.Log("Current Jobs " + job.JobId);

                if (job.JobStatus == JobStatus.Locked)
                {
                    _logger.Log("Skipping Job " + job.JobId + ", Status = " + JobStatus.Locked);
                    continue;
                }

                bool isExceedMaxRetry = (job.MaxRunCount == -1)
                    ? (job.RunCount >= _settings.MaxRetries)
                    : (job.RunCount >= job.MaxRunCount);

                if (isExceedMaxRetry)
                {
                    _logger.Log("Job " + job.JobId + ", Exceeded Max retry limit, Setting status = " + JobStatus.ExceededMaxRetryLimit);
                    job.JobStatus = JobStatus.ExceededMaxRetryLimit;
                    UpdateJobQueue(job);
                    continue;
                }

                try
                {
                    var assembly = Assembly.LoadFile(job.Assembly);
                    foreach (Type type in assembly.GetTypes())
                    {
                        if (typeof(IQueueJob).IsAssignableFrom(type))
                        {
                            if (!type.Name.Contains(job.JobName))
                            {
                                continue;
                            }

                            _logger.Log(
                                "Start executing job " + job.JobId + ", Type = " + type.Name + ", JobString = " +
                                job.JobString);

                            job.JobStatus = JobStatus.Locked;
                            UpdateJobQueue(job);

                            var obj = (IQueueJob)Activator.CreateInstance(type);
                            JobResult jr = obj.Run(job.Args, _logger);
                            job.RunCount += 1;

                            job.JobStatus = jr.Status;
                            if (jr.NextScheduleTimestamp != DateTime.MinValue)
                            {
                                job.ScheduledTimestamp = jr.NextScheduleTimestamp;
                            }
                            UpdateJobQueue(job);

                            _logger.Log("Finished executing job " + job.JobId + ", Status = " + jr.Status);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(
                        "Finished executing job " + job.JobId + ", Status = " + JobStatus.Faulted + ", Exception: " +
                        ex);

                    job.JobStatus = JobStatus.Faulted;
                    job.Comments = "Exception thrown. Check logs."; // + ex.Message.Replace("\n", "|-|");
                    UpdateJobQueue(job);
                }
                
            }
        }

        void UpdateJobQueue(Job j)
        {
            using (var sw = new StreamWriter(_settings.JobQueueFile, true, Encoding.UTF8))
            {
                sw.WriteLine(
                    string.Join(
                        "\t",
                        j.JobId,
                        DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss.fff tt"),
                        j.ScheduledTimestamp.ToString("yyyy-MM-dd hh:mm:ss.fff tt"),
                        j.JobStatus.ToString(),
                        j.JobString,
                        j.RunCount.ToString(),
                        j.MaxRunCount.ToString(),
                        j.Comments));
            }
        }

        protected override void OnStop()
        {
            _logger.Log("Stopping JobQueueService.");
        }

        private IEnumerable<Job> GetActiveJobs()
        {
            var currentJobs = ReadJobQueue().Values
                .Where(j => j.JobStatus == JobStatus.Queued || j.JobStatus == JobStatus.Retry);

            var currentTimestamp = DateTime.Now;
            return currentJobs.Where(j => currentTimestamp >= j.ScheduledTimestamp); // ScheduledTimestamp is in past
        }

        private static bool ShouldArchieveJob(Job job)
        {
            return job.JobStatus == JobStatus.Finished || job.JobStatus == JobStatus.Faulted ||
                   job.JobStatus == JobStatus.ExceededMaxRetryLimit;
        }

        private void ClearJobQueue()
        {
            if (!File.Exists(_settings.FinishedJobsFile))
            {
                JobQueueUtils.CreateJobQueueHeader(_settings.FinishedJobsFile);
            }

            var jobsMap = ReadJobQueue();

            HashSet<string> archievedJobs = new HashSet<string>();

            foreach (var job in jobsMap.Values.Where(ShouldArchieveJob))
            {
                JobQueueUtils.InsertJob(
                    _settings.FinishedJobsFile,
                    job.JobId,
                    job.Timestamp,
                    job.ScheduledTimestamp,
                    job.Assembly,
                    job.JobName,
                    job.Args,
                    job.JobStatus,
                    job.RunCount,
                    job.MaxRunCount,
                    job.Comments);

                archievedJobs.Add(job.JobId);
            }

            JobQueueUtils.CreateJobQueueHeader(_settings.JobQueueFileTmp);

            foreach (var job in jobsMap.Values.Where(j => !archievedJobs.Contains(j.JobId)))
            {
                JobQueueUtils.InsertJob(
                    _settings.JobQueueFileTmp,
                    job.JobId,
                    job.Timestamp,
                    job.ScheduledTimestamp,
                    job.Assembly,
                    job.JobName,
                    job.Args,
                    job.JobStatus,
                    job.RunCount,
                    job.MaxRunCount,
                    job.Comments);
            }

            File.Delete(_settings.JobQueueFile);
            File.Move(_settings.JobQueueFileTmp, _settings.JobQueueFile);
        }

        private Dictionary<string, Job> ReadJobQueue()
        {
            var jobsMap = new Dictionary<string, Job>();

            var columnsMap = new Dictionary<string, int>();
            var header = true;

            foreach (var line in File.ReadAllLines(_settings.JobQueueFile))
            {
                var fields = line.Split('\t');
                if (header)
                {
                    for (int i = 0; i < fields.Length; ++i)
                    {
                        columnsMap.Add(fields[i], i);
                    }

                    header = false;
                    continue;
                }

                var id = fields[columnsMap["JobId"]];
                var timestamp = DateTime.Parse(fields[columnsMap["Timestamp"]]);
                var scheduledTimestamp = DateTime.Parse(fields[columnsMap["ScheduledTimestamp"]]);
                var status = (JobStatus)Enum.Parse(typeof(JobStatus), fields[columnsMap["JobStatus"]]);
                var runCount = int.Parse(fields[columnsMap["RunCount"]]);
                var maxRunCount = int.Parse(fields[columnsMap["MaxRunCount"]]);
                var jobString = fields[columnsMap["JobString"]];
                var parts = jobString.Split('|');
                var comments = fields[columnsMap["Comments"]];

                var job = new Job
                {
                    Timestamp = timestamp,
                    ScheduledTimestamp = scheduledTimestamp,
                    JobId = id,
                    JobStatus = status,
                    Assembly = parts[0],
                    JobName = parts[1],
                    Args = parts.Skip(2).ToArray(),
                    JobString = jobString,
                    RunCount = runCount,
                    MaxRunCount = maxRunCount,
                    Comments = comments
                };

                if (!jobsMap.ContainsKey(id))
                {
                    jobsMap.Add(id, job);
                }
                else if (jobsMap[id].Timestamp < timestamp)
                {
                    jobsMap[id] = job;
                }
            }

            return jobsMap;
        }

        private class Job
        {
            public string[] Args;
            public string Assembly;
            public string Comments;
            public string JobId;
            public string JobName;
            public JobStatus JobStatus;
            public string JobString;
            public int MaxRunCount;
            public int RunCount;
            public DateTime ScheduledTimestamp;
            public DateTime Timestamp;
        }
    }
}
