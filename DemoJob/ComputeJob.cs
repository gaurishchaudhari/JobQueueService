using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JobQueue;

namespace DemoJob
{
    public class ComputeJob : IQueueJob
    {
        public JobResult Run(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Incorrect # of arguments: " + string.Join(",", args));
            }

            int num1 = int.Parse(args[0]);
            int num2 = int.Parse(args[1]);

            int result = num1 + num2;

            File.AppendAllLines(args[2], new [] {$"{DateTime.Now} Before Sleep: Adding {num1} and {num2}, result = {result}" });

            if (num1 < 1000)
            {
                File.AppendAllLines(args[2], new [] { "Sleeping for " + num1 + " seconds..." });
                Thread.Sleep(num1 * 1000);
            }
            
            File.AppendAllLines(args[2], new[] { $"{DateTime.Now} After Sleep: Adding {num1} and {num2}, result = {result}" });

            if (num1 > 100 && num2 > 100)
            {
                return new JobResult
                {
                    Status = JobStatus.Retry,
                    NextScheduleTimestamp = DateTime.Now.AddSeconds(15),
                };
            }

            return new JobResult
            {
                Status = JobStatus.Finished,
                NextScheduleTimestamp = default(DateTime),
            };
        }
    }
}
