using System.ServiceProcess;

namespace JobQueueService
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new global::JobQueueService.JobQueueService(), 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
