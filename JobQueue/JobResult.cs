using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JobQueue
{
    public class JobResult
    {
        public JobStatus Status;

        public DateTime NextScheduleTimestamp;
    }
}
