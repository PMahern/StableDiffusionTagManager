using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using TagUtil;
using System.Linq;
using System;
using Avalonia;
using Avalonia.Media.Imaging;
using System.IO;
using StableDiffusionTagManager.Models;
using System.Collections.Generic;
using StableDiffusionTagManager.Views;
using Avalonia.Controls;
using SdWebUiApi.Api;
using Newtonsoft.Json.Linq;
using StableDiffusionTagManager.Extensions;
using Avalonia.Media;
using StableDiffusionTagManager.Controls;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Base;
using System.Collections.Specialized;
using ImageUtil;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using SdWebUiApi;

namespace StableDiffusionTagManager.ViewModels
{
    public partial class MainWindowViewModel : ViewModelBase
    {
        private static readonly string PROJECT_FOLDER_NAME = ".sdtmproj";
        private static readonly string PROJECT_FILE_NAME = "_project.xml";
        private static readonly string ARCHIVE_FOLDER = "archive";
        private static readonly string TAG_PRIORITY_SETS_FOLDER = "TagPrioritySets";

        public MainWindowViewModel()
        {
            UpdateTagPrioritySets();
        }

        private List<string>? _CopiedTags = null;

        private List<string>? CopiedTags
        {
            get => _CopiedTags;
            set
            {
                _CopiedTags = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasCopiedTags));
            }
        }

        public bool HasCopiedTags
        {
            get
            {
                return _CopiedTags != null && _CopiedTags.Any();
            }
        }

        private ObservableCollection<ImageWithTagsViewModel>? imageWithTagsViewModel;
        public ObservableCollection<ImageWithTagsViewModel>? ImagesWithTags
        {
            get => imageWithTagsViewModel;
            set
            {
                if (imageWithTagsViewModel != null)
                {
                    imageWithTagsViewModel.CollectionChanged -= ImageWithTagsViewModel_CollectionChanged;
                }
                imageWithTagsViewModel = value;
                if (imageWithTagsViewModel != null)
                {
                    imageWithTagsViewModel.CollectionChanged += ImageWithTagsViewModel_CollectionChanged;
                }
                RebuildFilteredImageSet();
                OnPropertyChanged();
                OnPropertyChanged(nameof(ImagesLoaded));
                AddTagToEndOfAllImagesCommand.NotifyCanExecuteChanged();
                AddTagToStartOfAllImagesCommand.NotifyCanExecuteChanged();
                RemoveTagFromAllImagesCommand.NotifyCanExecuteChanged();
                RemoveMetaTagsCommand.NotifyCanExecuteChanged();
            }
        }

        public ObservableCollection<ImageWithTagsViewModel>? filteredImageSet;
        public ObservableCollection<ImageWithTagsViewModel>? FilteredImageSet
        {
            get => filteredImageSet;
            set
            {
                filteredImageSet = value;
                OnPropertyChanged();
            }
        }

        private void ImageWithTagsViewModel_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RebuildFilteredImageSet();
        }

        private void RebuildFilteredImageSet()
        {
            if (this.imageWithTagsViewModel != null)
            {
                this.FilteredImageSet = new ObservableCollection<ImageWithTagsViewModel>(this.imageWithTagsViewModel.Where(PassesCurrentFilter));
            }
            else
            {
                this.FilteredImageSet = null;
            }
        }

        public bool PassesCurrentFilter(ImageWithTagsViewModel imageWithTagsViewModel)
        {
            switch (currentImageFilterMode)
            {
                case ImageFilterMode.None:
                    return true;
                case ImageFilterMode.NotCompleted:
                    return !imageWithTagsViewModel.IsComplete;
                case ImageFilterMode.Completed:
                    return imageWithTagsViewModel.IsComplete;
            }

            return false;
        }


        List<string>? tagCache;

        ImageWithTagsViewModel? selectedImage;
        public ImageWithTagsViewModel? SelectedImage
        {
            get => selectedImage;
            set
            {
                if (selectedImage != value)
                {
                    if (tagCache != null && selectedImage != null)
                    {
                        var newTags = selectedImage.Tags.Select(t => t.Tag);

                        var removedTags = tagCache.Except(newTags);
                        var addedTags = newTags.Except(tagCache);

                        foreach (var removedTag in removedTags)
                        {
                            if (TagCountDictionary.ContainsKey(removedTag))
                            {
                                var vm = TagCountDictionary[removedTag];
                                vm.Count -= 1;
                                if (vm.Count == 0)
                                {
                                    TagCountDictionary.Remove(removedTag);
                                    TagCounts.Remove(vm);
                                }
                            }
                        }

                        foreach (var addedTag in addedTags)
                        {
                            if (TagCountDictionary.ContainsKey(addedTag))
                            {
                                TagCountDictionary[addedTag].Count += 1;
                            }
                            else
                            {
                                var added = new TagWithCountViewModel(this) { Tag = addedTag, Count = 1 };
                                TagCountDictionary[addedTag] = new TagWithCountViewModel(this) { Tag = addedTag, Count = 1 };
                                TagCounts.Add(added);
                            }
                        }
                    }

                    selectedImage = value;

                    tagCache = selectedImage?.Tags.Select(t => t.Tag).ToList();
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsImageSelected));
                }
            }
        }

        #region Tag Collections
        private TagCollectionViewModel? editTagCollectionTarget;

        public TagCollectionViewModel? EditTagCollectionTarget
        {
            get => editTagCollectionTarget;
            set
            {
                var old = editTagCollectionTarget;
                if (SetProperty(ref editTagCollectionTarget, value))
                {
                    if (old != null)
                    {
                        old.IsSelected = false;
                    }
                    if (value != null)
                    {
                        value.IsSelected = true;
                    }
                    AddTagToCollectionCommand.NotifyCanExecuteChanged();
                }
            }
        }

        public void RemoveTagCollection(TagCollectionViewModel collection)
        {
            if(this.TagCollections != null)
            {
                this.TagCollections.Remove(collection);
                if (this.EditTagCollectionTarget == collection)
                {
                    this.EditTagCollectionTarget = null;
                }
            }
        }

        public bool IsEditingTagCollection { get => EditTagCollectionTarget != null; }
        [ObservableProperty]
        private ObservableCollection<TagCollectionViewModel>? tagCollections;

        [RelayCommand(CanExecute = nameof(IsProject))]
        public async Task AddTagCollection()
        {
            if (OpenProject != null)
            {
                if (this.TagCollections == null)
                {
                    this.TagCollections = new ObservableCollection<TagCollectionViewModel>();
                }
                this.TagCollections.Add(new TagCollectionViewModel(this, ""));
            }
        }

        [RelayCommand(CanExecute = nameof(IsEditingTagCollection))]
        public void AddTagToCollection()
        {
            if (EditTagCollectionTarget != null)
            {
                EditTagCollectionTarget.AddEmptyTag();
            }
        }

        #endregion

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(AddTagCollectionCommand))]
        [NotifyPropertyChangedFor(nameof(IsProject))]
        private Project? openProject = null;

        public bool IsProject { get => OpenProject != null; }

        private string? openFolder = null;

        #region Callbacks and Events
        public DialogHandler? ShowDialogHandler { get; set; }

        public Func<Task<IReadOnlyList<IStorageFolder>>> ShowFolderDialogCallback { get; set; }

        public Action<TagViewModel>? FocusTagCallback { get; set; }

        public Action? ExitCallback { get; set; }

        public Func<Bitmap, bool>? ImageDirtyCallback { get; set; }
        public Func<Bitmap, bool, Bitmap>? GetModifiedImageDataCallback { get; set; }
        #endregion

        #region Dialog Handling
        private Task<TResult> ShowDialog<TResult>(IMsBox<TResult> mbox) where TResult : struct
        {
            if (ShowDialogHandler != null)
            {
                return ShowDialogHandler.ShowDialog(mbox);
            }
            return Task.FromResult(default(TResult));
        }

        private async Task<TResult?> ShowDialog<TResult>(IDialogWithResultAsync<TResult> dialog)
        {
            if (ShowDialogHandler != null)
            {
                return await ShowDialogHandler.ShowDialog(dialog);
            }
            return default;
        }

        private Task<TResult> ShowDialog<TResult>(Window dialog) where TResult : struct
        {
            if (ShowDialogHandler != null)
            {
                return ShowDialogHandler.ShowDialog<TResult>(dialog);
            }
            return Task.FromResult(default(TResult));
        }

        private Task ShowDialog(Window dialog)
        {
            if (ShowDialogHandler != null)
            {
                return ShowDialogHandler.ShowDialog(dialog);
            }
            return Task.CompletedTask;
        }

        #endregion

        public async Task CheckForAndConvertUnspportedImageFormats(string folder)
        {

            var webps = Directory.EnumerateFiles(folder, "*.webp");

            if (webps.Any())
            {
                var messageBoxStandardWindow = MessageBoxManager
                                .GetMessageBoxStandard("Unsuppoted Image Formats Detected",
                                                         "This tool only supports JPG and PNG image file formats, webp images can be automatically converted and the originals moved to the archive subdirectory, would you like to convert all webp images to png now?",
                                                         ButtonEnum.YesNo,
                                                         Icon.Question);
                if ((await ShowDialog(messageBoxStandardWindow)) == ButtonResult.Yes)
                {
                    foreach (var webp in webps)
                    {
                        ImageConverter.ConvertImageFileToPng(webp);
                        ArchiveImage(folder, webp);
                    }
                }
            }
        }

        [RelayCommand]
        public async Task LoadFolder()
        {
            if (!await CheckCanExit())
            {
                return;
            }

            if (ShowFolderDialogCallback != null)
            {
                var folderPickResult = await ShowFolderDialogCallback();
                if (folderPickResult.Any())
                {
                    var pickedFolderPath = folderPickResult.First().TryGetLocalPath();
                    if (pickedFolderPath != null)
                    {
                        await CheckForAndConvertUnspportedImageFormats(pickedFolderPath);

                        var projectPath = Path.Combine(pickedFolderPath, PROJECT_FOLDER_NAME);
                        var projFolder = Directory.Exists(projectPath);
                        var projFile = Path.Combine(projectPath, PROJECT_FILE_NAME);

                        if (!projFolder)
                        {
                            var messageBoxStandardWindow = MessageBoxManager
                                    .GetMessageBoxStandard("Create Project?",
                                                             "It appears you haven't created a project for this folder. Creating a project will back up all the current existing images and tag sets into a folder named .sdtmproj so you can restore them and will also let you set some properties on the project. Would you like to create a project now?",
                                                             ButtonEnum.YesNo,
                                                             Icon.Question);
                            if ((await ShowDialog(messageBoxStandardWindow)) == ButtonResult.Yes)
                            {
                                projFolder = true;

                                messageBoxStandardWindow = MessageBoxManager
                                                            .GetMessageBoxStandard("Rename Images?",
                                                                 "You can optionally rename all the images to have an increasing index instead of the current existing pattern.  Would you like to do this now?",
                                                                 ButtonEnum.YesNo,
                                                                 Icon.Question);

                                var renameImages = (await ShowDialog(messageBoxStandardWindow) == ButtonResult.Yes);

                                var jpegs = Directory.EnumerateFiles(pickedFolderPath, "*.jpg").ToList();
                                var pngs = Directory.EnumerateFiles(pickedFolderPath, "*.png").ToList();
                                var txts = Directory.EnumerateFiles(pickedFolderPath, "*.txt").ToList();

                                Directory.CreateDirectory(projectPath);

                                var project = new Project(projFile);
                                project.ProjectUpdated = UpdateProjectSettings;

                                var all = jpegs.Concat(pngs).Concat(txts).ToList();
                                if (renameImages)
                                {
                                    foreach (var file in all)
                                    {
                                        File.Move(file, Path.Combine(projectPath, Path.GetFileName(file)));
                                    }

                                    var movedjpegs = Directory.EnumerateFiles(projectPath, "*.jpg").ToList();
                                    var movedpngs = Directory.EnumerateFiles(projectPath, "*.png").ToList();
                                    var movedtxts = Directory.EnumerateFiles(projectPath, "*.txt").ToList();
                                    var imagesToCopy = movedjpegs.Concat(movedpngs).ToList();

                                    int i = 1;
                                    foreach (var image in imagesToCopy)
                                    {
                                        var filename = i.ToString("00000");
                                        var newFileName = Path.Combine(pickedFolderPath, $"{filename}{Path.GetExtension(image)}");
                                        File.Copy(image, newFileName);
                                        var matchingTagFile = movedtxts.FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == Path.GetFileNameWithoutExtension(image));
                                        if (matchingTagFile != null)
                                        {
                                            File.Copy(matchingTagFile, Path.Combine(pickedFolderPath, $"{filename}.txt"));
                                        }
                                        project.AddBackedUpFileMap(Path.GetFileName(image), Path.GetFileName(newFileName));

                                        ++i;
                                    }
                                    project.Save();
                                }
                                else
                                {
                                    foreach (var file in all)
                                    {
                                        File.Copy(file, Path.Combine(projectPath, Path.GetFileName(file)));
                                    }
                                }
                            }
                        }

                        var FolderTagSets = new FolderTagSets(pickedFolderPath);

                        ImagesWithTags = new(FolderTagSets.TagsSets.Select(tagSet => new ImageWithTagsViewModel(tagSet.ImageFile, tagSet.TagSet, ImageDirtyHandler))
                                        .OrderBy(iwt => iwt.FirstNumberedChunk)
                                        .ThenBy(iwt => iwt.SecondNumberedChunk));

                        if (projFolder)
                        {
                            OpenProject = new Project(projFile);
                            OpenProject.ProjectUpdated += UpdateProjectSettings;
                            UpdateProjectSettings();
                        }

                        if (OpenProject != null)
                        {
                            TagCollections = new ObservableCollection<TagCollectionViewModel>(OpenProject.TagCollections.Select(c => new TagCollectionViewModel(this, c.Name, c.Tags)));
                        }

                        openFolder = pickedFolderPath;
                        UpdateTagCounts();
                    }
                }
            }
        }

        private void UpdateProjectSettings()
        {
            TargetImageSize = OpenProject?.TargetImageSize;
            if (OpenProject != null && ImagesWithTags != null)
            {
                foreach (var image in ImagesWithTags)
                {
                    image.IsComplete = OpenProject.CompletedImages.Contains(image.Filename);
                }
            }
        }

        [RelayCommand]
        public void SaveChanges()
        {
            if (ImagesWithTags != null)
            {
                foreach (var image in ImagesWithTags)
                {
                    var set = new TagSet(Path.Combine(openFolder, image.GetTagsFileName()), image.Tags.Select(t => t.Tag));
                    set.WriteFile();

                    image.ClearTagsDirty();

                    if (image.IsImageDirty() && GetModifiedImageDataCallback != null)
                    {
                        var newImageData = GetModifiedImageDataCallback(image.ImageSource, true);
                        SaveUpdatedImage(image, newImageData);
                    }
                }

                if (OpenProject != null)
                {
                    if(TagCollections != null)
                    {
                        OpenProject.TagCollections = TagCollections.Select(tc => new TagCollection()
                        {
                            Name = tc.Name,
                            Tags = tc.Tags.Select(t => t.Tag).ToList()
                        }).ToList();
                    }
                    else
                    {
                        OpenProject.TagCollections.Clear();
                    }
                    
                    OpenProject.Save();
                }
            }
        }

        //Tag Drag handling
        public void BeginTagDrag(TagViewModel tagViewModel)
        {
            selectedImage?.BeginTagDrag(tagViewModel);
        }

        public void TagDrop(TagViewModel dropTarget)
        {
            selectedImage?.TagDrop(dropTarget);
        }

        public bool IsImageSelected => SelectedImage != null;

        public void AddTagInFrontOfTag(TagViewModel tag)
        {
            var newTag = new TagViewModel("");
            var index = SelectedImage?.Tags.IndexOf(tag) ?? -1;
            if (index != -1)
            {
                SelectedImage?.InsertTag(index, newTag);
            }
            FocusTagCallback?.Invoke(newTag);
        }

        [RelayCommand(CanExecute = nameof(IsImageSelected))]
        public void AddTagToFront()
        {
            var newTag = new TagViewModel("");
            SelectedImage?.InsertTag(0, newTag);
            FocusTagCallback?.Invoke(newTag);
        }

        [RelayCommand(CanExecute = nameof(IsImageSelected))]
        public void AddTagToEnd()
        {
            var newTag = new TagViewModel("");
            SelectedImage?.AddTag(newTag);
            FocusTagCallback?.Invoke(newTag);
        }

        public void AddTagToEnd(string tag)
        {
            var newTag = new TagViewModel(tag);
            SelectedImage?.AddTag(newTag);
        }

        public void AddMissingTagsToEnd(List<string> Tags)
        {
            if (SelectedImage != null)
            {
                foreach (var tag in Tags)
                {
                    if (!SelectedImage.Tags.Any(t => t.Tag == tag))
                    {
                        SelectedImage.AddTag(new TagViewModel(tag));
                    }
                }
            }
        }

        public bool ImagesLoaded => this.ImagesWithTags?.Any() ?? false;

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task AddTagToEndOfAllImages()
        {
            if (ImagesWithTags != null)
            {
                var dialog = new TagSearchDialog();
                var tagResult = await ShowDialog<string?>(dialog);

                if (tagResult != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        if (!image.Tags.Any(t => t.Tag == tagResult))
                        {
                            image.AddTag(new TagViewModel(tagResult));
                        }
                    }
                }
            }
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task AddTagToStartOfAllImages()
        {
            if (ImagesWithTags != null)
            {
                var dialog = new TagSearchDialog();
                var tagResult = await ShowDialog<string?>(dialog);

                if (tagResult != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        if (!image.Tags.Any(t => t.Tag == tagResult))
                        {
                            image.InsertTag(0, new TagViewModel(tagResult));
                        }
                    }
                }
            }

            UpdateTagCounts();
        }

        public async void ReplaceTagInAllImages(string target)
        {
            if (ImagesWithTags != null)
            {
                var dialog = new TagSearchDialog();
                dialog.Title = $"Replace all instances of tag {target} with new tag";
                var tagResult = await ShowDialog<string?>(dialog);

                if (tagResult != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        image.ReplaceTagIfExists(target, tagResult);
                    }
                }
            }
            UpdateTagCounts();
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task RemoveTagFromAllImages()
        {
            if (ImagesWithTags != null)
            {
                var dialog = new TagSearchDialog();
                var tagResult = await ShowDialog<string?>(dialog);

                if (tagResult != null)
                {
                    RemoveTagFromAllImages(tagResult);
                }
            }
        }

        public void RemoveTagFromAllImages(string tag)
        {
            if (ImagesWithTags != null)
            {
                foreach (var image in ImagesWithTags)
                {
                    var toRemove = image.Tags.Where(t => t.Tag == tag).ToList();
                    foreach (var tagToRemove in toRemove)
                    {
                        image.RemoveTag(tagToRemove);
                    }
                }
                UpdateTagCounts();
            }
        }

        internal void NextImage()
        {
            if (FilteredImageSet?.Any() ?? false)
            {
                if (SelectedImage == null)
                {
                    SelectedImage = this.FilteredImageSet.First();
                }
                else
                {
                    var index = FilteredImageSet.IndexOf(SelectedImage);
                    if (index < FilteredImageSet.Count() - 1)
                    {
                        SelectedImage = FilteredImageSet[index + 1];
                    }
                }
            }
        }

        internal void PreviousImage()
        {
            if (FilteredImageSet?.Any() ?? false)
            {
                if (SelectedImage != null)
                {
                    var index = FilteredImageSet.IndexOf(SelectedImage);
                    if (index > 0)
                    {
                        SelectedImage = FilteredImageSet[index - 1];
                    }
                }
            }
        }

        public void DeleteTagFromCurrentImage(TagViewModel tag)
        {
            if (SelectedImage != null)
            {
                SelectedImage.RemoveTag(tag);
            }
        }

        private ObservableCollection<TagWithCountViewModel> tagCounts = new ObservableCollection<TagWithCountViewModel>();
        public ObservableCollection<TagWithCountViewModel> TagCounts { get => tagCounts; set { tagCounts = value; OnPropertyChanged(); } }


        private PixelSize? targetImageSize;
        public PixelSize? TargetImageSize
        {
            get => targetImageSize; set
            {
                targetImageSize = value;
                OnPropertyChanged();
            }
        }

        #region Progress indicator
        private bool showProgressIndicator = false;
        public bool ShowProgressIndicator
        {
            get => showProgressIndicator;
            private set
            {
                showProgressIndicator = value;
                OnPropertyChanged();
            }
        }

        private int progressIndicatorMax = 0;
        public int ProgressIndicatorMax
        {
            get => progressIndicatorMax;
            private set
            {
                progressIndicatorMax = value;
                OnPropertyChanged();
            }
        }

        private int progressIndicatorProgress = 0;
        public int ProgressIndicatorProgress
        {
            get => progressIndicatorProgress;
            private set
            {
                progressIndicatorProgress = value;
                OnPropertyChanged();
            }
        }

        private string progressIndicatorMessage = "";
        public string ProgressIndicatorMessage
        {
            get => progressIndicatorMessage;
            private set
            {
                progressIndicatorMessage = value;
                OnPropertyChanged();
            }
        }
        #endregion

        private Dictionary<string, TagWithCountViewModel> TagCountDictionary = new Dictionary<string, TagWithCountViewModel>();


        public void UpdateTagCounts()
        {
            if (ImagesWithTags != null)
            {
                TagCountDictionary = ImagesWithTags.SelectMany(i => i.Tags.Select(t => t.Tag).Where(t => t != ""))
                        .GroupBy(t => t)
                        .OrderBy(t => t.Key)
                        .ToDictionary(g => g.Key, g => new TagWithCountViewModel(this) { Tag = g.Key, Count = g.Count() });
                //.Select(pair => new TagWithCountViewModel(this)
                //{
                //    Tag = pair.Key,
                //    Count = pair.Count()
                //}).OrderBy(tc => tc.Tag)); ;

                tagCache = selectedImage?.Tags.Select(t => t.Tag).ToList();
                UpdateTagCountsObservable();
            }
        }

        public void UpdateTagCountsObservable()
        {
            TagCounts = new ObservableCollection<TagWithCountViewModel>(TagCountDictionary
                                                                            .Select(pair => pair.Value));
        }

        public void MoveTagLeft(TagViewModel tag)
        {
            if (SelectedImage != null)
            {
                var index = SelectedImage.Tags.IndexOf(tag);
                if (index > 0)
                {
                    SelectedImage.RemoveTagAt(index);
                    SelectedImage.InsertTag(index - 1, tag);
                    FocusTagCallback?.Invoke(tag);
                }
            }

        }

        public void MoveTagRight(TagViewModel tag)
        {
            if (SelectedImage != null)
            {
                var index = SelectedImage.Tags.IndexOf(tag);
                if (index < SelectedImage.Tags.Count() - 1)
                {
                    SelectedImage.RemoveTagAt(index);
                    SelectedImage.InsertTag(index + 1, tag);
                    FocusTagCallback?.Invoke(tag);
                }
            }
        }

        public void ArchiveImage(string targetFolder, string targetFile, string? tagsFilename = null)
        {
            var destDirectory = Path.Combine(targetFolder, ARCHIVE_FOLDER);
            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            var destFile = Path.Combine(destDirectory, targetFile);
            var destFileWithoutExtension = Path.Combine(destDirectory, Path.GetFileNameWithoutExtension(targetFile));
            var i = 0;
            while (File.Exists(destFile))
            {
                destFileWithoutExtension = Path.Combine(destDirectory, $"{Path.GetFileNameWithoutExtension(targetFile)}_{i.ToString("00")}");
                destFile = $"{destFileWithoutExtension}{Path.GetExtension(targetFile)}";
            }

            File.Move(Path.Combine(targetFolder, targetFile), destFile);

            if (tagsFilename != null)
            {
                var tagFile = Path.Combine(targetFolder, tagsFilename);
                if (File.Exists(tagFile))
                {
                    File.Move(tagFile, Path.Combine(destDirectory, $"{destFileWithoutExtension}.txt"));
                }
            }

        }

        [RelayCommand]
        public async Task ArchiveSelectedImage()
        {
            if (SelectedImage != null && ImagesWithTags != null)
            {
                var messageBoxStandardWindow = MessageBoxManager
                            .GetMessageBoxStandard("Archive Image?",
                                                         "Really archive selected image? It will be moved to a subdirectory named archive.",
                                                         ButtonEnum.YesNo,
                                                         Icon.Warning);

                var result = await ShowDialog(messageBoxStandardWindow);

                if (result == ButtonResult.Yes)
                {
                    var imageToDelete = SelectedImage;
                    var index = ImagesWithTags.IndexOf(imageToDelete);
                    ImagesWithTags.Remove(imageToDelete);
                    if (ImagesWithTags.Count > 0)
                    {
                        if (index < ImagesWithTags.Count())
                        {
                            SelectedImage = ImagesWithTags[index];
                        }
                        else
                        {
                            SelectedImage = ImagesWithTags.Last();
                        }
                    }
                    else
                    {
                        SelectedImage = null;
                    }

                    ArchiveImage(openFolder, imageToDelete.Filename, imageToDelete.GetTagsFileName());
                }
            }
        }

        [RelayCommand]
        public async Task RunImgToImg(Bitmap image)
        {
            if (SelectedImage != null)
            {
                var dialog = new ImageTouchupDialog();

                dialog.Tags = SelectedImage.Tags.Select(t => t.Tag).ToList();
                dialog.Image = image;
                dialog.OpenProject = this.OpenProject;
                await ShowDialog(dialog);

                if (dialog.Success)
                {
                    SelectedImage.ImageSource = dialog.Image;
                    SelectedImage.ImageSource.Save(Path.Combine(this.openFolder, SelectedImage.Filename));
                }
            }
        }

        [RelayCommand]
        public async void ApplicationSettings()
        {
            var dialog = new SettingsDialog();

            await ShowDialog(dialog);
        }

        [RelayCommand]
        public async Task ProjectSettings()
        {
            if (OpenProject != null)
            {
                var dialog = new ProjectSettingsDialog();

                dialog.Project = OpenProject;

                await ShowDialog(dialog);
            }
        }

        [RelayCommand]
        public async Task Exit()
        {
            if (await CheckCanExit())
            {
                ExitCallback?.Invoke();
            }
        }

        [RelayCommand]
        public async Task ClearTags()
        {
            if (SelectedImage != null && SelectedImage.Tags.Any())
            {
                var dialog = MessageBoxManager
                                    .GetMessageBoxStandard("Delete all tags",
                                                                 "This will delete all tags for the current image, are you sure?",
                                                                 ButtonEnum.YesNo,
                                                                 Icon.Warning);

                var result = await ShowDialog(dialog);

                if (result == ButtonResult.Yes)
                {
                    SelectedImage.ClearTags();
                }
            }
        }

        public async Task<bool> CheckCanExit()
        {
            if (ImagesWithTags != null)
            {
                var dirtyImages = ImagesWithTags.Where(iwt => iwt.IsImageDirty());

                var dirtyTags = ImagesWithTags.Where(iwt => iwt.AreTagsDirty());

                if (dirtyImages.Any() || dirtyTags.Any())
                {
                    var dialog = MessageBoxManager
                                    .GetMessageBoxStandard("Unsaved Changes",
                                                                 "You have unsaved changes, do you wish to save them now?",
                                                                 ButtonEnum.YesNoCancel,
                                                                 Icon.Warning);

                    var result = (await ShowDialog(dialog));
                    if (result == ButtonResult.Yes)
                    {
                        SaveChanges();
                    }

                    if (result == ButtonResult.Cancel)
                    {
                        return false;
                    }
                }
            }

            return true;
        }
        public async Task InterrogateAndApplyToSelectedImage(Bitmap bitmap)
        {
            //Cache the selected image in case it's changed during async operation
            var selectedImage = SelectedImage;
            var tags = await Interrogate(bitmap);

            if (tags != null)
            {
                foreach (var tag in tags)
                {
                    selectedImage.AddTagIfNotExists(tag);
                }
            }
        }

        #region Image Interrogation
        public async Task ReviewRemoveBackground(Bitmap bitmap)
        {
            //Cache the selected image in case it's changed during async operation
            var selectedImage = SelectedImage;

            var api = new DefaultApi(App.Settings.WebUiAddress);

            using var uploadStream = new MemoryStream();
            bitmap.Save(uploadStream);
            var imagebase64 = Convert.ToBase64String(uploadStream.ToArray());

            var result = await api.RembgRemoveRembgPostAsync(new SdWebUiApi.Model.BodyRembgRemoveRembgPost
            {
                Model = RembgModels.U2NetHumanSeg,
                InputImage = imagebase64,
            });

            var jtokResult = result as JToken;
            var convertedresult = jtokResult?.ToObject<RemBgResult>();
            if (convertedresult != null)
            {
                using (var mstream = new MemoryStream(Convert.FromBase64String(convertedresult.image)))
                {
                    var imageResult = new Bitmap(mstream);

                    var viewer = new ImageReviewDialog();
                    viewer.Title = "Review image with removed background";
                    viewer.Images = new ObservableCollection<ImageReviewViewModel>(){ new ImageReviewViewModel(imageResult) };
                    viewer.ReviewMode = ImageReviewDialogMode.SingleSelect;
                    await ShowDialog(viewer);

                    if (viewer.Success)
                    {
                        selectedImage.ImageSource = imageResult;
                        selectedImage.ImageSource.Save(Path.Combine(this.openFolder, selectedImage.Filename));
                    }
                }
            }
        }

        public async Task<IEnumerable<TagViewModel>?> Interrogate(Bitmap image)
        {
            var api = new DefaultApi(App.Settings.WebUiAddress);

            var model = "deepdanbooru";

            if (OpenProject != null && OpenProject.InterrogateMethod == SdWebUiApi.InterrogateMethod.Clip)
            {
                model = "clip";
            }
            using var uploadStream = new MemoryStream();
            image.Save(uploadStream);
            var imagebase64 = Convert.ToBase64String(uploadStream.ToArray());

            try
            {
                var result = await api.InterrogateapiSdapiV1InterrogatePostAsync(new SdWebUiApi.Model.InterrogateRequest
                {
                    Image = imagebase64,
                    Model = model
                });

                var jtokResult = result as JToken;
                var convertedresult = jtokResult?.ToObject<InterrogateResult>();
                if (convertedresult != null)
                {
                    return convertedresult.caption.Split(", ")
                                                  .Select(t => new TagViewModel(t));
                }
            }
            catch (Exception ex)
            {
                var messageBoxStandardWindow = MessageBoxManager
                        .GetMessageBoxStandard("Interrogate Failed",
                                                     $"Failed to interrogate the image. This likely means the stable diffusion webui server can't be reached. Error message: {ex.Message}",
                                                     ButtonEnum.Ok,
                                                     Icon.Warning);

                await ShowDialog(messageBoxStandardWindow);
            }

            return null;
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task InterrogateAllImages()
        {
            if (ImagesWithTags != null && ImagesWithTags.Count > 0)
            {
                ShowProgressIndicator = true;
                ProgressIndicatorMax = ImagesWithTags.Count();
                ProgressIndicatorProgress = 0;
                ProgressIndicatorMessage = "Interrogating all images...";

                foreach (var image in ImagesWithTags)
                {
                    var tags = await Interrogate(image.ImageSource);

                    if (tags != null)
                    {
                        foreach (var tag in tags)
                        {
                            image.AddTagIfNotExists(tag);
                        }
                    }
                    else
                    {
                        //The interrogate failed, likely connection issue, cancel out
                        break;
                    }
                    ++ProgressIndicatorProgress;
                }

                UpdateTagCounts();

                ShowProgressIndicator = false;
            }
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task RemoveMetaTags()
        {
            if (ImagesWithTags != null && ImagesWithTags.Count > 0)
            {
                foreach(var tag in MetaTags.Tags)
                {
                    RemoveTagFromAllImages(tag);
                }
            }
        }

        #endregion

        public void AddNewImage(Bitmap image, IEnumerable<string>? tags = null)
        {
            var index = ImagesWithTags.IndexOf(SelectedImage);
            var withoutExtension = Path.GetFileNameWithoutExtension(SelectedImage.Filename);
            if (SelectedImage.FirstNumberedChunk != -1)
            {
                withoutExtension = SelectedImage.FirstNumberedChunk.ToString("00000");
            }

            int i = 0;
            if (SelectedImage.SecondNumberedChunk != -1)
            {
                i = SelectedImage.SecondNumberedChunk;
            }

            var newFilename = $"{withoutExtension}__{i:00}";
            while (ImagesWithTags.Any(i => newFilename == Path.GetFileNameWithoutExtension(i.Filename)))
            {
                newFilename = $"{withoutExtension}__{++i:00}";
            }
            newFilename = Path.Combine(openFolder, $"{newFilename}.png");
            image.Save(newFilename);
            var newImageViewModel = new ImageWithTagsViewModel(image, newFilename, ImageDirtyHandler, tags);
            if (tags != null)
            {
                var set = new TagSet(Path.Combine(openFolder, newImageViewModel.GetTagsFileName()), tags);
                set.WriteFile();
            }
            ImagesWithTags.Insert(index + 1, newImageViewModel);
        }

        public void UpdateImageListForCompletionStatusChanging()
        {

        }

        public void ImageCropped(Bitmap image)
        {
            AddNewImage(image, SelectedImage.Tags.Select(t => t.Tag));
        }

        public async Task ReviewComicPanels(List<Bitmap> panels)
        {
            ImageReviewDialog dialog = new ImageReviewDialog();
            dialog.Images = panels.Select(p => new ImageReviewViewModel(p)).ToObservableCollection();
            dialog.ReviewMode = ImageReviewDialogMode.MultiSelect;
            dialog.Title = "Select Images to Keep";

            await ShowDialog(dialog);

            if (dialog.Success)
            {
                foreach (var image in dialog.SelectedImages)
                {
                    AddNewImage(image);
                }
            }
        }

        public Task SaveCurrentImage(Bitmap image)
        {
            if (openFolder != null && SelectedImage != null)
            {
                SaveUpdatedImage(SelectedImage, image);
            }

            return Task.CompletedTask;
        }

        public Task SaveUpdatedImage(ImageWithTagsViewModel targetImageWithTags, Bitmap image)
        {
            if (openFolder != null)
            {
                if (Path.GetExtension(targetImageWithTags.Filename) != ".png")
                {
                    image.Save(Path.Combine(openFolder, targetImageWithTags.Filename));
                    targetImageWithTags.Filename = $"{Path.GetFileNameWithoutExtension(targetImageWithTags.Filename)}.png";
                }
                image.Save(Path.Combine(openFolder, targetImageWithTags.Filename));
                targetImageWithTags.ImageSource = image;
                targetImageWithTags.UpdateThumbnail();
            }

            return Task.CompletedTask;
        }

        public async Task ExpandImage(Bitmap image)
        {
            var dialog = new ExpandImageDialog();
            if (OpenProject != null && OpenProject.TargetImageSize.HasValue && OpenProject.TargetImageSize.Value.Width > 0 && OpenProject.TargetImageSize.Value.Height > 0)
            {
                dialog.ComputeExpansionNeededForTargetAspectRatio(image.PixelSize.Width, image.PixelSize.Height, OpenProject.TargetImageSize.Value.Width, OpenProject.TargetImageSize.Value.Height);
            }
            await ShowDialog(dialog);
            if (dialog.Success)
            {
                var finalSize = new PixelSize(image.PixelSize.Width + dialog.ExpandLeft + dialog.ExpandRight, image.PixelSize.Height + dialog.ExpandUp + dialog.ExpandDown);
                var imageRegion = new Rect(dialog.ExpandLeft, dialog.ExpandUp, image.PixelSize.Width, image.PixelSize.Height);
                var newImage = new RenderTargetBitmap(finalSize);
                using (var drawingContext = newImage.CreateDrawingContext())
                {
                    drawingContext.FillRectangle(new SolidColorBrush(new Color(255, 255, 255, 255)), new Rect(0, 0, finalSize.Width, finalSize.Height));

                    drawingContext.DrawImage(image, new Rect(0, 0, image.PixelSize.Width, image.PixelSize.Height), imageRegion);
                }

                AddNewImage(newImage, SelectedImage.Tags.Select(t => t.Tag));
            }
        }

        public bool ImageDirtyHandler(Bitmap image)
        {
            return ImageDirtyCallback?.Invoke(image) ?? false;
        }

        private ImageFilterMode currentImageFilterMode;

        public ImageFilterMode CurrentImageFilterMode
        {
            get { return currentImageFilterMode; }
            set
            {
                if (SetProperty(ref currentImageFilterMode, value))
                {
                    RebuildFilteredImageSet();
                }
            }
        }

        #region Tag Priority Sets
        private ObservableCollection<TagPrioritySetButtonViewModel>? tagPrioritySets;
        public ObservableCollection<TagPrioritySetButtonViewModel>? TagPrioritySets
        {
            get => tagPrioritySets;
            set
            {
                tagPrioritySets = value;
                OnPropertyChanged();
            }
        }

        [RelayCommand]
        public void ApplyTagPrioritySet(TagPrioritySetButtonViewModel buttonVM)
        {
            if (tagPrioritySets != null && tagPrioritySets.Any() && SelectedImage != null)
            {
                SelectedImage.ApplyTagOrdering(t => buttonVM.PrioritySet.GetTagPriority(t));
            }
        }

        [RelayCommand]
        public async Task EditTagPrioritySets()
        {
            var dialog = new TagPrioritySetManagerDialog();

            await ShowDialog(dialog);

            if (dialog.Success)
            {
                UpdateTagPrioritySets();
            }
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task ApplyTagPrioritySetToAllImages()
        {
            if (tagPrioritySets != null && ImagesWithTags != null)
            {
                var dialog = new TagPrioritySelectDialog();
                dialog.TagPrioritySets = tagPrioritySets.ToList();

                var result = await ShowDialog<TagPrioritySetButtonViewModel?>(dialog);
                if (result != null)
                {
                    foreach (var image in ImagesWithTags)
                    {
                        image.ApplyTagOrdering(t => result.PrioritySet.GetTagPriority(t));
                    }
                }
            }
        }

        [RelayCommand]
        public void SetImageFilter(ImageFilterMode mode)
        {
            CurrentImageFilterMode = mode;
        }

        [RelayCommand]
        public void ToggleImageComplete()
        {
            if (SelectedImage != null && OpenProject != null)
            {
                SelectedImage.IsComplete = !SelectedImage.IsComplete;
                OpenProject.SetImageCompletionStatus(SelectedImage.Filename, SelectedImage.IsComplete);
                OpenProject.Save();

                if (CurrentImageFilterMode != ImageFilterMode.None && FilteredImageSet != null)
                {
                    FilteredImageSet.Remove(SelectedImage);
                }
            }
        }

        public void UpdateTagPrioritySets()
        {
            if (Directory.Exists(TAG_PRIORITY_SETS_FOLDER))
            {
                var txts = Directory.EnumerateFiles(TAG_PRIORITY_SETS_FOLDER, "*.txt").ToList();
                TagPrioritySets = txts.Select(filename =>
                                                new TagPrioritySetButtonViewModel(
                                                        Path.GetFileNameWithoutExtension(filename),
                                                        new TagPrioritySet(filename)))
                                  .ToObservableCollection();
            }
        }

        public Bitmap ConvertImageAlphaToColor(Bitmap sourceImage, Color color)
        {
            var newImage = new RenderTargetBitmap(sourceImage.PixelSize);
            using (var drawingContext = newImage.CreateDrawingContext())
            {
                drawingContext.FillRectangle(new SolidColorBrush(color), new Rect(0, 0, sourceImage.PixelSize.Width, sourceImage.PixelSize.Height));
                drawingContext.DrawImage(sourceImage, new Rect(0, 0, sourceImage.PixelSize.Width, sourceImage.PixelSize.Height));
            }
            return newImage;
        }

        [RelayCommand(CanExecute = nameof(ImagesLoaded))]
        public async Task ConvertAllImageAlphasToColor()
        {
            if (openFolder != null && ImagesWithTags != null)
            {
                var dialog = new ColorPickerDialog();
                await ShowDialog<Color?>(dialog);
                if (dialog.Success)
                {
                    ShowProgressIndicator = true;
                    ProgressIndicatorMax = ImagesWithTags.Count();
                    ProgressIndicatorProgress = 0;
                    ProgressIndicatorMessage = "Converting image alpha channels...";

                    foreach (var image in ImagesWithTags)
                    {
                        var sourceImage = image.ImageSource;
                        var newImage = ConvertImageAlphaToColor(sourceImage, dialog.SelectedColor);

                        image.ImageSource = newImage;
                        image.ImageSource.Save(Path.Combine(this.openFolder, image.Filename));
                        ProgressIndicatorProgress++;

                        await Task.Delay(1);
                    }
                    
                    ShowProgressIndicator = false;
                }
            }
        }

        public async Task ReviewConvertAlpha(Bitmap bitmap)
        {
            //Cache the selected image in case it's changed during async operation
            var selectedImage = SelectedImage;

            var dialog = new ColorPickerDialog();
            await ShowDialog<Color?>(dialog);
            if (dialog.Success)
            {
                var imageResult = ConvertImageAlphaToColor(bitmap, dialog.SelectedColor);
                var viewer = new ImageReviewDialog();
                viewer.Title = "Review converted image";
                viewer.Images = new ObservableCollection<ImageReviewViewModel>() { new ImageReviewViewModel(imageResult) };
                viewer.ReviewMode = ImageReviewDialogMode.SingleSelect;
                await ShowDialog(viewer);

                if (viewer.Success)
                {
                    selectedImage.ImageSource = imageResult;
                    selectedImage.ImageSource.Save(Path.Combine(this.openFolder, selectedImage.Filename));
                }
            }
        }


        [RelayCommand]
        public void CopyTags()
        {
            if (SelectedImage != null)
            {
                CopiedTags = new List<string>(SelectedImage.Tags.Select(t => t.Tag));
            }
        }

        [RelayCommand]
        public void PasteTags()
        {
            if (SelectedImage != null && CopiedTags != null)
            {
                foreach (var tag in CopiedTags)
                {
                    SelectedImage.AddTagIfNotExists(new TagViewModel(tag));
                }
            }
        }
        #endregion
    }
}