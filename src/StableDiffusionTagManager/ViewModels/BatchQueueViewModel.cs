using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class BatchQueueViewModel : ObservableObject
    {
        private TaskCompletionSource<bool>? resumeTcs;
        private bool processingActive = false;

        public ObservableCollection<BatchQueueItem> Items { get; } = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        [NotifyCanExecuteChangedFor(nameof(ResumeCommand))]
        private bool isPaused;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(PauseCommand))]
        private bool isProcessing;

        [ObservableProperty]
        private bool isVisible;

        private bool CanPause() => IsProcessing && !IsPaused;
        private bool CanResume() => IsPaused;

        public bool HasItems => Items.Count > 0;
        public int PendingCount => Items.Count(i => i.IsPending || i.IsRunning);
        public int FailedCount => Items.Count(i => i.IsFailed);

        public void EnqueueRange(IEnumerable<BatchQueueItem> items)
        {
            foreach (var item in items)
                Items.Add(item);

            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(PendingCount));
            IsVisible = true;
            TriggerProcessing();
        }

        internal void TriggerProcessing()
        {
            if (IsPaused && resumeTcs != null)
            {
                // Resume a paused queue
                var tcs = resumeTcs;
                resumeTcs = null;
                IsPaused = false;
                tcs.SetResult(true);
            }
            else if (!processingActive && Items.Any(i => i.Status == BatchQueueItemStatus.Pending))
            {
                processingActive = true;
                _ = ProcessQueueAsync();
            }
        }

        private async Task ProcessQueueAsync()
        {
            await Dispatcher.UIThread.InvokeAsync(() => IsProcessing = true);

            while (true)
            {
                // Yield to UI thread between items so the app stays responsive
                await Task.Yield();

                // Wait if paused (manual or auto-pause after failure)
                if (resumeTcs != null)
                {
                    await resumeTcs.Task;
                }

                BatchQueueItem? nextItem = null;
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    nextItem = Items.FirstOrDefault(i => i.Status == BatchQueueItemStatus.Pending);
                });

                if (nextItem == null)
                    break;

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    nextItem.Status = BatchQueueItemStatus.Running;
                    OnPropertyChanged(nameof(PendingCount));
                });

                try
                {
                    await nextItem.Operation();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        nextItem.Status = BatchQueueItemStatus.Completed;
                        nextItem.Image?.SetHasPendingOperation(false);
                        OnPropertyChanged(nameof(PendingCount));
                    });
                }
                catch (Exception ex)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        nextItem.ErrorMessage = ex.Message;
                        nextItem.Status = BatchQueueItemStatus.Failed;
                        nextItem.Image?.SetHasPendingOperation(false);
                        OnPropertyChanged(nameof(PendingCount));
                        OnPropertyChanged(nameof(FailedCount));

                        // Auto-pause: next loop iteration will wait on this TCS
                        resumeTcs = new TaskCompletionSource<bool>();
                        IsPaused = true;
                    });
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsProcessing = false;
                processingActive = false;
                OnPropertyChanged(nameof(PendingCount));
            });
        }

        /// <summary>
        /// Called whenever a folder's images are loaded into the UI. Finds any pending or
        /// running queue items targeting this folder, re-links their Image reference to the
        /// freshly-created ImageWithTagsViewModel, and sets HasPendingOperation so thumbnails
        /// show the clock indicator. This handles both switching to a new folder that has
        /// queued work and switching back to a folder the user previously left.
        /// </summary>
        public void SyncPendingIndicatorsForFolder(string folder, IEnumerable<ImageWithTagsViewModel> loadedImages)
        {
            var activeItems = Items
                .Where(i => i.IsPending || i.IsRunning)
                .Where(i => string.Equals(i.Folder, folder, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (activeItems.Count == 0) return;

            var byFilename = loadedImages.ToDictionary(
                img => img.Filename,
                StringComparer.OrdinalIgnoreCase);

            foreach (var item in activeItems)
            {
                if (!byFilename.TryGetValue(item.ImageFilename, out var loadedImage))
                    continue;

                item.Image = loadedImage;
                loadedImage.SetHasPendingOperation(true, item.OperationDescription);
            }
        }

        [RelayCommand(CanExecute = nameof(CanPause))]
        public void Pause()
        {
            if (!IsPaused)
            {
                resumeTcs = new TaskCompletionSource<bool>();
                IsPaused = true;
            }
        }

        [RelayCommand(CanExecute = nameof(CanResume))]
        public void Resume()
        {
            TriggerProcessing();
        }

        [RelayCommand]
        public void ClearCompleted()
        {
            var toRemove = Items.Where(i => i.IsCompleted).ToList();
            foreach (var item in toRemove)
                Items.Remove(item);

            OnPropertyChanged(nameof(HasItems));
        }

        /// <summary>
        /// Removes all Pending and Failed items from the queue and clears their pending status
        /// on the associated image. The currently-running item (if any) is allowed to finish.
        /// Use this when the user wants to cancel a batch and re-queue with corrected settings.
        /// </summary>
        [RelayCommand]
        public void CancelPending()
        {
            var toRemove = Items
                .Where(i => i.Status == BatchQueueItemStatus.Pending || i.Status == BatchQueueItemStatus.Failed)
                .ToList();

            foreach (var item in toRemove)
            {
                item.Image?.SetHasPendingOperation(false);
                Items.Remove(item);
            }

            OnPropertyChanged(nameof(HasItems));
            OnPropertyChanged(nameof(PendingCount));
            OnPropertyChanged(nameof(FailedCount));

            // If the queue was paused (e.g. after a failure), release the pause so the
            // processing loop can wake up, find no pending items, and exit cleanly.
            if (IsPaused && resumeTcs != null)
            {
                var tcs = resumeTcs;
                resumeTcs = null;
                IsPaused = false;
                tcs.SetResult(true);
            }
        }
    }
}
