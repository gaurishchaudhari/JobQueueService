using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using JobQueue;

namespace DemoJob
{
    class Program
    {
        static void Main(string[] args)
        {
            // The DLL or EXE that has the implementation of job should be different from the one the submits the job.

            JobQueueUtils.QueueNewJob(
                @"F:\JobQueueFile.tsv",
                @"F:\Projects\DemoJob\bin\Debug\DemoJob.exe", 
                "ComputeJob", 
                new [] {"1", "3", @"F:\output-job1.txt"},
                DateTime.Now,
                -1,
                "demo job"
            );
        }
    }
}
