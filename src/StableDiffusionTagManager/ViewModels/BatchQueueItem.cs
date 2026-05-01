using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Threading.Tasks;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class BatchQueueItem : ObservableObject
    {
        private readonly BatchQueueViewModel queue;

        public BatchQueueItem(
            BatchQueueViewModel queue,
            ImageWithTagsViewModel? image,
            string imageFilename,
            string folder,
            string operationDescription,
            Func<Task> operation,
            Func<Task>? reviewOperation = null)
        {
            this.queue = queue;
            Image = image;
            ImageFilename = imageFilename;
            Folder = folder;
            OperationDescription = operationDescription;
            Operation = operation;
            ReviewOperation = reviewOperation;
        }

        /// <summary>
        /// The image this operation targets, or null if the image is not in the currently loaded folder.
        /// Updated by BatchQueueViewModel.SyncPendingIndicatorsForFolder when the folder is loaded.
        /// </summary>
        public ImageWithTagsViewModel? Image { get; internal set; }

        /// <summary>
        /// The filename (not full path) of the target image. Always set, even when Image is null,
        /// so the queue can re-link to freshly loaded ImageWithTagsViewModel instances on folder switch.
        /// </summary>
        public string ImageFilename { get; }

        public string Folder { get; }
        public string OperationDescription { get; }
        internal Func<Task> Operation { get; }

        /// <summary>
        /// Optional second phase shown when the user clicks "Review". When set, the item enters
        /// AwaitingReview after the main operation completes rather than going straight to Completed.
        /// </summary>
        internal Func<Task>? ReviewOperation { get; }

        public bool HasReviewOperation => ReviewOperation != null;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPending))]
        [NotifyPropertyChangedFor(nameof(IsRunning))]
        [NotifyPropertyChangedFor(nameof(IsAwaitingReview))]
        [NotifyPropertyChangedFor(nameof(IsCompleted))]
        [NotifyPropertyChangedFor(nameof(IsFailed))]
        [NotifyCanExecuteChangedFor(nameof(StartReviewCommand))]
        private BatchQueueItemStatus status;

        [ObservableProperty]
        private string? errorMessage;

        public bool IsPending => Status == BatchQueueItemStatus.Pending;
        public bool IsRunning => Status == BatchQueueItemStatus.Running;
        public bool IsAwaitingReview => Status == BatchQueueItemStatus.AwaitingReview;
        public bool IsCompleted => Status == BatchQueueItemStatus.Completed;
        public bool IsFailed => Status == BatchQueueItemStatus.Failed;

        [RelayCommand(CanExecute = nameof(IsAwaitingReview))]
        public async Task StartReview()
        {
            if (ReviewOperation == null) return;
            try
            {
                await ReviewOperation();
                Status = BatchQueueItemStatus.Completed;
                Image?.SetHasPendingOperation(false);
                queue.OnItemReviewCompleted(this);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Status = BatchQueueItemStatus.Failed;
                Image?.SetHasPendingOperation(false);
                queue.OnItemReviewFailed(this);
            }
        }

        [RelayCommand]
        public void Retry()
        {
            ErrorMessage = null;
            if (ReviewOperation != null)
            {
                // Failed during the review phase — re-enter awaiting review so user can try again
                Status = BatchQueueItemStatus.AwaitingReview;
                Image?.SetHasPendingOperation(true);
                queue.OnItemRetryReview(this);
            }
            else
            {
                Status = BatchQueueItemStatus.Pending;
                Image?.SetHasPendingOperation(true);
                queue.TriggerProcessing();
            }
        }
    }
}
