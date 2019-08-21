namespace JobQueue
{
    public interface IQueueJob
    {
        JobResult Run(string[] args, ILogger logger);

    }
}
