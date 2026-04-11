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
            Func<Task> operation)
        {
            this.queue = queue;
            Image = image;
            ImageFilename = imageFilename;
            Folder = folder;
            OperationDescription = operationDescription;
            Operation = operation;
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsPending))]
        [NotifyPropertyChangedFor(nameof(IsRunning))]
        [NotifyPropertyChangedFor(nameof(IsCompleted))]
        [NotifyPropertyChangedFor(nameof(IsFailed))]
        private BatchQueueItemStatus status;

        [ObservableProperty]
        private string? errorMessage;

        public bool IsPending => Status == BatchQueueItemStatus.Pending;
        public bool IsRunning => Status == BatchQueueItemStatus.Running;
        public bool IsCompleted => Status == BatchQueueItemStatus.Completed;
        public bool IsFailed => Status == BatchQueueItemStatus.Failed;

        [RelayCommand]
        public void Retry()
        {
            ErrorMessage = null;
            Status = BatchQueueItemStatus.Pending;
            Image?.SetHasPendingOperation(true);
            queue.TriggerProcessing();
        }
    }
}
