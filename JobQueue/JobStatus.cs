namespace JobQueue
{
    public enum JobStatus
    {
        Unknown,
        Queued,
        Retry,
        Finished,
        Faulted,
        ExceededMaxRetryLimit,
        Locked,
    }
}
