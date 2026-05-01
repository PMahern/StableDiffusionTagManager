namespace StableDiffusionTagManager.ViewModels
{
    public enum BatchQueueItemStatus
    {
        Pending,
        Running,
        AwaitingReview,
        Completed,
        Failed
    }
}
