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
            string folder,
            string operationDescription,
            Func<Task> operation)
        {
            this.queue = queue;
            Image = image;
            Folder = folder;
            OperationDescription = operationDescription;
            Operation = operation;
        }

        /// <summary>
        /// The image this operation targets, or null if the image is not in the currently loaded folder.
        /// </summary>
        public ImageWithTagsViewModel? Image { get; }

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
